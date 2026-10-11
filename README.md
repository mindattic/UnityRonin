# Unity Ronin

A Unity 6 portrait mobile game: explore one-thumb with a transparent analog stick covering the bottom
half of the screen, then duel one-thumb when a Samurai meets a sword — cardinal swipes parry or strike
a telegraphed katana duel.

See [docs/BIBLE.md](docs/BIBLE.md) for what this is, what it isn't, and its laws, and
[docs/USER_STORIES.md](docs/USER_STORIES.md) for verified status.

## Quick start

```powershell
pwsh tools/test.ps1            # Core movement/combat rules, seconds, no Unity needed
pwsh tools/test.ps1 -Compile   # + a Unity 6000.4.3f1 batch-mode compile check
```

Open the project in Unity 6000.4.3f1, then **UnityRonin > Build Demo Scene** to (re)generate
`Assets/UnityRonin/Scenes/Demo.unity` from primitives.

## Self-play verification (no touch hardware needed)

```powershell
pwsh tools/autoplay.ps1
```

Builds a Windows player and runs it `-batchmode -nographics -autoplay`: `RoninAutopilot` drives the
Samurai and the duel through the real `VirtualJoystick`/`SwipeInputReader` pointer-handler methods (the
same ones a touchscreen calls), proving the explore → engage → duel → win/retreat loop fires correctly.
This proves the code paths work; it cannot prove the gestures feel right — see below.

## Demo Mode (watch it play itself)

```powershell
pwsh tools/demo.ps1
```

Builds a Windows player and launches it with `-autoplay`'s sibling flag, `-demo`: `RoninDemoMode` drives
the Samurai through the same real pointer-handler methods as autoplay, but for watching or recording
rather than verifying — it never quits, always plays to win, and once both enemies are defeated it
reactivates them and loops forever. Close the window when you're done watching.

## Playtest on a phone

Autoplay simulates pointer events; it is not a substitute for a human thumb. To actually feel the stick
and the swipes:

1. `UnityRonin > Build Android APK` in the Editor (or `pwsh tools/unity.ps1 -Method UnityRonin.Editor.RoninBuild.BuildAndroid -Log build-android`).
   This writes `Build/Android/UnityRonin.apk` (~30 MB), signed with Unity's auto-generated debug
   keystore — fine for sideloading onto your own device, not for a store listing.
2. Get the APK onto an Android phone (USB + `adb install Build/Android/UnityRonin.apk`, or copy it over
   and open it on-device with "install from unknown sources" enabled).
3. Play: drag anywhere in the bottom half of the screen to move, walk up to a swordsman to draw the
   katana, and swipe to parry an attack or strike an opening once the duel starts.

There is no iOS build yet (it needs a provisioning profile/signing you'd supply; the Unity iOS player
module is installed locally but Xcode + an Apple developer account are not something this repo can
provide).
