#requires -Version 7.2
[CmdletBinding()]
param([Parameter(Mandatory)][string] $OutputDirectory)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $projectRoot 'scripts/ReleaseTools.ps1')
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new check output directory.' }
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$checks = 0
function Assert-Workflow([bool] $Condition, [string] $Label) {
    if (-not $Condition) { throw $Label }
    $script:checks++
    Write-Host "PASS: $Label"
}
function Invoke-CheckProcess([string[]] $Arguments, [bool] $ShouldPass, [string] $ExpectedError = '') {
    $lines = @(& pwsh -NoProfile -File @Arguments 2>&1 | ForEach-Object { $_.ToString() })
    $code = $LASTEXITCODE
    if ($ShouldPass -and $code -ne 0) { throw ($lines -join "`n") }
    if (-not $ShouldPass -and ($code -eq 0 -or ($lines -join "`n") -notmatch $ExpectedError)) { throw ('Expected rejection missing: ' + ($lines -join "`n")) }
}
$build = Join-Path $projectRoot 'scripts/Build-Release.ps1'
$verify = Join-Path $projectRoot 'scripts/Verify-Release.ps1'
$first = Join-Path $OutputDirectory 'first'
$second = Join-Path $OutputDirectory 'second'
Write-Host 'Building twice to check reproducibility (both builds run all four source test suites)...'
Invoke-CheckProcess @($build, '-OutputDirectory', $first) $true
Invoke-CheckProcess @($build, '-OutputDirectory', $second) $true
$manifest = Get-Content -LiteralPath (Join-Path $first 'RELEASE.json') -Raw | ConvertFrom-Json
$version = $manifest.Version
foreach ($path in @("EpicLootSpawner-$version.zip", "EpicLootSpawner-$version-source.zip", 'RELEASE.json', 'runtime/BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll')) {
    Assert-Workflow ((Get-ReleaseHash (Join-Path $first $path)) -ceq (Get-ReleaseHash (Join-Path $second $path))) "identical independent builds: $path"
}
$before = Get-ReleaseHash (Join-Path $first "EpicLootSpawner-$version.zip")
Invoke-CheckProcess @($build, '-OutputDirectory', $first) $false 'Output directory already exists'
Assert-Workflow ((Get-ReleaseHash (Join-Path $first "EpicLootSpawner-$version.zip")) -ceq $before) 'existing release is preserved'
$missing = Join-Path $OutputDirectory 'missing-output'
Invoke-CheckProcess @($build, '-OutputDirectory', $missing, '-GamePath', (Join-Path $OutputDirectory 'missing-game')) $false 'Dependency missing'
Assert-Workflow (-not (Test-Path -LiteralPath $missing)) 'missing dependency creates no release'

