<#
.SYNOPSIS
  Runs the built UnityRonin player with no window in the shared MindAttic headless mode (U.RON-T22).
.DESCRIPTION
  Calls ..\Unity.Shared\tools\Start-Headless.ps1, which launches the player with
  --headless -batchmode -nographics -logFile <path>. Unity.Shared's headless mode mutes the session, runs
  in the background, turns vSync off and logs one MINDATTIC_HEADLESS_READY line. The game has no menu, so
  it goes straight into its one scene as it always does. Any other arguments (e.g. -demo, to self-play the
  tour) pass through to the player.
.PARAMETER Exe
  The player to run. Default: Build\Windows\UnityRonin.exe (build it with tools\unity.ps1 -Method UnityRonin.Editor.RoninBuild.BuildWindows).
.PARAMETER Wait
  Wait for the ready line before returning (exit 1 on timeout, 2 if the player quits first).
.EXAMPLE
  pwsh tools/play-headless.ps1 -Wait
  pwsh tools/play-headless.ps1 -Wait -demo
#>
param([string]$Exe, [switch]$Wait, [Parameter(ValueFromRemainingArguments = $true)][string[]]$GameArgs)
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
if (-not $Exe) { $Exe = Join-Path $root "Build\Windows\UnityRonin.exe" }
if (-not (Test-Path $Exe)) { throw "Player not found at $Exe; build it first (tools/unity.ps1 -Method UnityRonin.Editor.RoninBuild.BuildWindows)." }
$launcher = Join-Path $root "..\Unity.Shared\tools\Start-Headless.ps1"
if (-not (Test-Path $launcher)) { throw "Unity.Shared not found next to this repo ($launcher); clone it beside UnityRonin." }
$launchArgs = @{ Exe = $Exe }
if ($Wait) { $launchArgs.Wait = $true }
if ($GameArgs) { & $launcher @launchArgs @GameArgs } else { & $launcher @launchArgs }
exit $LASTEXITCODE
