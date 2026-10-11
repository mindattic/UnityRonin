# MindAttic project agent entrypoint

Read the shared protocol at ..\mindattic-agent-standard\AGENTS.md, then `docs/BIBLE.md`. Common prompt
commands are implemented by the shared runner; do not add a second copy under a provider-specific
command folder.

## What this is

Unity Ronin is a Unity 6 portrait mobile game: a Samurai explored one-thumb, via a transparent analog
stick covering the bottom half of the screen, and fought one-thumb, drawing calligraphy-stroke gestures
(four cuts and a thrust) that parry or strike a telegraphed katana duel when an enemy comes into range.
A successful parry opens a slowed-time riposte window to counterattack. Read `docs/BIBLE.md` for what
it is, what it is not, and its laws.

## Laws (do not break)

1. **Core is pure C#.** `Assets/UnityRonin/Core` is `noEngineReferences: true` and must compile under
   plain `dotnet test` (`Tests/Core.Tests`). Randomness goes only through the seeded `Core/Util/Rng`.
2. **Presentation never decides a rule.** MonoBehaviours call Core (`DuelDirector`, `EngageGate`,
   `VirtualStick`, `CameraRelativeMotion`, `GestureClassifier`) and present the result; they never
   re-implement the dead zone, the hysteresis band, or the parry/strike/riposte scoring themselves —
   including which side's turn it is (`DuelDirector.Phase`) and whether the enemy retreats or counters.
3. **One input owner at a time.** The joystick zone drives movement while exploring; the swipe zone
   drives the duel. `RoninGameController` is the only thing that toggles which is active — never both.
4. **Content is generated.** `Demo.unity` and everything in it come from `DemoContentBuilder`. Change
   the builder, not the generated scene, then re-run it.
5. **Done is verified, not asserted.** `./tools/test.ps1` green, then `./tools/test.ps1 -Compile` green.
   Anything visual needs an owner playtest before it is marked done in `docs/USER_STORIES.md`.
6. **Free assets only,** recorded in `CREDITS.md`. Never Mixamo or store assets.

## Map

| File | Role |
| --- | --- |
| `Core/Movement/VirtualStick.cs` | Dead-zone + clamp + rescale math for the on-screen stick |
| `Core/Movement/CameraRelativeMotion.cs` | Rotates stick input into a world heading by camera yaw |
| `Core/Movement/EngageGate.cs` | Hysteresis gate between Explore and Duel |
| `Core/Combat/GestureClassifier.cs` | Splits a drawn path into strokes, classifies each into a `GestureSymbol` |
| `Core/Combat/DuelDirector.cs` | The duel's rules: telegraph a beat, score the gesture, track health, run the parry→riposte→combo state machine (`DuelPhase`) |
| `Runtime/Input/VirtualJoystick.cs`, `SwipeInputReader.cs` | The two touch surfaces |
| `Runtime/Player/SamuraiLocomotion.cs`, `SamuraiWeapon.cs` | Movement and the drawn/sheathed katana, swung through whatever gesture was just drawn |
| `Runtime/Camera/SmartFollowCamera.cs` | Slightly-above third-person follow; frames the duel when one starts |
| `Runtime/Combat/EnemyEngageZone.cs`, `EnemyCombatFeedback.cs`, `DuelTelegraph.cs`, `DuelPresenter.cs` | Range detection, the enemy's retreat/counter feedback, the calligraphy-glyph HUD, the beat/riposte loop |
| `Runtime/Game/RoninGameController.cs` | The Explore ⇄ Duel switch |
| `Runtime/Game/SelfPlayInput.cs` | Shared pointer-handler self-play driver (walk/swipe) used by autopilot and demo mode |
| `Runtime/Game/RoninAutopilot.cs` | `-autoplay`: verifies the loop, quits 0/1 |
| `Runtime/Game/RoninDemoMode.cs` | `-demo`: self-plays on loop for watching/recording, never quits |
| `Editor/DemoContentBuilder.cs` | Generates `Demo.unity` |
