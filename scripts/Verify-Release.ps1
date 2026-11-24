#requires -Version 7.2
[CmdletBinding()]
param([Parameter(Mandatory)][string] $ReleaseDirectory)
$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'ReleaseTools.ps1')
$ReleaseDirectory = [IO.Path]::GetFullPath($ReleaseDirectory)
$json = [IO.File]::ReadAllText((Join-Path $ReleaseDirectory 'RELEASE.json'))
$manifest = $json | ConvertFrom-Json
if ($manifest.Format -ne 'EpicLootSpawner.Release' -or $manifest.SchemaVersion -ne 1) { throw 'Unsupported release manifest.' }
if ($manifest.Version -notmatch '^\d+\.\d+\.\d+$') { throw 'Invalid release version.' }
if ((Get-SourceFingerprint $manifest.SourceFiles) -cne $manifest.SourceFingerprint) { throw 'Source fingerprint mismatch.' }
$dllEntry = @($manifest.RuntimeFiles | Where-Object { $_.Path -ceq 'BepInEx/plugins/EpicLootSpawner/EpicLootSpawner.dll' })
if ($dllEntry.Count -ne 1 -or $dllEntry[0].SHA256 -cne $manifest.DLLSHA256) { throw 'DLL manifest mismatch.' }
Test-ReleaseArchive (Join-Path $ReleaseDirectory "EpicLootSpawner-$($manifest.Version).zip") $manifest.RuntimeFiles $json
Test-ReleaseArchive (Join-Path $ReleaseDirectory "EpicLootSpawner-$($manifest.Version)-source.zip") $manifest.SourceFiles $json
$checks = ($manifest.Tests | Measure-Object -Property Checks -Sum).Sum
Write-Host "Verified: version $($manifest.Version), $checks recorded checks, both ZIPs and every file hash."
Write-Host 'Hashes confirm file consistency; they are not a digital signature or gameplay validation.'
