#requires -Version 7.2
Set-StrictMode -Version Latest

function Get-ReleaseHash([string] $Path) {
    (Get-FileHash -LiteralPath $Path -Algorithm SHA256 -ErrorAction Stop).Hash
}

function Get-ManagedVersionConstant([string] $Path, [string] $Namespace, [string] $TypeName) {
    # Read metadata only: do not load or execute game/plugin assemblies.
    $stream = [IO.File]::OpenRead($Path)
    $pe = [System.Reflection.PortableExecutable.PEReader]::new($stream)
    try {
        $reader = [System.Reflection.Metadata.PEReaderExtensions]::GetMetadataReader($pe)
        foreach ($handle in $reader.TypeDefinitions) {
            $type = $reader.GetTypeDefinition($handle)
            if ($reader.GetString($type.Namespace) -ne $Namespace -or $reader.GetString($type.Name) -ne $TypeName) { continue }
            foreach ($fieldHandle in $type.GetFields()) {
                $field = $reader.GetFieldDefinition($fieldHandle)
                if ($reader.GetString($field.Name) -ne 'Version') { continue }
                $constant = $reader.GetConstant($field.GetDefaultValue())
                if ($constant.TypeCode -ne [System.Reflection.Metadata.ConstantTypeCode]::String) { throw 'Version must be a string constant.' }
                $blob = $reader.GetBlobReader($constant.Value)
                return $blob.ReadUTF16($blob.Length)
            }
        }
        throw "Version constant not found: $Namespace.$TypeName"
    } finally { $pe.Dispose(); $stream.Dispose() }
}

function Get-ReleaseFiles([string] $Root) {
    $result = foreach ($file in (Get-ChildItem -LiteralPath $Root -File -Recurse)) {
        [ordered]@{ Path = $file.FullName.Substring($Root.Length + 1).Replace('\', '/'); SHA256 = Get-ReleaseHash $file.FullName }
    }
    @($result | Sort-Object -Property Path -CaseSensitive -Culture '' )
}

function Get-SourceFingerprint($Files) {
    $lines = @($Files | ForEach-Object { $_.Path + "`t" + $_.SHA256 }) -join "`n"
    $sha = [Security.Cryptography.SHA256]::Create()
    try { [Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($lines))) }
    finally { $sha.Dispose() }
}

function Write-ReleaseJson([string] $Path, $Value) {
    $json = $Value | ConvertTo-Json -Depth 15
    [IO.File]::WriteAllText($Path, $json.Replace("`r`n", "`n") + "`n", [Text.UTF8Encoding]::new($false))
}

function Get-GameplaySourceFingerprint([string] $ProjectRoot) {
    $sourceRoot = Join-Path $ProjectRoot 'src'
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $files = @(Get-ChildItem -LiteralPath $sourceRoot -File -Filter '*.cs' | ForEach-Object {
            $text = [IO.File]::ReadAllText($_.FullName).Replace("`r`n", "`n")
            if ($_.Name -ceq 'Plugin.cs') {
                $text = [regex]::Replace($text, 'public const string Version = "[^"]+";', 'public const string Version = "<release-version>";')
            }
            [ordered]@{ Path = 'src/' + $_.Name; SHA256 = [Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text))) }
        } | Sort-Object -Property Path -CaseSensitive -Culture '')
        if ($files.Count -eq 0) { throw 'Gameplay source files missing.' }
        $projectText = [IO.File]::ReadAllText((Join-Path $ProjectRoot 'EpicLootSpawner.csproj')).Replace("`r`n", "`n")
        $projectText = [regex]::Replace($projectText, '<Version>[^<]+</Version>', '<Version>&lt;release-version&gt;</Version>')
        $files += [ordered]@{ Path = 'EpicLootSpawner.csproj'; SHA256 = [Convert]::ToHexString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($projectText))) }
        Get-SourceFingerprint @($files | Sort-Object -Property Path -CaseSensitive -Culture '')
    } finally { $sha.Dispose() }
}

function New-DeterministicArchive([string] $Root, [string] $Path) {
    $stream = [IO.File]::Open($Path, [IO.FileMode]::CreateNew)
    $archive = [IO.Compression.ZipArchive]::new($stream, [IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        foreach ($file in (Get-ReleaseFiles $Root)) {
            $entry = $archive.CreateEntry($file.Path, [IO.Compression.CompressionLevel]::Optimal)
            $entry.LastWriteTime = [DateTimeOffset]::new(1980, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
            $entry.ExternalAttributes = 0
            $input = [IO.File]::OpenRead((Join-Path $Root $file.Path))
            $output = $entry.Open()
            try { $input.CopyTo($output) } finally { $input.Dispose(); $output.Dispose() }
        }
    } finally { $archive.Dispose(); $stream.Dispose() }
}

function Test-ReleaseArchive([string] $Path, $ExpectedFiles, [string] $ManifestJson) {
    $archive = [IO.Compression.ZipFile]::OpenRead($Path)
    try {
        $expected = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::Ordinal)
        foreach ($file in $ExpectedFiles) { $expected.Add($file.Path, $file.SHA256) }
        $seen = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        if ($archive.Entries.Count -ne $expected.Count + 1) { throw 'Unexpected archive file count.' }
        foreach ($entry in $archive.Entries) {
            if (-not $seen.Add($entry.FullName)) { throw 'Duplicate archive entry.' }
            if ($entry.FullName -eq 'RELEASE.json') {
                $reader = [IO.StreamReader]::new($entry.Open())
                try { if ($reader.ReadToEnd() -cne $ManifestJson) { throw 'Embedded release manifest differs.' } }
                finally { $reader.Dispose() }
                continue
            }
            if (-not $expected.ContainsKey($entry.FullName)) { throw "Unexpected archive entry: $($entry.FullName)" }
            $stream = $entry.Open(); $sha = [Security.Cryptography.SHA256]::Create()
            try { $hash = [Convert]::ToHexString($sha.ComputeHash($stream)) }
            finally { $sha.Dispose(); $stream.Dispose() }
            if ($hash -cne $expected[$entry.FullName]) { throw "Archive hash mismatch: $($entry.FullName)" }
        }
        if (-not $seen.Contains('RELEASE.json')) { throw 'Release manifest missing.' }
    } finally { $archive.Dispose() }
}
