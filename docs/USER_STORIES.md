---
codex: 1
project: UnityRonin
code: U.RON
layer: stories
status: living
updated: 2026-10-11
---
<!-- GENERATED from MindAttic.Tickets/data/Tickets.db by `tickets db render`. Do not edit: this file is rewritten from the database; change stories and tickets with the tickets CLI. -->

# Unity Ronin: User stories

> **Status:** ✅ verified (a named test or batch-mode check proves it) · 🟡 partial · ⬜ planned.
> **Evidence:** `TestName` = NUnit in `Tests/Core.Tests`; `autoplay` = `tools/autoplay.ps1`'s
> `-batchmode -nographics -autoplay` run, which drives `VirtualJoystick`/`SwipeInputReader` through
> their real pointer-handler methods — proves the code path fires correctly, not that it feels right
> under a real thumb (that still needs an owner playtest, BIBLE §7).

**Definition of done (every story):**
1. `tools/test.ps1` is green.
2. Each rule has a named test.
3. Anything visible has an owner playtest or an autoplay step.
4. It is committed once.

## Movement

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-001 | The on-screen stick ignores small jitter near its center (dead zone) | ✅ | `Input_inside_dead_zone_resolves_to_zero` |
| US-002 | Dragging past the stick's travel radius clamps to full deflection, never overshoots | ✅ | `Input_beyond_full_deflection_clamps_to_unit_magnitude` |
| US-003 | The stick's output rescales continuously from the dead-zone edge to full deflection | ✅ | `Rescaling_is_continuous_from_the_dead_zone_edge_to_full_deflection` |
| US-004 | Stick input is interpreted relative to the camera's facing, not world axes | ✅ | `Pushing_up_with_zero_yaw_moves_along_world_forward`, `Pushing_up_with_camera_yawed_90_degrees_moves_along_world_right` |
| US-005 | The stick covers the bottom half of the screen and appears wherever the thumb lands | 🟡 | autoplay drives real `OnPointerDown`/`OnDrag` and the Samurai moves correctly; confirmed rendering on a real Android device (`artifacts/android-playtest/`); needs an owner playtest for how it feels under a real thumb |
| US-006 | The camera sits slightly above and behind and smart-follows without a second-finger drag | 🟡 | `SmartFollowCamera` runs every frame of the autoplay pass with no errors; confirmed rendering on a real Android device; needs an owner playtest |

## Engagement

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-010 | Coming within an enemy's enter radius starts a duel | ✅ | `Engages_once_the_enemy_is_within_enter_radius` |
| US-011 | The duel doesn't release until the player backs out past a larger exit radius (no flicker at the boundary) | ✅ | `Stays_engaged_inside_the_hysteresis_band_so_it_cannot_flicker_at_the_boundary`, `Releases_back_to_exploring_only_past_the_exit_radius` |
| US-012 | An exit radius that isn't strictly larger than the enter radius is rejected outright | ✅ | `Rejects_an_exit_radius_that_would_allow_flicker` |
| US-013 | Entering a duel swaps the live input surface from the stick to full-screen swipes, and movement goes automatic | ✅ | autoplay: joystick zone deactivates and swipe zone activates on engage, for both enemies, in `Logs/autoplay.log` |

