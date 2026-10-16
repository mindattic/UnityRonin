---
codex: 1
project: UnityRonin
code: U.RON
layer: bible
status: living
updated: 2026-10-11
---

# Unity Ronin (UR): Bible

## 1. The one sentence {#U.RON-§1}
Unity Ronin is a Unity 6 portrait mobile game where a Samurai is explored one-thumb, dragging a
transparent analog stick that covers the bottom half of the screen, and fought one-thumb, drawing
calligraphy-stroke gestures — four cuts and a thrust, the same five techniques both sides use — to
parry or strike through a telegraphed katana duel, where a successful parry opens a slowed-time
riposte window to press the attack.

## 2. The product promise {#U.RON-§2}
- **One hand, no buttons.** Every input is either a drag on the bottom-half stick or a swipe anywhere
  on screen. There is no on-screen button, no virtual D-pad overlay, no tap-to-target.
- **Explore freely.** The stick drives camera-relative third-person movement; the Samurai runs where
  the thumb points regardless of which way the camera happens to be facing. The camera sits slightly
  above and behind, smart-following so the player never has to drag a second finger to look around.
  Dragon Hunter: Hero's Legends' bottom-half virtual-stick exploration is the closest reference for
  the feel (owner reference, 2026-10-11).
- **Combat takes the thumb off the stick.** The instant the Samurai comes within range of an enemy
  swordsman, movement goes automatic (the game closes the distance and squares the two combatants up)
  and the katana comes out. The same thumb that was dragging the stick now draws: a calligraphy stroke
  shaped like the telegraphed technique. A cutting attack is stopped by the perpendicular cross-block;
  a thrust only by another thrust; an open guard is struck by matching its shape. Guessing wrong, or
  not drawing in time, costs a hit.
- **A parry earns a riposte, not a point.** Countering a live attack doesn't score damage on the
  spot — it opens a real, risk/reward combo: time slows, and the player draws a free attack of their
  own choosing (no telegraph this time). The enemy retreats (safe) or counters with a technique of its
  own, using the same cross-block/thrust/match rule mirrored. Land it and, if the enemy survives, the
  window reopens for another; get countered and the "flash slash" costs a hit instead. This is the
  duel's real combo system: chained exchanges, not a bonus tacked onto one swipe.
- **The duel is a rhythm of beats**, not a fixed combo string: telegraph, respond, resolve, and — on a
  parry — riposte, repeat, until one side's health runs out.
- It is a touch-only mobile game, portrait orientation, locked (no landscape, no autorotate).

## 3. What it is NOT {#U.RON-§3}
- **Not a twin-stick or button-combo brawler.** There is exactly one drag surface and one swipe
  surface; never both live at once, and neither is a tap-target or an icon to press.
- **Not free camera.** The player never drags to look around; `SmartFollowCamera` owns every camera
  move, including framing the duel.
- **Not a full game yet.** One demo scene, two enemies, primitives only (no art, no animation rig, no
  sound) — a vertical slice proving the movement and the duel feel are right before investing in art.