$mismatch = Join-Path $OutputDirectory 'mismatch-project'
New-Item -ItemType Directory -Path (Join-Path $mismatch 'scripts'), (Join-Path $mismatch 'src') | Out-Null
Copy-Item -LiteralPath $build -Destination (Join-Path $mismatch 'scripts')
Copy-Item -LiteralPath (Join-Path $projectRoot 'scripts/ReleaseTools.ps1') -Destination (Join-Path $mismatch 'scripts')
Copy-Item -LiteralPath (Join-Path $projectRoot 'EpicLootSpawner.csproj') -Destination $mismatch
$plugin = [IO.File]::ReadAllText((Join-Path $projectRoot 'src/Plugin.cs')).Replace("public const string Version = `"$version`";", 'public const string Version = "99.99.99";')
[IO.File]::WriteAllText((Join-Path $mismatch 'src/Plugin.cs'), $plugin)
$mismatchOutput = Join-Path $OutputDirectory 'mismatch-output'
Invoke-CheckProcess @((Join-Path $mismatch 'scripts/Build-Release.ps1'), '-OutputDirectory', $mismatchOutput) $false 'versions differ'
Assert-Workflow (-not (Test-Path -LiteralPath $mismatchOutput)) 'version mismatch creates no release'

if ($manifest.GameplayValidation -ceq 'UserReportedBaseline') {
    Assert-Workflow ((Get-GameplaySourceFingerprint (Join-Path $first 'source')) -ceq $manifest.GameplayEvidence.GameplaySourceFingerprint) 'packaged source matches user-reported baseline'
    $changed = Join-Path $OutputDirectory 'changed-gameplay-project'
    Copy-Item -LiteralPath (Join-Path $first 'source') -Destination $changed -Recurse
    $fieldPath = Join-Path $changed 'src/FieldValidation.cs'
    $originalFields = [IO.File]::ReadAllText($fieldPath)
    $changedFields = $originalFields.Replace('Rules.Whole(recipe.Quality, 1,', 'Rules.Whole(recipe.Quality, 2,')
    if ($changedFields -ceq $originalFields) { throw 'Gameplay negative control was not changed.' }
    [IO.File]::WriteAllText($fieldPath, $changedFields, [Text.UTF8Encoding]::new($false))
    $changedOutput = Join-Path $OutputDirectory 'changed-gameplay-output'
    Invoke-CheckProcess @((Join-Path $changed 'scripts/Build-Release.ps1'), '-OutputDirectory', $changedOutput) $false 'Gameplay source differs'
    Assert-Workflow (-not (Test-Path -LiteralPath $changedOutput)) 'changed gameplay cannot inherit user acceptance'
    [IO.File]::WriteAllText($fieldPath, $originalFields, [Text.UTF8Encoding]::new($false))
    $wrongAcceptance = Get-Content -LiteralPath (Join-Path $changed 'ACCEPTANCE.json') -Raw | ConvertFrom-Json
    $wrongAcceptance.ReleaseVersion = '99.99.99'
    Write-ReleaseJson (Join-Path $changed 'ACCEPTANCE.json') $wrongAcceptance
    $wrongOutput = Join-Path $OutputDirectory 'wrong-acceptance-output'
    Invoke-CheckProcess @((Join-Path $changed 'scripts/Build-Release.ps1'), '-OutputDirectory', $wrongOutput) $false 'Gameplay acceptance version differs'
    Assert-Workflow (-not (Test-Path -LiteralPath $wrongOutput)) 'wrong acceptance version creates no release'
    $wrongAcceptance.ReleaseVersion = $version
    $wrongAcceptance.BaselineDependencies[0].SHA256 = '0' * 64
    Write-ReleaseJson (Join-Path $changed 'ACCEPTANCE.json') $wrongAcceptance
    $dependencyOutput = Join-Path $OutputDirectory 'changed-dependency-output'
    Invoke-CheckProcess @((Join-Path $changed 'scripts/Build-Release.ps1'), '-OutputDirectory', $dependencyOutput) $false 'Gameplay dependencies differ'
    Assert-Workflow (-not (Test-Path -LiteralPath $dependencyOutput)) 'different dependencies cannot inherit user acceptance'
}

foreach ($suffix in @('', '-source')) {
    $bad = Join-Path $OutputDirectory ('tampered' + $suffix)
    New-Item -ItemType Directory -Path $bad | Out-Null
    foreach ($name in @('RELEASE.json', "EpicLootSpawner-$version.zip", "EpicLootSpawner-$version-source.zip")) {
        Copy-Item -LiteralPath (Join-Path $first $name) -Destination $bad
    }
    $zip = [IO.Compression.ZipFile]::Open((Join-Path $bad "EpicLootSpawner-$version$suffix.zip"), [IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry = $zip.GetEntry('README.md'); $entry.Delete()
        $writer = [IO.StreamWriter]::new($zip.CreateEntry('README.md').Open())
        try { $writer.Write('tampered') } finally { $writer.Dispose() }
    } finally { $zip.Dispose() }
    Invoke-CheckProcess @($verify, '-ReleaseDirectory', $bad) $false 'Archive hash mismatch'
    Assert-Workflow $true "tampered$suffix ZIP rejected"
}
Write-ReleaseJson (Join-Path $OutputDirectory 'WORKFLOW-CHECKS.json') ([ordered]@{
    Checks = $checks; Result = 'PASS'; SourceFingerprint = $manifest.SourceFingerprint
    RuntimeZipSHA256 = Get-ReleaseHash (Join-Path $first "EpicLootSpawner-$version.zip")
    SourceZipSHA256 = Get-ReleaseHash (Join-Path $first "EpicLootSpawner-$version-source.zip")
})
Write-Host "PASS: $checks release workflow checks; no installed game/profile writes."