## Duel (katana combat)

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-020 | A drawn path is split into straight strokes at sharp corners only, each classified into one of five shapes | ✅ | `GestureClassifierTests`: `A_long_straight_horizontal_drag_classifies_as_horizontal`, `A_sharp_corner_splits_into_two_chained_strokes`, `A_gentle_curve_with_no_sharp_corner_stays_one_stroke` |
| US-021 | A path that never clears the minimum stroke length is a Thrust (a tap), not an aborted swipe | ✅ | `A_pure_tap_with_no_movement_is_a_thrust`, `A_drag_shorter_than_the_minimum_distance_is_also_a_thrust_not_an_aborted_swipe` |
| US-022 | Drawing the perpendicular shape against a cutting enemy attack parries it: no damage, and it opens a riposte | ✅ | `Perpendicular_stroke_against_a_cutting_attack_parries`, `Successful_parry_opens_a_riposte_window_instead_of_dealing_immediate_damage` |
| US-023 | Drawing the wrong shape, or not in time, against an attack hits the player | ✅ | `Matching_stroke_against_a_cutting_attack_hits_the_player`, `Not_responding_to_an_attack_in_time_also_hits_the_player` |
| US-023a | A thrust attack is parried only by a thrust in return — a cross-block has no purchase on a straight point | ✅ | `Matching_thrust_against_a_thrust_attack_parries`, `Thrusting_against_a_cutting_attack_does_not_parry_it`, `A_cross_block_does_not_stop_a_thrust_attack` |
| US-024 | Matching the open shape against an enemy guard strikes it | ✅ | `Matching_the_open_shape_against_a_guard_strikes_the_enemy`, `Matching_a_thrust_guard_opening_strikes_the_enemy` |
| US-025 | Drawing the wrong shape, or not in time, against a guard just wastes the beat — no damage either way | ✅ | `Wrong_shape_against_a_guard_wastes_the_beat_with_no_damage`, `Not_responding_to_a_guard_wastes_the_beat_with_no_damage` |
| US-025a | Chaining strokes onto a successful guard strike lands a bonus hit each, as long as they keep alternating shape | ✅ | `Chained_strokes_alternating_shape_land_a_bonus_hit_each`, `Repeating_the_same_shape_breaks_the_combo_chain` |
| US-026 | The duel ends the instant either combatant's health reaches zero | ✅ | `The_duel_ends_as_soon_as_either_side_reaches_zero_health` |
| US-027 | The same seed replays the identical sequence of beats, including riposte exchanges | ✅ | `The_same_seed_replays_the_same_sequence_of_beats`, `Same_seed_replays_the_identical_sequence` (Rng) |
| US-028 | The katana is sheathed while exploring and drawn the instant a duel starts, and swings through whatever gesture was just drawn | 🟡 | `DuelPresenter.Begin/End` calls `SamuraiWeapon.Draw/Sheathe`, and every gesture calls `SamuraiWeapon.Swing`, without error every autoplay duel; needs an owner playtest to confirm it looks right |
| US-029 | The HUD shows the telegraphed technique as a real calligraphy-brush glyph, colored by attack vs. guard | 🟡 | `DuelTelegraph.Show/Hide` runs every autoplay beat without error; `-demo` screenshots confirm all five glyphs render correctly (see §Combo for the riposte prompt); needs an owner playtest to confirm it reads clearly |
| US-030 | Winning a duel removes that enemy | ✅ | autoplay: both enemies inactive after their duels, in `Logs/autoplay.log` |
| US-031 | Losing a duel knocks the Samurai back out of range instead of the duel immediately re-triggering | ✅ | autoplay: `RoninAutopilot` deliberately loses the first duel, then confirms the gate released and the enemy is still alive (`Logs/autoplay.log`: "confirmed a loss retreats the player") |

## Combo (parry → riposte)

