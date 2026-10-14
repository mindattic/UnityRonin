<#
.SYNOPSIS
  Builds the Windows player and launches it with -demo (windowed, not headless): RoninAutopilot self-plays
  the demo scene via the real VirtualJoystick/SwipeInputReader pointer-handler methods, then quits with
  PASS/FAIL. Same tour as tools/autoplay.ps1, just watchable instead of headless.
.EXAMPLE
  pwsh tools/demo.ps1
  pwsh tools/demo.ps1 -SkipBuild   # reuse the last built player
#>
param([switch]$SkipBuild)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

if (-not $SkipBuild) {
    & "$PSScriptRoot/unity.ps1" -Method UnityRonin.Editor.RoninBuild.BuildWindows -Log build-player
    if ($LASTEXITCODE -ne 0) { Write-Host "FAIL player build"; exit $LASTEXITCODE }
}

$exe = "$root/Build/Windows/UnityRonin.exe"
if (-not (Test-Path $exe)) { throw "Player not found at $exe; run without -SkipBuild first." }

Start-Process -FilePath $exe -ArgumentList @("-demo")
Write-Host "Launched $exe -demo (close its window when done watching)"
