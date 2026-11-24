#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $PackagePath,
    [Parameter(Mandatory)][string] $EpicLootAssembly,
    [Parameter(Mandatory)][string] $PreviousSpawnerAssembly,
    [Parameter(Mandatory)][string] $OutputDirectory
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $projectRoot 'scripts/InstallTools.ps1')
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw 'Choose a new isolated check directory.' }
$profile = Join-Path $OutputDirectory 'mock-profile'
New-Item -ItemType Directory -Path (Join-Path $profile 'BepInEx/core'), (Join-Path $profile 'BepInEx/plugins/RandyKnapp-EpicLoot'), (Join-Path $profile 'BepInEx/plugins/ValheimModding-Jotunn'), (Join-Path $profile 'BepInEx/config') | Out-Null
Copy-Item -LiteralPath $EpicLootAssembly -Destination (Join-Path $profile 'BepInEx/plugins/RandyKnapp-EpicLoot/EpicLoot.dll')
[IO.File]::WriteAllText((Join-Path $profile 'BepInEx/core/BepInEx.dll'), 'dependency presence fixture')
[IO.File]::WriteAllText((Join-Path $profile 'BepInEx/plugins/ValheimModding-Jotunn/Jotunn.dll'), 'dependency presence fixture')
$config = Join-Path $profile 'BepInEx/config/EpicLootSpawner.presets.json'
[IO.File]::WriteAllText($config, '{"user-data":"preserve"}')
$configHash = Get-ReleaseHash $config
$checks = 0
function Assert-Installer([bool] $Value, [string] $Label) {
    if (-not $Value) { throw $Label }
    $script:checks++; Write-Host "PASS: $Label"
}
function Assert-Rejected([scriptblock] $Action, [string] $Expected, [string] $Label) {
    $errorText = $null
    try { & $Action | Out-Null } catch { $errorText = $_.Exception.Message }
    Assert-Installer ($null -ne $errorText -and $errorText -match $Expected) $Label
}
$plan = Get-InstallationPlan $PackagePath $profile
Assert-Installer (-not (Test-Path -LiteralPath $plan.TargetPath)) 'preview writes no target DLL'
$newInstall = Install-VerifiedDll $plan
Assert-Installer ($newInstall.SHA256 -ceq $plan.DLLSHA256 -and (Get-ManagedVersionConstant $newInstall.InstalledDLL 'EpicLootSpawner' 'Plugin') -eq $plan.Version) 'new installation hash and version'
Assert-Installer ($null -eq $newInstall.PreviousDLLBackup) 'first installation needs no old DLL backup'
Assert-Installer ((Get-ReleaseHash $config) -ceq $configHash) 'installation preserves user config'

Copy-Item -LiteralPath $PreviousSpawnerAssembly -Destination $plan.TargetPath
$oldHash = Get-ReleaseHash $PreviousSpawnerAssembly
$update = Install-VerifiedDll (Get-InstallationPlan $PackagePath $profile)
Assert-Installer ((Get-ReleaseHash $update.PreviousDLLBackup) -ceq $oldHash -and $update.SHA256 -ceq $plan.DLLSHA256) 'update installs new DLL and backs up exact old bytes'
Assert-Installer ((Get-ReleaseHash $config) -ceq $configHash) 'update preserves user config'
Assert-Installer (@(Get-ChildItem -LiteralPath (Split-Path -Parent $plan.TargetPath) -Filter '*.tmp').Count -eq 0) 'successful install leaves no temporary DLL'

$stalePlan = Get-InstallationPlan $PackagePath $profile
Copy-Item -LiteralPath $PreviousSpawnerAssembly -Destination $plan.TargetPath
Assert-Rejected { Install-VerifiedDll $stalePlan } 'changed after preview' 'external DLL change blocks stale plan'
Assert-Installer ((Get-ReleaseHash $plan.TargetPath) -ceq $oldHash) 'stale plan does not overwrite external change'

$duplicate = Join-Path $profile 'BepInEx/plugins/renamed-old-spawner.dll'
Copy-Item -LiteralPath $PreviousSpawnerAssembly -Destination $duplicate
$duplicatePlan = Get-InstallationPlan $PackagePath $profile
Assert-Installer ($duplicatePlan.Duplicates.Count -eq 1 -and $duplicatePlan.Duplicates[0] -eq $duplicate) 'renamed duplicate found by managed metadata'
Assert-Rejected { Install-VerifiedDll $duplicatePlan } 'Duplicate spawner DLLs' 'duplicate prevents installation'
# Remove only the one fixture file created above; keep output evidence and profile for inspection.
[IO.File]::Delete($duplicate)

Assert-Rejected { Get-InstallationPlan $PackagePath (Join-Path $OutputDirectory 'missing-profile') } 'BepInEx profile not found' 'missing profile rejected'
$epicTarget = Join-Path $profile 'BepInEx/plugins/RandyKnapp-EpicLoot/EpicLoot.dll'
Copy-Item -LiteralPath $PreviousSpawnerAssembly -Destination $epicTarget
Assert-Rejected { Get-InstallationPlan $PackagePath $profile } 'Version constant not found' 'wrong EpicLoot assembly rejected'
Copy-Item -LiteralPath $EpicLootAssembly -Destination $epicTarget

$badPackage = Join-Path $OutputDirectory 'tampered.zip'
Copy-Item -LiteralPath $PackagePath -Destination $badPackage
$zip = [IO.Compression.ZipFile]::Open($badPackage, [IO.Compression.ZipArchiveMode]::Update)
try {
    $entry = $zip.GetEntry('BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll'); $entry.Delete()
    $writer = [IO.StreamWriter]::new($zip.CreateEntry('BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll').Open())
    try { $writer.Write('tampered') } finally { $writer.Dispose() }
} finally { $zip.Dispose() }
Assert-Rejected { Get-InstallationPlan $badPackage $profile } 'Archive hash mismatch' 'tampered package rejected before installation'
Assert-Installer ((Get-ReleaseHash $plan.TargetPath) -ceq $oldHash -and (Get-ReleaseHash $config) -ceq $configHash) 'failed inspections preserve target and config'

$installScript = Join-Path $projectRoot 'scripts/Install-Release.ps1'
$output = @(& pwsh -NoProfile -File $installScript -PackagePath $PackagePath -ProfilePath $profile 2>&1)
Assert-Installer ($LASTEXITCODE -eq 0 -and ($output -join "`n") -match 'Preview only' -and (Get-ReleaseHash $plan.TargetPath) -ceq $oldHash) 'CLI defaults to read-only preview'
Write-ReleaseJson (Join-Path $OutputDirectory 'INSTALLER-CHECKS.json') ([ordered]@{ Checks = $checks; Result = 'PASS'; TargetVersion = $plan.Version; MockProfile = $profile; ActualInstalledProfileWrites = $false })
Write-Host "PASS: $checks isolated installer checks; no installed game/profile writes."