A successful parry doesn't score damage on the spot — it earns a free, player-chosen attack
(`DuelPhase.PlayerRiposte`), answered by the enemy retreating or countering with the same five
techniques, mirrored. This is the duel's real combo system (see `docs/BIBLE.md` §2).

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-050 | A successful parry opens a riposte window instead of immediately dealing the next enemy beat | ✅ | `Successful_parry_opens_a_riposte_window_instead_of_dealing_immediate_damage` |
| US-051 | Not drawing a riposte attack in time wastes the window — no damage either way | ✅ | `Riposte_timeout_wastes_the_window_with_no_damage` |
| US-052 | The enemy may retreat from a riposte attack instead of risking a counter — no damage either way | ✅ | `Riposte_retreat_ends_the_window_safely` |
| US-053 | If the enemy's counter doesn't validly answer the riposte attack, it lands — enemy takes damage | ✅ | `Riposte_lands_when_the_enemys_counter_fails` |
| US-054 | If the enemy's counter does validly answer the riposte attack, the "flash slash" lands on the player instead | ✅ | `Riposte_countered_when_the_enemys_counter_matches` |
| US-055 | A landed riposte reopens the window if the enemy survives, chaining into a real combo with no artificial depth cap | ✅ | `A_chained_combo_of_riposte_lands_can_defeat_the_enemy_without_returning_to_enemy_beat` |
| US-056 | The riposte window visibly slows time and shows a neutral "your turn" cue, not a telegraphed technique | ✅ | `-demo` screenshots: the gold "刀" glyph appears after a real parry and persists ≈3× its nominal 0.6s window, confirming `Time.timeScale` slow-motion is genuinely active |
| US-057 | The enemy hops back on a retreat and flashes on a successful counter — primitive-only feedback, no rig | 🟡 | `EnemyCombatFeedback.HopBack/FlashCounter` wired from `RoninGameController.OnBeatResolved`, runs without error every autoplay duel; needs an owner playtest to confirm it reads clearly |
| US-058 | `-autoplay` proves the riposte wiring fires for real (prompt, slow-motion, timeout) without gambling the pass's deterministic win on the riposte's real risk | ✅ | `-autoplay` declines every riposte window (always a safe, scored timeout); two consecutive runs both exited 0 |

