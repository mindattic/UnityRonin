---
codex: 1
project: UnityRonin
code: UR
layer: bible
status: living
updated: 2026-10-11
---

# Unity Ronin (UR): Bible

## 1. The one sentence {#UR-§1}
Unity Ronin is a Unity 6 portrait mobile game where a Samurai is explored one-thumb, dragging a
transparent analog stick that covers the bottom half of the screen, and fought one-thumb, cardinal
swipes parrying or striking through a telegraphed katana duel the instant an enemy swordsman comes
into range.

## 2. The product promise {#UR-§2}
- **One hand, no buttons.** Every input is either a drag on the bottom-half stick or a swipe anywhere
  on screen. There is no on-screen button, no virtual D-pad overlay, no tap-to-target.
- **Explore freely.** The stick drives camera-relative third-person movement; the Samurai runs where
  the thumb points regardless of which way the camera happens to be facing. The camera sits slightly
  above and behind, smart-following so the player never has to drag a second finger to look around.
  Dragon Hunter: Hero's Legends' bottom-half virtual-stick exploration is the closest reference for
  the feel (owner reference, 2026-10-11).
- **Combat takes the thumb off the stick.** The instant the Samurai comes within range of an enemy
  swordsman, movement goes automatic (the game closes the distance and squares the two combatants up)
  and the katana comes out. The same thumb that was dragging the stick now swipes: a direction
  cardinal to the telegraph. If the enemy is attacking from a side, swiping that side parries it; if
  the enemy is guarding and has left a side open, swiping that side strikes it. Guessing wrong, or not
  swiping in time, costs a hit.
- **The duel is a rhythm of beats**, not a combo string: telegraph, respond, resolve, repeat, until one
  side's health runs out.
- It is a touch-only mobile game, portrait orientation, locked (no landscape, no autorotate).

## 3. What it is NOT {#UR-§3}
- **Not a twin-stick or button-combo brawler.** There is exactly one drag surface and one swipe
  surface; never both live at once, and neither is a tap-target or an icon to press.
- **Not free camera.** The player never drags to look around; `SmartFollowCamera` owns every camera
  move, including framing the duel.
- **Not a full game yet.** One demo scene, two enemies, primitives only (no art, no animation rig, no
  sound) — a vertical slice proving the movement and the duel feel are right before investing in art.
