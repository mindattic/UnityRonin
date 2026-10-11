using System.Collections.Generic;
using UnityRonin.Core.Combat;
using UnityRonin.Core.Util;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class DuelDirectorTests
    {
        private static readonly GesturePoint Origin = new GesturePoint(0f, 0f);

        private static DuelDirector FindBeat(DuelAction action, GestureSymbol symbol, out DuelDirector duel)
        {
            for (uint seed = 1; seed < 10000; seed++)
            {
                duel = new DuelDirector(new Rng(seed));
                if (duel.CurrentBeat.EnemyAction == action && duel.CurrentBeat.Symbol == symbol) return duel;
            }
            duel = null;
            return null;
        }

        /// <summary>Finds a seed whose first beat is a Horizontal attack, parries it (guaranteed Parried, since
        /// Vertical is Horizontal's perpendicular), and returns the duel sitting in a fresh riposte window.</summary>
        private static DuelDirector ReachRiposte(float retreatChance = 0.3f)
        {
            for (uint seed = 1; seed < 10000; seed++)
            {
                var duel = new DuelDirector(new Rng(seed), retreatChance: retreatChance);
                if (duel.CurrentBeat.EnemyAction != DuelAction.Attack || duel.CurrentBeat.Symbol != GestureSymbol.Horizontal) continue;
                duel.Resolve(Strokes(GestureSymbol.Vertical));
                return duel;
            }
            return null;
        }

        /// <summary>Searches for a seed where resolving a riposte (after a guaranteed parry) yields exactly
        /// <paramref name="desiredOutcome"/>, with the enemy's counter never allowed to just retreat.</summary>
        private static DuelDirector ReachRiposteOutcome(DuelOutcome desiredOutcome, out DuelOutcome actualOutcome,
            int enemyHealth = 3)
        {
            for (uint seed = 1; seed < 10000; seed++)
            {
                var duel = new DuelDirector(new Rng(seed), enemyHealth: enemyHealth, retreatChance: 0f);
                if (duel.CurrentBeat.EnemyAction != DuelAction.Attack || duel.CurrentBeat.Symbol != GestureSymbol.Horizontal) continue;
                duel.Resolve(Strokes(GestureSymbol.Vertical));
                var outcome = duel.ResolveRiposte(Strokes(GestureSymbol.Horizontal));
                if (outcome == desiredOutcome)
                {
                    actualOutcome = outcome;
                    return duel;
                }
            }
            actualOutcome = default;
            return null;
        }

        private static IReadOnlyList<GestureStroke> Strokes(params GestureSymbol[] symbols)
        {
            var strokes = new List<GestureStroke>();
            foreach (var symbol in symbols)
                strokes.Add(new GestureStroke(symbol, Origin, Origin));
            return strokes;
        }

        [Fact]
        public void Perpendicular_stroke_against_a_cutting_attack_parries()
        {
            FindBeat(DuelAction.Attack, GestureSymbol.Horizontal, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Vertical));
            Assert.Equal(DuelOutcome.PlayerParried, outcome);
            Assert.Equal(3, duel.PlayerHealth);
        }

        [Fact]
        public void Matching_stroke_against_a_cutting_attack_hits_the_player()
        {
            FindBeat(DuelAction.Attack, GestureSymbol.Horizontal, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Horizontal));
            Assert.Equal(DuelOutcome.PlayerHit, outcome);
            Assert.Equal(2, duel.PlayerHealth);
        }

        [Fact]
        public void Thrusting_against_a_cutting_attack_does_not_parry_it()
        {
            FindBeat(DuelAction.Attack, GestureSymbol.Horizontal, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Thrust));
            Assert.Equal(DuelOutcome.PlayerHit, outcome);
        }

        [Fact]
        public void Matching_thrust_against_a_thrust_attack_parries()
        {
            FindBeat(DuelAction.Attack, GestureSymbol.Thrust, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Thrust));
            Assert.Equal(DuelOutcome.PlayerParried, outcome);
            Assert.Equal(3, duel.PlayerHealth);
        }

        [Fact]
        public void A_cross_block_does_not_stop_a_thrust_attack()
        {
            FindBeat(DuelAction.Attack, GestureSymbol.Thrust, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Vertical));
            Assert.Equal(DuelOutcome.PlayerHit, outcome);
            Assert.Equal(2, duel.PlayerHealth);
        }

        [Fact]
        public void Not_responding_to_an_attack_in_time_also_hits_the_player()
        {
            FindBeat(DuelAction.Attack, GestureSymbol.Horizontal, out var duel);
            var outcome = duel.Resolve(Strokes());
            Assert.Equal(DuelOutcome.PlayerTimedOutUndefended, outcome);
            Assert.Equal(2, duel.PlayerHealth);
        }

        [Fact]
        public void Matching_the_open_shape_against_a_guard_strikes_the_enemy()
        {
            FindBeat(DuelAction.Guard, GestureSymbol.Vertical, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Vertical));
            Assert.Equal(DuelOutcome.PlayerStruck, outcome);
            Assert.Equal(2, duel.EnemyHealth);
        }

        [Fact]
        public void Matching_a_thrust_guard_opening_strikes_the_enemy()
        {
            FindBeat(DuelAction.Guard, GestureSymbol.Thrust, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Thrust));
            Assert.Equal(DuelOutcome.PlayerStruck, outcome);
            Assert.Equal(2, duel.EnemyHealth);
        }

        [Fact]
        public void Wrong_shape_against_a_guard_wastes_the_beat_with_no_damage()
        {
            FindBeat(DuelAction.Guard, GestureSymbol.Vertical, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Horizontal));
            Assert.Equal(DuelOutcome.PlayerWhiffed, outcome);
            Assert.Equal(3, duel.PlayerHealth);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void Not_responding_to_a_guard_wastes_the_beat_with_no_damage()
        {
            FindBeat(DuelAction.Guard, GestureSymbol.Vertical, out var duel);
            var outcome = duel.Resolve(Strokes());
            Assert.Equal(DuelOutcome.PlayerTimedOutWasted, outcome);
            Assert.Equal(3, duel.PlayerHealth);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void Chained_strokes_alternating_shape_land_a_bonus_hit_each()
        {
            FindBeat(DuelAction.Guard, GestureSymbol.Horizontal, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Horizontal, GestureSymbol.Vertical, GestureSymbol.Horizontal));
            Assert.Equal(DuelOutcome.PlayerStruck, outcome);
            Assert.Equal(0, duel.EnemyHealth); // base hit + 2 bonus combo hits, from a starting 3
        }

        [Fact]
        public void Repeating_the_same_shape_breaks_the_combo_chain()
        {
            FindBeat(DuelAction.Guard, GestureSymbol.Horizontal, out var duel);
            var outcome = duel.Resolve(Strokes(GestureSymbol.Horizontal, GestureSymbol.Horizontal, GestureSymbol.Vertical));
            Assert.Equal(DuelOutcome.PlayerStruck, outcome);
            Assert.Equal(2, duel.EnemyHealth); // base hit only; the chain breaks at the repeat
        }

        [Fact]
        public void Successful_parry_opens_a_riposte_window_instead_of_dealing_immediate_damage()
        {
            var duel = ReachRiposte();
            Assert.Equal(DuelPhase.PlayerRiposte, duel.Phase);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void Riposte_timeout_wastes_the_window_with_no_damage()
        {
            var duel = ReachRiposte();
            var outcome = duel.ResolveRiposte(Strokes());
            Assert.Equal(DuelOutcome.PlayerRiposteTimedOut, outcome);
            Assert.Equal(DuelPhase.EnemyBeat, duel.Phase);
            Assert.Equal(3, duel.PlayerHealth);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void Riposte_retreat_ends_the_window_safely()
        {
            var duel = ReachRiposte(retreatChance: 1f);
            var outcome = duel.ResolveRiposte(Strokes(GestureSymbol.Horizontal));
            Assert.Equal(DuelOutcome.PlayerRiposteRetreated, outcome);
            Assert.Equal(DuelPhase.EnemyBeat, duel.Phase);
            Assert.Equal(3, duel.PlayerHealth);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void Riposte_lands_when_the_enemys_counter_fails()
        {
            var duel = ReachRiposteOutcome(DuelOutcome.PlayerRiposteLanded, out var outcome);
            Assert.Equal(DuelOutcome.PlayerRiposteLanded, outcome);
            Assert.Equal(2, duel.EnemyHealth);
            Assert.Equal(DuelPhase.PlayerRiposte, duel.Phase); // enemy survived; the chain continues
        }

        [Fact]
        public void Riposte_countered_when_the_enemys_counter_matches()
        {
            var duel = ReachRiposteOutcome(DuelOutcome.PlayerRiposteCountered, out var outcome);
            Assert.Equal(DuelOutcome.PlayerRiposteCountered, outcome);
            Assert.Equal(2, duel.PlayerHealth);
            Assert.Equal(DuelPhase.EnemyBeat, duel.Phase);
        }

        [Fact]
        public void A_chained_combo_of_riposte_lands_can_defeat_the_enemy_without_returning_to_enemy_beat()
        {
            for (uint seed = 1; seed < 10000; seed++)
            {
                var duel = new DuelDirector(new Rng(seed), enemyHealth: 2, retreatChance: 0f);
                if (duel.CurrentBeat.EnemyAction != DuelAction.Attack || duel.CurrentBeat.Symbol != GestureSymbol.Horizontal) continue;
                duel.Resolve(Strokes(GestureSymbol.Vertical));

                var first = duel.ResolveRiposte(Strokes(GestureSymbol.Horizontal));
                if (first != DuelOutcome.PlayerRiposteLanded) continue;
                Assert.Equal(DuelPhase.PlayerRiposte, duel.Phase); // one health left; the chain stays open

                var second = duel.ResolveRiposte(Strokes(GestureSymbol.Horizontal));
                if (second != DuelOutcome.PlayerRiposteLanded) continue;

                Assert.True(duel.IsOver);
                Assert.True(duel.PlayerWon);
                Assert.Equal(0, duel.EnemyHealth);
                return;
            }
            Assert.Fail("No seed under 10000 produced two consecutive riposte lands.");
        }

        [Fact]
        public void The_duel_ends_as_soon_as_either_side_reaches_zero_health()
        {
            // Always answering correctly (perpendicular to a cut, matching a thrust, matching a guard) parries
            // every attack and strikes every guard, and declining every riposte bonus is always safe, so with
            // 1 enemy health this must end quickly in a win; the iteration cap guards against a hang if that
            // assumption is ever wrong.
            var duel = new DuelDirector(new Rng(42), playerHealth: 1, enemyHealth: 1);
            for (int i = 0; i < 200 && !duel.IsOver; i++)
            {
                if (duel.Phase == DuelPhase.PlayerRiposte)
                {
                    duel.ResolveRiposte(Strokes()); // decline every bonus attack: free upside not worth the risk here
                    continue;
                }
                var beat = duel.CurrentBeat;
                var response = beat.Symbol == GestureSymbol.Thrust
                    ? GestureSymbol.Thrust
                    : beat.EnemyAction == DuelAction.Attack
                        ? Perpendicular(beat.Symbol)
                        : beat.Symbol;
                duel.Resolve(Strokes(response));
            }

            Assert.True(duel.IsOver);
            Assert.True(duel.PlayerWon);
        }

        [Fact]
        public void The_same_seed_replays_the_same_sequence_of_beats()
        {
            var a = new DuelDirector(new Rng(2024));
            var b = new DuelDirector(new Rng(2024));
            for (int i = 0; i < 20 && !a.IsOver; i++)
            {
                Assert.Equal(a.Phase, b.Phase);
                if (a.Phase == DuelPhase.PlayerRiposte)
                {
                    var riposteA = a.ResolveRiposte(Strokes(GestureSymbol.Horizontal));
                    var riposteB = b.ResolveRiposte(Strokes(GestureSymbol.Horizontal));
                    Assert.Equal(riposteA, riposteB);
                    continue;
                }
                Assert.Equal(a.CurrentBeat.EnemyAction, b.CurrentBeat.EnemyAction);
                Assert.Equal(a.CurrentBeat.Symbol, b.CurrentBeat.Symbol);
                a.Resolve(Strokes(a.CurrentBeat.Symbol));
                b.Resolve(Strokes(b.CurrentBeat.Symbol));
            }
        }

        private static GestureSymbol Perpendicular(GestureSymbol symbol) => symbol switch
        {
            GestureSymbol.Horizontal => GestureSymbol.Vertical,
            GestureSymbol.Vertical => GestureSymbol.Horizontal,
            GestureSymbol.Rising => GestureSymbol.Falling,
            GestureSymbol.Falling => GestureSymbol.Rising,
            _ => GestureSymbol.Horizontal
        };
    }
}