## Rendering

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-040 | The project has its own real URP pipeline asset, not a dangling reference to another project's | ✅ | `RoninBuild.EnsureRenderPipeline`; confirmed by shader variant counts dropping from ~1.7M to 1–2 per pass in `Logs/build-player.log` |
| US-041 | The ground renders as matte terrain, not a blown-out white specular highlight | ✅ | On-device screenshot `artifacts/android-playtest/03-fixed-ground.png` on a real Pixel 7 emulator, Android 16 |
| US-042 | The game boots and renders correctly on real Android hardware (not just Unity's -nographics batch mode) | ✅ | Installed via `adb install` and launched via `adb shell monkey` on a booted Pixel 7 (API 36) AVD; screenshots in `artifacts/android-playtest/` |

## Tickets
> Permanent log of every change and fix — this repo's ticket system. Rows are append-only: never
> delete or reword one; only move its Status (open → closed / wont-do) and fill in Ref. Status is
> a word, not a glyph, so tickets don't count toward the story index. Keep this section last.

| ID | Date | Type | Status | Summary | Ref |
|---|---|---|---|---|---|
| U.RON-T1 | 2026-10-12 | docs | closed | Start the permanent ticket log in USER_STORIES.md | — |
| U.RON-T2 | 2026-10-14 | chore | closed | NUnit tests for DuelDirector guard rails and riposte edge cases (add to the existing xUnit Tests/Core.Tests/DuelDirectorTests.cs, Assets/UnityRonin/Core/Combat/DuelDirector.cs): Resolve throws InvalidOperationException while Phase is PlayerRiposte, ResolveRiposte throws while Phase is EnemyBeat, both throw once IsOver, constructor throws ArgumentNullException for a null Rng, PlayerWon is false when the player reaches zero health, a landed riposte with chained alternating strokes deals 1 plus bonus hits and a repeated shape breaks it, a countered riposte on the player's last health ends the duel, and after a riposte timeout, retreat or counter Phase returns to EnemyBeat with a fresh CurrentBeat; these are the state-machine invariants RoninGameController and DuelPresenter rely on and none are asserted today | Tests/Core.Tests/DuelDirectorTests.cs |
| U.RON-T3 | 2026-10-14 | chore | open | NUnit tests for GestureClassifier edge cases (add to the existing xUnit Tests/Core.Tests/GestureClassifierTests.cs, Assets/UnityRonin/Core/Combat/GestureClassifier.cs): an empty or single-point path returns no strokes (SwipeInputReader then drops the gesture silently), the angle-bucket boundaries at 22.5, 67.5, 112.5 and 157.5 degrees land in the documented shape, a three-segment zigzag yields three alternating strokes with shared corner points, a short tail under 24px after a real corner is dropped rather than turned into a Thrust, a sharp turn before 24px of travel is treated as jitter and does not split, and downward or leftward drags fold to the same shape; misclassification here silently decides parries and combo bonus hits | — |
| U.RON-T4 | 2026-10-14 | chore | open | NUnit tests for EngageGate and StickVector boundary behavior (add to the existing xUnit Tests/Core.Tests/EngageGateTests.cs, Assets/UnityRonin/Core/Movement/EngageGate.cs and StickVector.cs): a zero or negative enterRadius throws ArgumentOutOfRangeException, a distance exactly equal to EnterRadius engages and exactly equal to ExitRadius releases, the gate can re-engage after releasing, Update returns the same value as State, and StickVector.Magnitude and StickVector.Zero behave as documented; the equality boundaries decide whether a duel triggers or releases at the edge of the enemy zone | — |
| U.RON-T5 | 2026-10-14 | chore | open | NUnit tests for SelfPlayInput's scripted duel answers staying in sync with DuelDirector (Assets/UnityRonin/Runtime/Game/SelfPlayInput.cs CorrectResponse and Perpendicular, currently private in the Unity-only Runtime assembly, so either move that pure mapping into Core or cover it with a Unity EditMode test): for every DuelAction x GestureSymbol beat, the correct response really parries or strikes when fed to DuelDirector and the alwaysLose response never does; the -autoplay/-demo PASS depends on this hand-copied mirror of the scoring rules and it will silently drift if the rules change | — |
| U.RON-T6 | 2026-10-16 | chore | closed | install.ps1 adds this repo's 15 .prose/commands stubs for the shared global commands, so any LLM client finds them | mindattic-agent-standard/install.ps1 run 2026-10-16 |
| U.RON-T7 | 2026-10-16 | chore | closed | Core tests use NUnit instead of xUnit (the MindAttic global test rule): Tests/Core.Tests packages and every test converted, same test count | 866cb76 / Tests/Core.Tests |
| U.RON-T8 | 2026-10-16 | chore | open | Bug hunt: run an exhaustive /bughunt over the whole repo and fix every real bug found (each fix under its own fix ticket), then hunt again; close this only when a full pass finds nothing | user request 2026-10-16: approved repo sweep |
| U.RON-T9 | 2026-10-16 | chore | open | Run the full unit test suite and fix every failure (each under its own fix ticket) until it passes; put the passing run in --ref. No test project: close this wont-do saying so | user request 2026-10-16: approved repo sweep |
| U.RON-T10 | 2026-10-16 | chore | open | Clean up code and docs: remove dead code, stale comments and outdated or duplicated docs, and simplify where behaviour doesn't change (/simplify); tests stay green | user request 2026-10-16: approved repo sweep |
| U.RON-T11 | 2026-10-16 | docs | open | Bring README.md up to date with the code as it is now (what it is, setup, usage, commands, current state), following codex-standard/README-STYLE.md; README.htm regenerated if the repo has one | user request 2026-10-16: approved repo sweep |
| U.RON-T12 | 2026-10-19 | chore | open | Convert tracked source files to LF line endings via .gitattributes, per HOUSE-LAW-13 (low priority) | — |
| U.RON-T13 | 2026-10-20 | docs | open | Publish UnityRonin docs to GitHub Pages via the shared Toolkit DocFX pipeline: run adopt-docs-site.ps1 (README.md as home page, docs/*.md as articles, API reference only where the code builds in CI), enable Pages with build_type=workflow, set the repo homepage URL, retire tools/build-readme.ps1 and README.htm, confirm the docs Actions run is green and https://mindattic.github.io/UnityRonin/ returns 200. Depends on MAT-T8. | — |
