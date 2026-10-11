---
codex: 1
project: UnityRonin
code: UR
layer: stories
status: living
updated: 2026-10-11
---

# Unity Ronin: User stories

> **Status:** ✅ verified (a named test or batch-mode check proves it) · 🟡 partial · ⬜ planned.
> **Evidence:** `TestName` = xUnit in `Tests/Core.Tests`; `batch:name` = a `tools/unity.ps1` batch-mode step.

**Definition of done (every story):**
1. `tools/test.ps1` is green.
2. Each rule has a named test.
3. Anything visible has an owner playtest or an autoplay step (not yet built — see BIBLE §7).
4. It is committed once.

## Movement

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-001 | The on-screen stick ignores small jitter near its center (dead zone) | ✅ | `Input_inside_dead_zone_resolves_to_zero` |
| US-002 | Dragging past the stick's travel radius clamps to full deflection, never overshoots | ✅ | `Input_beyond_full_deflection_clamps_to_unit_magnitude` |
| US-003 | The stick's output rescales continuously from the dead-zone edge to full deflection | ✅ | `Rescaling_is_continuous_from_the_dead_zone_edge_to_full_deflection` |
| US-004 | Stick input is interpreted relative to the camera's facing, not world axes | ✅ | `Pushing_up_with_zero_yaw_moves_along_world_forward`, `Pushing_up_with_camera_yawed_90_degrees_moves_along_world_right` |
| US-005 | The stick covers the bottom half of the screen and appears wherever the thumb lands | 🟡 | `VirtualJoystick` + `JoystickZone` in `DemoContentBuilder`; needs an owner playtest on a touch device |
| US-006 | The camera sits slightly above and behind and smart-follows without a second-finger drag | 🟡 | `SmartFollowCamera`; needs an owner playtest |

## Engagement

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-010 | Coming within an enemy's enter radius starts a duel | ✅ | `Engages_once_the_enemy_is_within_enter_radius` |
| US-011 | The duel doesn't release until the player backs out past a larger exit radius (no flicker at the boundary) | ✅ | `Stays_engaged_inside_the_hysteresis_band_so_it_cannot_flicker_at_the_boundary`, `Releases_back_to_exploring_only_past_the_exit_radius` |
| US-012 | An exit radius that isn't strictly larger than the enter radius is rejected outright | ✅ | `Rejects_an_exit_radius_that_would_allow_flicker` |
| US-013 | Entering a duel swaps the live input surface from the stick to full-screen swipes, and movement goes automatic | 🟡 | `RoninGameController.StartDuel`; needs an owner playtest |

## Duel (katana combat)

| ID | Story | Status | Evidence |
| --- | --- | --- | --- |
| US-020 | A drag is classified as Up/Down/Left/Right only once it clears a minimum distance | ✅ | `Classifies_the_dominant_axis_of_a_clear_swipe`, `A_drag_shorter_than_the_minimum_distance_is_not_a_swipe` |
| US-021 | A diagonal drag resolves to whichever axis travelled further | ✅ | `A_diagonal_drag_resolves_to_whichever_axis_travelled_further` |
| US-022 | Swiping the telegraphed direction against an enemy attack parries it: no damage | ✅ | `Swiping_the_telegraphed_direction_against_an_attack_parries` |
| US-023 | Swiping the wrong direction, or not in time, against an attack hits the player | ✅ | `Swiping_the_wrong_direction_against_an_attack_hits_the_player`, `Not_responding_to_an_attack_in_time_also_hits_the_player` |
| US-024 | Swiping the open direction against an enemy guard strikes it | ✅ | `Swiping_the_open_direction_against_a_guard_strikes_the_enemy` |
| US-025 | Swiping the wrong direction, or not in time, against a guard just wastes the beat — no damage either way | ✅ | `Swiping_the_wrong_direction_against_a_guard_wastes_the_beat_with_no_damage`, `Not_responding_to_a_guard_wastes_the_beat_with_no_damage` |
| US-026 | The duel ends the instant either combatant's health reaches zero | ✅ | `The_duel_ends_as_soon_as_either_side_reaches_zero_health` |
| US-027 | The same seed replays the identical sequence of enemy beats | ✅ | `The_same_seed_replays_the_same_sequence_of_beats`, `Same_seed_replays_the_identical_sequence` (Rng) |
| US-028 | The katana is sheathed while exploring and drawn the instant a duel starts | 🟡 | `SamuraiWeapon.Draw/Sheathe`, wired in `DuelPresenter.Begin/End`; needs an owner playtest |
| US-029 | The HUD shows a direction arrow colored by whether the enemy is attacking or guarding | 🟡 | `DuelTelegraph`; needs an owner playtest |
| US-030 | Winning a duel removes that enemy; losing it immediately restarts the duel (no retreat/game-over flow yet) | ⬜ | See BIBLE §7 — player-defeat flow is planned, not built |
