#requires -Version 7.2
Set-StrictMode -Version Latest
. (Join-Path $PSScriptRoot 'ReleaseTools.ps1')

function Get-InstallationPlan([string] $PackagePath, [string] $ProfilePath) {
    $PackagePath = [IO.Path]::GetFullPath($PackagePath)
    $ProfilePath = [IO.Path]::GetFullPath($ProfilePath)
    if (-not (Test-Path -LiteralPath (Join-Path $ProfilePath 'BepInEx/core/BepInEx.dll') -PathType Leaf)) { throw 'BepInEx profile not found. Choose the profile root directory.' }
    $archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        $entry = $archive.GetEntry('RELEASE.json')
        if ($null -eq $entry) { throw 'This installer requires a manifest-bearing runtime ZIP.' }
        $reader = [IO.StreamReader]::new($entry.Open())
        try { $json = $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally { $archive.Dispose() }
    $manifest = $json | ConvertFrom-Json
    if ($manifest.Format -ne 'EpicLootSpawner.Release' -or $manifest.SchemaVersion -ne 1 -or $manifest.Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Unsupported release manifest.' }
    $dllEntry = @($manifest.RuntimeFiles | Where-Object { $_.Path -ceq 'BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll' })
    if ($dllEntry.Count -ne 1 -or $dllEntry[0].SHA256 -cne $manifest.DLLSHA256) { throw 'DLL manifest mismatch.' }
    Test-ReleaseArchive $PackagePath $manifest.RuntimeFiles $json
    $epicLoot = Join-Path $ProfilePath 'BepInEx/plugins/RandyKnapp-EpicLoot/EpicLoot.dll'
    if (-not (Test-Path -LiteralPath $epicLoot -PathType Leaf)) { throw 'EpicLoot DLL not found in target profile.' }
    $actualVersion = Get-ManagedVersionConstant $epicLoot 'EpicLoot' 'EpicLoot'
    if ($actualVersion -ne $manifest.EpicLootPluginVersion -or $actualVersion -ne '0.15.6') { throw "Target profile EpicLoot version mismatch: $actualVersion" }
    if (-not (Test-Path -LiteralPath (Join-Path $ProfilePath 'BepInEx/plugins/ValheimModding-Jotunn/Jotunn.dll') -PathType Leaf)) { throw 'Jotunn DLL not found in target profile.' }
    $plugins = Join-Path $ProfilePath 'BepInEx/plugins'
    $target = Join-Path $plugins 'EpicLootSpawner/EpicLootSpawner.dll'
    foreach ($path in @($ProfilePath, (Join-Path $ProfilePath 'BepInEx'), $plugins, (Split-Path -Parent $target), $target, (Join-Path $ProfilePath 'BepInEx/config'), (Join-Path $ProfilePath 'BepInEx/config/EpicLootSpawnerBackups'))) {
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Linked installation path is unsupported: $path" }
    }
    $duplicates = @(foreach ($file in (Get-ChildItem -LiteralPath $plugins -Recurse -File -Filter '*.dll')) {
        if ($file.FullName -ieq $target) { continue }
        $isSpawner = $file.Name -ieq 'EpicLootSpawner.dll'
        if (-not $isSpawner) { try { $null = Get-ManagedVersionConstant $file.FullName 'EpicLootSpawner' 'Plugin'; $isSpawner = $true } catch { } }
        if ($isSpawner) { $file.FullName }
    })
    [pscustomobject]@{
        Version = $manifest.Version; PackagePath = $PackagePath; PackageSHA256 = Get-ReleaseHash $PackagePath
        ProfilePath = $ProfilePath; TargetPath = $target; DLLSHA256 = $manifest.DLLSHA256
        PreviousSHA256 = if (Test-Path -LiteralPath $target -PathType Leaf) { Get-ReleaseHash $target } else { $null }
        Duplicates = $duplicates
    }
}

function Install-VerifiedDll($Plan) {
    if ($Plan.Duplicates.Count -gt 0) { throw ('Duplicate spawner DLLs found. Resolve these paths first: ' + ($Plan.Duplicates -join ', ')) }
    # Recheck the entire package/profile immediately before writing.
    $fresh = Get-InstallationPlan $Plan.PackagePath $Plan.ProfilePath
    if ($fresh.PreviousSHA256 -cne $Plan.PreviousSHA256 -or $fresh.PackageSHA256 -cne $Plan.PackageSHA256) { throw 'Package or installed DLL changed after preview. Preview again.' }
    if ($fresh.Duplicates.Count -gt 0) { throw 'Duplicate spawner DLL appeared after preview.' }
    $parent = Split-Path -Parent $fresh.TargetPath
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    $temporary = Join-Path $parent ('EpicLootSpawner.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    $backup = $null
    try {
        $archive = [IO.Compression.ZipFile]::OpenRead($fresh.PackagePath)
        try {
            $input = $archive.GetEntry('BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll').Open()
            try {
                $output = [IO.File]::Open($temporary, [IO.FileMode]::CreateNew)
                try { $input.CopyTo($output) } finally { $output.Dispose() }
            } finally { $input.Dispose() }
        } finally { $archive.Dispose() }
        if ((Get-ReleaseHash $temporary) -cne $fresh.DLLSHA256 -or (Get-ManagedVersionConstant $temporary 'EpicLootSpawner' 'Plugin') -ne $fresh.Version) { throw 'Extracted DLL hash/version mismatch.' }
        $currentHash = if (Test-Path -LiteralPath $fresh.TargetPath -PathType Leaf) { Get-ReleaseHash $fresh.TargetPath } else { $null }
        if ($currentHash -cne $fresh.PreviousSHA256) { throw 'Installed DLL changed during preparation.' }
        if ($fresh.PreviousSHA256) {
            $backupDirectory = Join-Path $fresh.ProfilePath ('BepInEx/config/EpicLootSpawnerBackups/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss') + '-' + [Guid]::NewGuid().ToString('N'))
            New-Item -ItemType Directory -Path $backupDirectory | Out-Null
            $backup = Join-Path $backupDirectory 'EpicLootSpawner.dll'
            [IO.File]::Replace($temporary, $fresh.TargetPath, $backup)
        } else { [IO.File]::Move($temporary, $fresh.TargetPath) }
        [pscustomobject]@{ Version = $fresh.Version; InstalledDLL = $fresh.TargetPath; PreviousDLLBackup = $backup; SHA256 = Get-ReleaseHash $fresh.TargetPath }
    } finally {
        # Delete only this newly created temporary file, never a directory or user DLL.
        if ([IO.File]::Exists($temporary)) { [IO.File]::Delete($temporary) }
    }
}
