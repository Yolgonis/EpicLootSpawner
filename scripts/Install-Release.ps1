#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $PackagePath,
    [Parameter(Mandatory)][string] $ProfilePath,
    [switch] $Apply
)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'InstallTools.ps1')
$plan = Get-InstallationPlan $PackagePath $ProfilePath
Write-Host "Package: $($plan.Version)"
Write-Host "Target: $($plan.TargetPath)"
Write-Host ('Mode: ' + $(if ($plan.PreviousSHA256) { 'Update existing DLL (previous DLL will be backed up).' } else { 'New installation.' }))
if ($plan.Duplicates.Count -gt 0) { Write-Warning ('Duplicate DLL locations: ' + ($plan.Duplicates -join ', ')) }
if (-not $Apply) { Write-Host 'Preview only; no profile changes. Add -Apply to install after closing Valheim.'; return }
if (@(Get-Process -Name valheim -ErrorAction SilentlyContinue).Count -gt 0) { throw 'Close Valheim before installing.' }
Install-VerifiedDll $plan | Format-List