- **Not built on paid or non-redistributable assets.** No Mixamo, no Asset Store packs, no ripped
  game files, per [HOUSE-LAW-3](../../MindAttic.HouseRules.md#HOUSE-LAW-3) in spirit and
  [[project_unityeve]]'s own convention.

## 4. Architecture canon {#U.RON-§4}

```text
 ┌─────────────────────────────────┐   ┌───────────────────────────────────────┐
 │ UnityRonin.Core (no UnityEngine) │◄──│ UnityRonin.Runtime (MonoBehaviours)    │
 │ VirtualStick  CameraRelativeMotion│   │ VirtualJoystick  SwipeInputReader      │
 │ EngageGate  GestureClassifier     │   │ SamuraiLocomotion  SamuraiWeapon       │
 │ DuelDirector  DuelPhase            │   │ SmartFollowCamera                      │
 │ DuelBeat/Outcome  Rng (seeded)     │   │ EnemyEngageZone  EnemyCombatFeedback   │
 └────────────────▲──────────────────┘   │ DuelTelegraph  DuelPresenter           │
                  │ dotnet test            │ RoninGameController                   │
                  │ (Tests/Core.Tests)      └──────────────────▲───────────────────┘
                  └─────────────────────────┌───────────────────┘
                                            │ UnityRonin.Editor   │
                                            │ DemoContentBuilder  │
                                            │ RoninBuild          │
                                            └─────────────────────┘
```

- **Nouns:** `GestureSymbol` (the five techniques: Horizontal/Vertical/Rising/Falling/Thrust),
  `GestureStroke`, `DuelBeat`, `DuelOutcome`, `DuelPhase` (`EnemyBeat` vs `PlayerRiposte`),
  `EngageState` (Core data/enums).
- **Verbs:**
  - `VirtualStick.Resolve/FromDrag` — dead zone, clamp, rescale.
  - `CameraRelativeMotion.ToWorldDirection` — stick → world heading by camera yaw.
  - `EngageGate.Update` — Explore ⇄ Engaged with hysteresis (enter radius < exit radius).
  - `GestureClassifier.Classify` — a drawn path → one or more `GestureStroke`s, each a `GestureSymbol`.
  - `DuelDirector.Resolve` — scores one enemy beat; a parry transitions `Phase` to `PlayerRiposte`
    instead of dealing the next beat.
  - `DuelDirector.ResolveRiposte` — scores the player's freely-chosen riposte attack; the enemy
    retreats, counters (mirroring the same parry rule), or takes the hit and reopens the window.
  - `RoninGameController` — the only thing that decides whether the joystick zone or the swipe zone is
    live, and which enemy (if any) is currently being fought.
- **Content:** `DemoContentBuilder` generates every GameObject, wiring, and the scene file,
  deterministically, from primitives.

## 5. The Laws {#U.RON-§5}
This project inherits the org-wide laws in [MindAttic.HouseRules.md](../../MindAttic.HouseRules.md),
including HOUSE-LAW-1 (whole-number versions, `version.txt`), HOUSE-LAW-8 (done is verified) and
HOUSE-LAW-10 (commit to main).

### {#U.RON-LAW-1} Core has no UnityEngine
`UnityRonin.Core` is `noEngineReferences: true` and compiles as netstandard2.1 under `dotnet test`.
All randomness goes through the seeded `Rng`. *(Source: asmdef; test `Same_seed_replays_the_identical_sequence`.)*

### {#U.RON-LAW-2} The hysteresis band cannot flicker
`EngageGate` requires `exitRadius > enterRadius`, and only releases past the exit radius once engaged,
so standing at the boundary never toggles stance on and off every frame. *(Test
`Stays_engaged_inside_the_hysteresis_band_so_it_cannot_flicker_at_the_boundary`, `Rejects_an_exit_radius_that_would_allow_flicker`.)*

### {#U.RON-LAW-3} Exactly one input surface is live at a time
`RoninGameController` disables the joystick zone and enables the swipe zone the instant a duel starts,
and reverses it the instant the duel ends. Neither `VirtualJoystick` nor `SwipeInputReader` decides
this for itself. *(Source: `RoninGameController.StartDuel/OnDuelEnded`.)*

### {#U.RON-LAW-4} A duel beat is won or lost by the gesture alone
`DuelDirector.Resolve`/`ResolveRiposte` take only the classified `GestureStroke`s (or an empty list on
timeout) and return a pure outcome; neither reads a transform, a collider, or wall-clock time itself.
The presentation layer owns the clock, including the riposte window's `Time.timeScale` slow-motion —
Core only knows `Phase` and `RiposteWindowSeconds`, never how "slow" feels. *(Tests in
`DuelDirectorTests`.)*

### {#U.RON-LAW-5} Generated content is regenerated, not hand-edited
Edit `DemoContentBuilder`, then re-run `UnityRonin/Build Demo Scene`. A hand edit to `Demo.unity` is
lost on the next build.

### {#U.RON-LAW-6} Free assets only
Every third-party file is CC0 or OFL and listed in `CREDITS.md`. The one exception to primitives-only
is the duel telegraph's font (Yuji Syuku, OFL — `CREDITS.md`), needed for the gesture glyphs to read
as real calligraphy; everything else stays primitives on purpose, so the movement and duel feel are
proven before any more art is committed.

### {#U.RON-LAW-7} Verification means an actual device, not just batch mode
`-nographics` never renders a single pixel and runs uncapped (observed ~10,000 FPS with nothing to draw)
— it cannot catch a rendering bug or a physics bug that only appears at a real frame rate. Treat a batch
compile and a headless autoplay pass as necessary, not sufficient: install the real build
(`UnityRonin/Build Android APK`) on an actual device or emulator before calling anything visual done.
*(This is how §6's bugs 6–8 were found — a dangling render pipeline reference, a blown-out ground
material, and a frame-rate-dependent movement stall — none visible to `-nographics`.)*

## 6. Verified state {#U.RON-§6}
- `dotnet test Tests/Core.Tests`: 45 passing — stick dead zone/clamp, camera-relative heading, engage
  hysteresis, gesture classification (including the Thrust tap), seeded RNG, the full duel scoring
  matrix (cut cross-blocks, thrust-vs-thrust, guard matches), every riposte branch (landed, countered,
  retreated, timed out, a chained combo defeating an enemy without leaving `PlayerRiposte`), and seed
  replay through a riposte exchange.
- Both runs of `-autoplay` on a Windows player with the riposte system built: exit 0. Autoplay
  deliberately declines every riposte window it's offered (an empty gesture, which always times out
  safely) rather than attempting a bonus attack — it only needs to prove the slow-time wiring fires
  for real through actual pointer-handler calls, not gamble this pass's deterministic win on the
  riposte's genuine risk; the Core tests already cover every branch of that risk directly. `-demo`
  confirmed visually: the gold "刀" riposte prompt appears after a real parry and persists well past
  its nominal 0.6s window (≈3× longer across screenshots 300ms apart), confirming the slow-motion is
  actually in effect, not just a longer timer.
