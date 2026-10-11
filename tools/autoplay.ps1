<#
.SYNOPSIS
  Builds the Windows player and runs it with -autoplay so RoninAutopilot self-plays the demo: walk to
  each enemy via the real joystick pointer handlers, win each duel via the real swipe pointer handlers.
  Exits 0 only if every enemy was defeated. No touch hardware or human required.
.EXAMPLE
  pwsh tools/autoplay.ps1
  pwsh tools/autoplay.ps1 -SkipBuild   # reuse the last built player
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

New-Item -ItemType Directory -Force "$root/Logs" | Out-Null
$logFile = "$root/Logs/autoplay.log"
$p = Start-Process -FilePath $exe -ArgumentList @("-batchmode", "-nographics", "-autoplay", "-logFile", "`"$logFile`"") -Wait -PassThru -NoNewWindow

Select-String -Path $logFile -Pattern '\[UnityRonin\]|error CS|Exception' | Select-Object -First 80 | ForEach-Object { $_.Line }

$passed = Select-String -Path $logFile -Pattern '\[UnityRonin\] autoplay PASS' -Quiet
Write-Host "exit=$($p.ExitCode)  autoplayPass=$passed  log=$logFile"
if ($p.ExitCode -ne 0 -or -not $passed) { exit 1 }
exit 0