- **Not built on paid or non-redistributable assets.** No Mixamo, no Asset Store packs, no ripped
  game files, per [HOUSE-LAW-3](../../MindAttic.HouseRules.md#HOUSE-LAW-3) in spirit and
  [[project_unityeve]]'s own convention.

## 4. Architecture canon {#UR-§4}

```text
 ┌─────────────────────────────────┐   ┌───────────────────────────────────────┐
 │ UnityRonin.Core (no UnityEngine) │◄──│ UnityRonin.Runtime (MonoBehaviours)    │
 │ VirtualStick  CameraRelativeMotion│   │ VirtualJoystick  SwipeInputReader      │
 │ EngageGate  SwipeClassifier       │   │ SamuraiLocomotion  SamuraiWeapon       │
 │ DuelDirector  DuelBeat/Outcome    │   │ SmartFollowCamera                      │
 │ Rng (seeded)                      │   │ EnemyEngageZone DuelTelegraph           │
 └────────────────▲──────────────────┘   │ DuelPresenter  RoninGameController     │
                  │ dotnet test            └──────────────────▲───────────────────┘
                  │ (Tests/Core.Tests)                         │
                  └─────────────────────────┌───────────────────┘
                                            │ UnityRonin.Editor   │
                                            │ DemoContentBuilder  │
                                            │ RoninBuild          │
                                            └─────────────────────┘
```

- **Nouns:** `StickVector`, `DuelBeat`, `DuelOutcome`, `EngageState` (Core data/enums).
- **Verbs:**
  - `VirtualStick.Resolve/FromDrag` — dead zone, clamp, rescale.
  - `CameraRelativeMotion.ToWorldDirection` — stick → world heading by camera yaw.
  - `EngageGate.Update` — Explore ⇄ Engaged with hysteresis (enter radius < exit radius).
  - `SwipeClassifier.Classify` — raw drag → cardinal `SwipeDirection`.
  - `DuelDirector.Resolve` — scores one beat, mutates health, deals the next beat.
  - `RoninGameController` — the only thing that decides whether the joystick zone or the swipe zone is
    live, and which enemy (if any) is currently being fought.
- **Content:** `DemoContentBuilder` generates every GameObject, wiring, and the scene file,
  deterministically, from primitives.

## 5. The Laws {#UR-§5}
This project inherits the org-wide laws in [MindAttic.HouseRules.md](../../MindAttic.HouseRules.md),
including HOUSE-LAW-1 (whole-number versions, `version.txt`), HOUSE-LAW-8 (done is verified) and
HOUSE-LAW-10 (commit to main).

### {#UR-LAW-1} Core has no UnityEngine
`UnityRonin.Core` is `noEngineReferences: true` and compiles as netstandard2.1 under `dotnet test`.
All randomness goes through the seeded `Rng`. *(Source: asmdef; test `Same_seed_replays_the_identical_sequence`.)*

### {#UR-LAW-2} The hysteresis band cannot flicker
`EngageGate` requires `exitRadius > enterRadius`, and only releases past the exit radius once engaged,
so standing at the boundary never toggles stance on and off every frame. *(Test
`Stays_engaged_inside_the_hysteresis_band_so_it_cannot_flicker_at_the_boundary`, `Rejects_an_exit_radius_that_would_allow_flicker`.)*

### {#UR-LAW-3} Exactly one input surface is live at a time
`RoninGameController` disables the joystick zone and enables the swipe zone the instant a duel starts,
and reverses it the instant the duel ends. Neither `VirtualJoystick` nor `SwipeInputReader` decides
this for itself. *(Source: `RoninGameController.StartDuel/OnDuelEnded`.)*

### {#UR-LAW-4} A duel beat is won or lost by the swipe alone
`DuelDirector.Resolve` takes only a `SwipeDirection` (or `None` on timeout) and returns a pure outcome;
it never reads a transform, a collider, or wall-clock time itself. The presentation layer owns the
clock. *(Tests in `DuelDirectorTests`.)*

### {#UR-LAW-5} Generated content is regenerated, not hand-edited
Edit `DemoContentBuilder`, then re-run `UnityRonin/Build Demo Scene`. A hand edit to `Demo.unity` is
lost on the next build.

### {#UR-LAW-6} Free assets only
Every third-party file is CC0 or OFL and listed in `CREDITS.md`. The current build has none — primitives
only — on purpose, so the movement and duel feel are proven before any art is committed.

## 6. Verified state {#UR-§6}
- `dotnet test Tests/Core.Tests`: 30 passing — stick dead zone/clamp, camera-relative heading, engage
  hysteresis, swipe classification, seeded RNG, full duel scoring matrix and seed replay.
- `tools/test.ps1 -Compile`: Unity 6000.4.3f1 batch-mode compile of `UnityRonin.Core/Runtime/Editor`
  is clean (the only errors in the log are inside Unity's own bundled `com.unity.shadergraph` package,
  unrelated to this project).
- `UnityRonin/Build Demo Scene` (`DemoContentBuilder.BuildAll`) runs batch-mode end to end and writes
  `Assets/UnityRonin/Scenes/Demo.unity` with no errors: ground, Samurai (capsule + CharacterController +
  `SamuraiLocomotion`/`SamuraiWeapon`), camera, HUD canvas (joystick zone, swipe zone, duel telegraph),
  `GameController`, `DuelPresenter`, and two enemy swordsmen, all wired by `SerializedObject`.
- No autoplay/self-play pass yet (nothing presses the stick or swipes for itself) — see Active frontier.
- No owner playtest on a touch device yet.

## 7. Active frontier {#UR-§7}
- An `Autopilot`-style self-play verification (`-batchmode -autoplay`), on UnityEve's pattern, so the
  explore→engage→duel→resolve loop can be proven without a human holding a phone.
- Real free-asset Samurai and enemy models/animations (CC0, credited in `CREDITS.md`) replacing the
  capsule/cube primitives.
- Sound: a footstep, the blade draw, a clash, a hit, a kill — none exist yet.
- A player-defeat flow: losing a duel currently just restarts it at full health immediately (the
  enemy is still in engage range), so there is no retreat, no game over, and no way to walk away from
  a lost fight yet.
- Difficulty scaling across repeated duels (telegraph speed, response window) as the player progresses.
- Portrait UI polish: a health readout for both combatants during a duel.

## 8. Quality bar {#UR-§8}
A change is done when all of these hold:
1. `tools/test.ps1` is green.
2. `tools/test.ps1 -Compile` is green.
3. The demo content builder has been re-run if any wiring changed.
4. `docs/USER_STORIES.md` is updated.
5. It is committed once, directly to `main`.

## 9. Glossary {#UR-§9}
- **Stick zone:** the invisible RectTransform covering the bottom half of the screen that owns the
  transparent analog stick while exploring.
- **Swipe zone:** the full-screen RectTransform that reads one drag-and-release as a cardinal swipe
  while a duel is active.
- **Engage / Explore:** the two states of `EngageGate` — free movement vs. a live duel.
- **Beat:** one telegraph → response → resolution cycle of a duel.
- **Telegraph:** the HUD arrow showing the enemy's next direction and whether it is an attack (parry)
  or a guard opening (strike).
- **Parry / Strike / Hit / Whiff:** the four live `DuelOutcome` results of a beat (see `DuelOutcome.cs`
  for the two timeout variants as well).
