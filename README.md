# Unity Ronin

A Unity 6 portrait mobile game played with one thumb: run a Samurai with a stick over the bottom half of the screen, then draw calligraphy strokes to parry and strike in a telegraphed katana duel.

![Unity 6](https://img.shields.io/badge/Unity-6000.4.3f1-black) ![C#](https://img.shields.io/badge/C%23-NUnit%20tested-239120) ![Platform](https://img.shields.io/badge/platform-Android%20%7C%20Windows-blue) ![Status](https://img.shields.io/badge/status-prototype-orange)

```text
 explore                    engage                       duel
 +--------------+           +--------------+             +--------------+
 |              |  within   |   telegraph  |  parry ->   |  slowed-time |
 |   Samurai    |  enter    |   (glyph)    |  riposte    |  riposte     |
 |              |  radius   |              |  window     |  window      |
 |  [ stick ]   | --------> | swipe anywhere| ---------> | draw a free  |
 |  bottom half |           | on screen    |             | attack       |
 +--------------+           +--------------+             +--------------+
```

The game is built from primitives (capsules, cubes, planes) so the movement and duel rules are proven before art arrives. No screenshot is shown here because the only committed captures predate the current scene; see Limitations.

## Why

- Play with one hand and no buttons: every input is a stick drag or a swipe.
- Fight by reading the enemy, not by mashing: each attack is telegraphed as a brush glyph and answered with the right shape.
- Trust the rules: movement, engagement and duel logic live in a Unity-free Core library with NUnit tests that run in seconds.
- Watch it prove itself: a self-play autopilot drives the real touch handlers, so the explore, engage, duel loop is checked without a phone.

## Features

- **Explore**: a stick that appears wherever the thumb lands in the bottom half of the screen, with a dead zone, clamped travel and camera-relative movement, followed by a smart-follow camera.
- **Engage**: coming within an enemy's enter radius starts a duel; a larger exit radius stops boundary flicker. Input swaps from the stick to full-screen swipes and movement goes automatic.
- **Duel**: five techniques (four cuts and a thrust) shared by both sides. A cut is parried by the perpendicular cross-block, a thrust only by a thrust, and an open guard is struck by matching its shape.
- **Riposte**: a parry slows time and opens a free attack window; chained strokes that alternate shape land bonus hits.
- **Calligraphy HUD**: the telegraphed technique shows as a Yuji Syuku brush glyph, coloured by attack or guard.
- **Deterministic**: the same seed replays the same sequence of beats.
- **Self-play tour**: the `-demo` flag runs an autopilot through the whole loop and exits PASS or FAIL.

## Quick start

Prerequisites: Unity 6000.4.3f1 (with Android module for phone builds), PowerShell 7 (`pwsh`) and the .NET SDK for the Core tests.

```powershell
pwsh tools/test.ps1            # Core rules under dotnet test, seconds, no Unity needed
pwsh tools/test.ps1 -Compile   # also a Unity batch-mode compile check
```

Then open the project in Unity and choose **UnityRonin > Build Demo Scene** to regenerate `Assets/UnityRonin/Scenes/Demo.unity` from primitives. You should see `PASS core tests` from the first command.

## Usage

Editor menu items under **UnityRonin**:

| Menu item | What it does |
| --- | --- |
| Build Demo Scene | Regenerates Demo.unity from primitives |
| Build Calligraphy Font Asset | Builds the TextMeshPro font asset from Yuji Syuku |
| Compile Check | Compiles Runtime and Editor (used by the test script) |
| Ensure Render Pipeline | Makes sure the render pipeline assets exist |
| Build Windows Player | Builds Build/Windows/UnityRonin.exe |
| Build Android APK | Builds Build/Android/UnityRonin.apk |

Scripts in `tools/`:

| Script | What it does |
| --- | --- |
| tools/test.ps1 | Core NUnit tests; `-Compile` adds a Unity compile check |
| tools/autoplay.ps1 | Builds a Windows player and runs it headless with `-batchmode -nographics -demo` |
| tools/demo.ps1 | Same tour, windowed so you can watch; `-SkipBuild` reuses the last player |
| tools/unity.ps1 | Runs any editor method in batch mode (`-Method`, `-Log`); needs the Editor closed on this project |

## Self-play verification

```powershell
pwsh tools/autoplay.ps1   # headless
pwsh tools/demo.ps1       # windowed, watchable
```

With the `-demo` flag, `RoninAutopilot` drives the Samurai and the duel through the real `VirtualJoystick` and `SwipeInputReader` pointer-handler methods, the same ones a touchscreen calls. It first loses one duel on purpose to check the player is knocked back out of range, then defeats each enemy, and quits with PASS or FAIL. That proves the explore, engage, duel, win or retreat loop fires correctly. It cannot prove the gestures feel right.

## Playtest on a phone

1. Run **UnityRonin > Build Android APK**, or `pwsh tools/unity.ps1 -Method UnityRonin.Editor.RoninBuild.BuildAndroid -Log build-android`. It writes `Build/Android/UnityRonin.apk`, signed with Unity's auto-generated debug keystore: fine for sideloading, not for a store.
2. Install it: `adb install Build/Android/UnityRonin.apk`, or copy it to the phone and open it with install from unknown sources enabled.
3. Drag in the bottom half of the screen to move, walk up to a swordsman to draw the katana, then swipe to parry an attack or strike an opening.

## Project layout

```text
Assets/UnityRonin/Core       Unity-free rules: gestures, duel director, stick, engage gate, Rng
Assets/UnityRonin/Runtime    MonoBehaviours: input, camera, player, duel presenter, game controller
Assets/UnityRonin/Editor     Scene, font and build menu items
Assets/UnityRonin/Fonts      Yuji Syuku brush typeface and its SDF asset
Tests/Core.Tests             NUnit tests for Core
tools                        test, autoplay, demo and batch-mode scripts
artifacts/android-playtest   Early on-device captures
```

## Testing

`pwsh tools/test.ps1` runs the NUnit tests in `Tests/Core.Tests`. Each rule in [docs/USER_STORIES.md](docs/USER_STORIES.md) names its test. Anything visible is covered by an autoplay step or waits on an owner playtest.

## Capture

Packages/manifest.json references the local MindAttic.Capture package (`com.mindattic.capture`) at `../../MindAttic.Capture`, so a sibling checkout of that repo is needed to open the project. No capture configuration file is committed.

## Limitations

- Gesture feel under a real thumb is unconfirmed; several stories are partial until an owner playtest.
- Primitives only: no real models, rigs, animation or sound yet.
- Android only on mobile. There is no iOS build; it needs signing and an Apple developer account this repo cannot supply.
- The Android APK uses a debug keystore.
- The screenshots in `artifacts/android-playtest` are early on-device captures and do not show the current scene.

## Documentation

- [docs/BIBLE.md](docs/BIBLE.md): what this is, what it is not, and its laws
- [docs/USER_STORIES.md](docs/USER_STORIES.md): verified status of every story
- [AGENTS.md](AGENTS.md): agent instructions
- [CREDITS.md](CREDITS.md): asset sources and licenses

## License

No LICENSE file: all rights reserved. The Yuji Syuku font is under the SIL Open Font License 1.1; see [CREDITS.md](CREDITS.md).

Part of [MindAttic](https://mindattic.com) — see more projects at [github.com/mindattic](https://github.com/mindattic).