- `tools/test.ps1 -Compile`: Unity 6000.4.3f1 batch-mode compile of `UnityRonin.Core/Runtime/Editor`
  is clean (the only errors in the log are inside Unity's own bundled `com.unity.shadergraph` package,
  unrelated to this project).
- `UnityRonin/Build Demo Scene` (`DemoContentBuilder.BuildAll`) runs batch-mode end to end and writes
  `Assets/UnityRonin/Scenes/Demo.unity` with no errors: ground, Samurai (capsule + CharacterController +
  `SamuraiLocomotion`/`SamuraiWeapon`), camera, HUD canvas (joystick zone, swipe zone, duel telegraph),
  `GameController`, `DuelPresenter`, and two enemy swordsmen, all wired by `SerializedObject`.
- `tools/autoplay.ps1`: builds a Windows player of Demo.unity and runs it `-batchmode -nographics
  -autoplay`. `RoninAutopilot` calls `VirtualJoystick`'s and `SwipeInputReader`'s real
  `IPointerDownHandler`/`IDragHandler`/`IPointerUpHandler` methods — the same ones a touchscreen would
  invoke — to (1) deliberately lose one duel and confirm the loss knocks the Samurai back out of the
  engage radius rather than the duel re-triggering instantly, then (2) walk to and defeat both enemies
  for real. **PASS, run twice in a row**: exit 0 both times (`Logs/autoplay.log`). This is the loop end
  to end: stick input → camera-relative movement → engage-radius trigger → katana draw → duel beats →
  swipe scoring → win (enemy removed) or loss (Samurai knocked back), with no human and no touch
  hardware.
  - This pass caught five real bugs before any human touched a device:
    1. A `CanvasGroup` with `blocksRaycasts=false` sat on the joystick's own raycast-receiving
       GameObject — a `CanvasGroup` disables raycasting for its whole subtree, including itself, so
       the stick could never have received a touch.
    2. `RoninGameController` measured engage distance from its own logic-only GameObject instead of
       the Samurai's transform — the duel could never have triggered regardless of where the player
       walked.
    3. `SamuraiLocomotion`'s camera-yaw reference was never actually bound by the content builder.
    4. The content builder passed the HUD canvas itself, not the joystick zone, as the GameObject
       `RoninGameController` toggles off when a duel starts (a `.transform.parent` mistake).
    5. The katana prop cubes (cosmetic only) each got a default `BoxCollider` from
       `CreatePrimitive`, parented inside the Samurai's own `CharacterController` radius — a classic
       Unity self-collision gotcha that nearly froze all movement.
  - All five are fixed in the current build.
