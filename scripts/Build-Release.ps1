#requires -Version 7.2
[CmdletBinding()]
param(
    [string] $GamePath,
    [string] $ProfilePath,
    [string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReleaseTools.ps1')
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $projectRoot 'EpicLootSpawner.csproj'
[xml] $project = Get-Content -LiteralPath $projectPath -Raw
$version = $project.SelectSingleNode('/Project/PropertyGroup/Version').InnerText
if ($version -notmatch '^\d+\.\d+\.\d+$') { throw 'Release version must use major.minor.patch.' }
$pluginText = Get-Content -LiteralPath (Join-Path $projectRoot 'src/Plugin.cs') -Raw
$pluginMatch = [regex]::Match($pluginText, 'public const string Version = "([^"]+)";')
if (-not $pluginMatch.Success -or $pluginMatch.Groups[1].Value -cne $version) { throw 'Plugin.cs and project versions differ.' }
if (-not $GamePath) { $GamePath = $project.SelectSingleNode('/Project/PropertyGroup/GamePath').InnerText }
if (-not $ProfilePath) { $ProfilePath = $project.SelectSingleNode('/Project/PropertyGroup/ProfilePath').InnerText.Replace('$(APPDATA)', $env:APPDATA) }
$GamePath = [IO.Path]::GetFullPath($GamePath)
$ProfilePath = [IO.Path]::GetFullPath($ProfilePath)
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $projectRoot ('artifacts/' + $version) }
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Output directory already exists. Choose a new directory; previous releases are preserved.' }

$dependencyPaths = @{}
$dependencies = foreach ($reference in $project.SelectNodes('/Project/ItemGroup/Reference')) {
    $hint = $reference.SelectSingleNode('HintPath').InnerText.Replace('$(GamePath)', $GamePath).Replace('$(ProfilePath)', $ProfilePath)
    if (-not (Test-Path -LiteralPath $hint -PathType Leaf)) { throw "Dependency missing: $hint" }
    $dependencyPaths[$reference.Include] = $hint
    $info = [Diagnostics.FileVersionInfo]::GetVersionInfo($hint)
    [ordered]@{ Name = $reference.Include; SHA256 = Get-ReleaseHash $hint; FileVersion = $info.FileVersion; ProductVersion = $info.ProductVersion }
}
$epicLootPath = Join-Path $ProfilePath 'BepInEx/plugins/RandyKnapp-EpicLoot/EpicLoot.dll'
$epicLootVersion = Get-ManagedVersionConstant $epicLootPath 'EpicLoot' 'EpicLoot'
if ($epicLootVersion -ne '0.15.6') { throw "Target EpicLoot 0.15.6 required; actual plugin version is $epicLootVersion. Other versions are unverified." }
$sdk = (& dotnet --version | Out-String).Trim()
if ($LASTEXITCODE -ne 0 -or $sdk -notmatch '^8\.') { throw '.NET 8 SDK required for this release workflow.' }

# Whitelist distributable files; game dependencies, build outputs and private config are excluded.
$docs = @('README.md', 'CHANGELOG.md', 'ROADMAP.md', 'TESTPLAN.md', 'PERFORMANCE.md', 'BUILDING.md', 'COMPATIBILITY.md', 'INSTALLING.md', 'UI-CHANGES.md', 'CODE-REVIEW.md')
foreach ($optionalDoc in @('icon.png', 'manifest.json', 'MOD-PAGE.md')) {
    if (Test-Path -LiteralPath (Join-Path $projectRoot $optionalDoc) -PathType Leaf) { $docs += $optionalDoc }
}
if (Test-Path -LiteralPath (Join-Path $projectRoot 'manifest.json')) {
    $packageInfo = Get-Content -LiteralPath (Join-Path $projectRoot 'manifest.json') -Raw | ConvertFrom-Json
    if ($packageInfo.name -cne 'EpicLootSpawner' -or $packageInfo.version_number -cne $version) { throw 'Mod-manager manifest version or name differs.' }
}
$acceptance = $null
$acceptancePath = Join-Path $projectRoot 'ACCEPTANCE.json'
if (Test-Path -LiteralPath $acceptancePath -PathType Leaf) {
    $acceptance = Get-Content -LiteralPath $acceptancePath -Raw | ConvertFrom-Json
    if ($acceptance.Format -cne 'EpicLootSpawner.Acceptance' -or $acceptance.SchemaVersion -ne 1 -or $acceptance.Kind -cne 'UserReportedBaseline') { throw 'Unsupported gameplay acceptance record.' }
    if ($acceptance.ReleaseVersion -cne $version) { throw 'Gameplay acceptance version differs from release.' }
    if ((Get-GameplaySourceFingerprint $projectRoot) -cne $acceptance.GameplaySourceFingerprint) { throw 'Gameplay source differs from user-reported baseline.' }
    if (@($acceptance.BaselineDependencies).Count -ne @($dependencies).Count) { throw 'Gameplay dependencies differ from user-reported baseline.' }
    foreach ($baselineDependency in $acceptance.BaselineDependencies) {
        $matching = @($dependencies | Where-Object { $_.Name -ceq $baselineDependency.Name })
        if ($matching.Count -ne 1 -or $matching[0].SHA256 -cne $baselineDependency.SHA256) { throw 'Gameplay dependencies differ from user-reported baseline.' }
    }
    $docs += 'ACCEPTANCE.json'
}
$sourcePaths = @('EpicLootSpawner.csproj') + $docs
foreach ($folder in @('src', 'tests', 'scripts')) {
    $sourcePaths += @(Get-ChildItem -LiteralPath (Join-Path $projectRoot $folder) -Recurse -File |
        Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' } |
        ForEach-Object { $_.FullName.Substring($projectRoot.Length + 1).Replace('\', '/') })
}
$sourcePaths = @($sourcePaths | Sort-Object -CaseSensitive -Culture '' -Unique)
$sourceFiles = @($sourcePaths | ForEach-Object { [ordered]@{ Path = $_; SHA256 = Get-ReleaseHash (Join-Path $projectRoot $_) } })
$sourceFingerprint = Get-SourceFingerprint $sourceFiles

function Invoke-ReleaseDotnet([string[]] $Arguments) {
    $lines = @(& dotnet @Arguments 2>&1 | ForEach-Object { $_.ToString() })
    if ($LASTEXITCODE -ne 0) { throw ($lines -join "`n") }
    foreach ($line in $lines) { Write-Host $line }
    return $lines
}

Write-Host "Building EpicLoot Spawner $version against EpicLoot $epicLootVersion..."
$buildArgs = @('build', $projectPath, '-c', 'Release', '--nologo', '--no-incremental',
    "-p:GamePath=$GamePath", "-p:ProfilePath=$ProfilePath", '-p:Deterministic=true',
    "-p:PathMap=$projectRoot=/_/EpicLootSpawner", '-p:ContinuousIntegrationBuild=true')
$buildLines = Invoke-ReleaseDotnet $buildArgs
$tests = @()
$testLogs = [ordered]@{}
# LiveCapChecks loads the DLL built above with the installed EpicLoot/Harmony/game assemblies.
$env:GamePath = $GamePath
$env:ProfilePath = $ProfilePath
# Run net8.0 tests through dotnet <dll> instead of a fresh unsigned apphost .exe, which
# Windows Smart App Control may block. Set as an environment property so both the build
# and the run step of `dotnet run` see it.
$env:UseAppHost = 'false'
foreach ($test in @('tests/SpawnerChecks.csproj', 'tests/BehaviorChecks/BehaviorChecks.csproj', 'tests/RuntimeChecks/RuntimeChecks.csproj', 'tests/PanelChecks/PanelChecks.csproj', 'tests/LiveCapChecks/LiveCapChecks.csproj')) {
    $lines = Invoke-ReleaseDotnet @('run', '--project', (Join-Path $projectRoot $test), '-c', 'Release', '--nologo')
    $passes = @($lines | Where-Object { $_ -match '^PASS: \d+ ' })
    if ($passes.Count -ne 1) { throw "Test result missing or ambiguous: $test" }
    $count = [int]([regex]::Match($passes[0], '^PASS: (\d+) ').Groups[1].Value)
    if ($count -le 0) { throw "Empty test suite: $test" }
    $tests += [ordered]@{ Project = $test; Checks = $count; Result = 'PASS' }
    $testLogs[$test] = $lines
}
foreach ($file in $sourceFiles) {
    if ((Get-ReleaseHash (Join-Path $projectRoot $file.Path)) -cne $file.SHA256) { throw "Source changed during build: $($file.Path)" }
}
foreach ($dependency in $dependencies) {
    if ((Get-ReleaseHash $dependencyPaths[$dependency.Name]) -cne $dependency.SHA256) { throw "Dependency changed during build: $($dependency.Name)" }
}
$dll = Join-Path $projectRoot 'bin/Release/net481/EpicLootSpawner.dll'
$dllVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo($dll)
if ($dllVersion.ProductVersion -ne $version -or $dllVersion.FileVersion -ne "$version.0" -or (Get-ManagedVersionConstant $dll 'EpicLootSpawner' 'Plugin') -ne $version) { throw 'Built DLL version differs from source.' }

# Output is created only after prerequisites, build, tests and source checks have passed.
$runtimeStage = Join-Path $OutputDirectory 'runtime'
$sourceStage = Join-Path $OutputDirectory 'source'
$pluginDirectory = Join-Path $runtimeStage 'BepInEx/plugins/EpicLootSpawner'
New-Item -ItemType Directory -Path $pluginDirectory, $sourceStage | Out-Null
Copy-Item -LiteralPath $dll -Destination $pluginDirectory
foreach ($doc in $docs) { Copy-Item -LiteralPath (Join-Path $projectRoot $doc) -Destination $runtimeStage }
if (Test-Path -LiteralPath (Join-Path $projectRoot 'MOD-PAGE.md')) { Copy-Item -LiteralPath (Join-Path $projectRoot 'MOD-PAGE.md') -Destination (Join-Path $runtimeStage 'README.md') }
New-Item -ItemType Directory -Path (Join-Path $runtimeStage 'scripts') | Out-Null
foreach ($script in @('Install-Release.ps1', 'InstallTools.ps1', 'ReleaseTools.ps1')) {
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot $script) -Destination (Join-Path $runtimeStage 'scripts')
}
foreach ($file in $sourceFiles) {
    $target = Join-Path $sourceStage $file.Path
    $parent = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    Copy-Item -LiteralPath (Join-Path $projectRoot $file.Path) -Destination $target
    if ((Get-ReleaseHash $target) -cne $file.SHA256) { throw "Staged source differs: $($file.Path)" }
}
$runtimeFiles = Get-ReleaseFiles $runtimeStage
$manifest = [ordered]@{
    Format = 'EpicLootSpawner.Release'; SchemaVersion = 1; Version = $version
    SDK = $sdk; TargetFramework = 'net481'; EpicLootPluginVersion = $epicLootVersion
    SourceFingerprint = $sourceFingerprint; DLLSHA256 = Get-ReleaseHash $dll
    Dependencies = @($dependencies | Sort-Object -Property Name -CaseSensitive -Culture '')
    Tests = $tests; GameplayValidation = $(if ($acceptance) { $acceptance.Kind } else { 'Pending' })
    GameplayEvidence = $acceptance; RuntimeFiles = @($runtimeFiles); SourceFiles = $sourceFiles
}
$manifestPath = Join-Path $OutputDirectory 'RELEASE.json'
Write-ReleaseJson $manifestPath $manifest
Copy-Item -LiteralPath $manifestPath -Destination $runtimeStage
Copy-Item -LiteralPath $manifestPath -Destination $sourceStage
$runtimeZip = Join-Path $OutputDirectory "EpicLootSpawner-$version.zip"
$sourceZip = Join-Path $OutputDirectory "EpicLootSpawner-$version-source.zip"
New-DeterministicArchive $runtimeStage $runtimeZip
New-DeterministicArchive $sourceStage $sourceZip
& (Join-Path $PSScriptRoot 'Verify-Release.ps1') -ReleaseDirectory $OutputDirectory
Write-ReleaseJson (Join-Path $OutputDirectory 'BUILD-REPORT.json') ([ordered]@{
    BuiltAtUTC = [DateTime]::UtcNow.ToString('o'); BuildOutput = $buildLines; TestOutput = $testLogs
})
Write-Host "Release ready: $OutputDirectory"