- **An actual Android install, not just a build.** `UnityRonin/Build Android APK` was installed on a
  real booted emulator (Pixel 7, API 36) via `adb install` and driven with genuine `adb shell input`
  touch injection against the real Android touch stack — categorically more real than calling a C#
  pointer-handler method. Screenshots in `artifacts/android-playtest/` confirm it boots, renders, and
  responds on device. This on-device test caught three more real bugs no batch-mode run ever could,
  because `-nographics` never renders a single pixel:
  6. **ProjectSettings/GraphicsSettings.asset and QualitySettings.asset still pointed at UnityEve's own
     URP pipeline asset GUID** (copied wholesale at scaffold time; that asset doesn't exist in this
     project). With no render pipeline actually resolvable, URP's shader stripper kept ~1.7M
     "ForwardLit" variants instead of the handful this scene uses — a ~36 GB, 15+ minute player build
     that also pressured system memory badly enough that the harness killed a background shell twice.
     Fixed by `RoninBuild.EnsureRenderPipeline`, which creates and assigns a real, minimal URP asset;
     variant counts dropped to 1–2 per pass and player builds now take under a minute.
  7. **The ground rendered as a blown-out white specular highlight** on real GPU hardware — invisible
     to `-nographics`, which never renders a pixel. `Default-Material`'s smoothness produces a huge
     specular highlight across a plane this large under a directional light. Fixed with explicit matte
     (near-zero smoothness, non-metallic), saved-as-asset materials for ground/Samurai/enemies.
  8. **Movement crawled to a near-standstill** when the headless Windows autoplay build ran completely
     uncapped (~10,000 FPS with nothing to render): each frame's displacement became so small that
     `CharacterController.Move` couldn't reliably apply it while touching the ground. A real phone never
     runs anywhere near that fast, so this specific failure mode could never happen on-device — but
     capping `Application.targetFrameRate = 60` in `RoninGameController.Awake` is correct regardless,
     for battery and thermal reasons, and it also made the Windows autoplay harness itself reliable.
- Still outstanding: an owner playtest for *feel*. Autoplay and the on-device install both prove the
  code paths fire correctly; neither can judge whether the stick drag and the swipe timing feel right
  under a real thumb — that is not something any amount of automated testing or agent work can
  substitute for. See README "Playtest on a phone" for how to get the build onto one.

## 7. Active frontier {#U.RON-§7}
- An owner playtest for feel on a touch device — automated testing (autoplay, on-device ADB injection)
  only proves the wiring works, never the feel. This is a hard limit: it genuinely requires a human.
  The riposte combo especially needs a real thumb's judgment: is the slow-motion window long enough,
  does `retreatChance` (0.3) feel fair, is drawing a second gesture mid-combo actually comfortable.
- Real free-asset Samurai and enemy models/animations (CC0, credited in `CREDITS.md`) replacing the
  capsule/cube primitives — this also covers `EnemyCombatFeedback`'s hop/flash, which stands in for a
  real retreat/counter animation.
- Sound: a footstep, the blade draw, a clash, a hit, a kill, and a distinct cue for the riposte window
  opening — none exist yet.
- A true game-over/retry screen after a loss, beyond the current knockback-and-retreat.
- Difficulty/balance scaling: `retreatChance`, `riposteWindowSeconds`, and `riposteTimeScale` are fixed
  constants right now, not tuned or scaled across repeated duels as the player progresses.
- Portrait UI polish: a health readout for both combatants during a duel, and some sense of combo
  count/length during a riposte chain.

## 8. Quality bar {#U.RON-§8}
A change is done when all of these hold:
1. `tools/test.ps1` is green.
2. `tools/test.ps1 -Compile` is green.
3. The demo content builder has been re-run if any wiring changed.
4. `docs/USER_STORIES.md` is updated.
5. It is committed once, directly to `main`.

## 9. Glossary {#U.RON-§9}
- **Stick zone:** the invisible RectTransform covering the bottom half of the screen that owns the
  transparent analog stick while exploring.
- **Swipe zone:** the full-screen RectTransform that reads one drawn gesture (a drag-and-release,
  possibly several strokes chained in one continuous motion) while a duel is active.
- **Engage / Explore:** the two states of `EngageGate` — free movement vs. a live duel.
- **Beat:** one telegraph → response → resolution cycle of an enemy-initiated exchange
  (`DuelPhase.EnemyBeat`).
- **Riposte:** the free, player-initiated exchange a successful parry earns (`DuelPhase.PlayerRiposte`):
  no telegraph — the player draws whichever of the five techniques they choose — answered by the
  enemy's own retreat-or-counter reaction. Chains as long as the player keeps landing it.
- **Technique / gesture symbol:** one of the five drawable shapes both sides attack and defend with —
  Horizontal (横斬り yoko-giri), Vertical (斬り下ろし kiri-oroshi), Rising (斬り上げ kiriage), Falling
  (袈裟斬り kesa-giri), Thrust (突き tsuki, drawn as a tap).
- **Telegraph:** the HUD glyph (a real brush-calligraphy character, see `CREDITS.md`) showing the
  enemy's next technique and whether it is an attack (parry) or a guard opening (strike); a riposte
  window shows a neutral "刀" cue instead, since the player picks freely.
- **Parry / Strike / Hit / Whiff:** the four live enemy-beat `DuelOutcome` results (see `DuelOutcome.cs`
  for the two timeout variants and the four `PlayerRiposte*` riposte results as well).
