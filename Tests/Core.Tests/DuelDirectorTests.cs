using UnityRonin.Core.Combat;
using UnityRonin.Core.Util;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class DuelDirectorTests
    {
        private static DuelDirector FindBeat(DuelAction action, out SwipeDirection direction)
        {
            for (uint seed = 1; seed < 10000; seed++)
            {
                var duel = new DuelDirector(new Rng(seed));
                if (duel.CurrentBeat.EnemyAction == action)
                {
                    direction = duel.CurrentBeat.Direction;
                    return duel;
                }
            }
            direction = SwipeDirection.None;
            return null;
        }

        [Fact]
        public void Swiping_the_telegraphed_direction_against_an_attack_parries()
        {
            var duel = FindBeat(DuelAction.Attack, out var direction);
            var outcome = duel.Resolve(direction);
            Assert.Equal(DuelOutcome.PlayerParried, outcome);
            Assert.Equal(3, duel.PlayerHealth);
        }

        [Fact]
        public void Swiping_the_wrong_direction_against_an_attack_hits_the_player()
        {
            var duel = FindBeat(DuelAction.Attack, out var direction);
            var wrong = direction == SwipeDirection.Up ? SwipeDirection.Down : SwipeDirection.Up;
            var outcome = duel.Resolve(wrong);
            Assert.Equal(DuelOutcome.PlayerHit, outcome);
            Assert.Equal(2, duel.PlayerHealth);
        }

        [Fact]
        public void Not_responding_to_an_attack_in_time_also_hits_the_player()
        {
            var duel = FindBeat(DuelAction.Attack, out _);
            var outcome = duel.Resolve(SwipeDirection.None);
            Assert.Equal(DuelOutcome.PlayerTimedOutUndefended, outcome);
            Assert.Equal(2, duel.PlayerHealth);
        }

        [Fact]
        public void Swiping_the_open_direction_against_a_guard_strikes_the_enemy()
        {
            var duel = FindBeat(DuelAction.Guard, out var direction);
            var outcome = duel.Resolve(direction);
            Assert.Equal(DuelOutcome.PlayerStruck, outcome);
            Assert.Equal(2, duel.EnemyHealth);
        }

        [Fact]
        public void Swiping_the_wrong_direction_against_a_guard_wastes_the_beat_with_no_damage()
        {
            var duel = FindBeat(DuelAction.Guard, out var direction);
            var wrong = direction == SwipeDirection.Up ? SwipeDirection.Down : SwipeDirection.Up;
            var outcome = duel.Resolve(wrong);
            Assert.Equal(DuelOutcome.PlayerWhiffed, outcome);
            Assert.Equal(3, duel.PlayerHealth);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void Not_responding_to_a_guard_wastes_the_beat_with_no_damage()
        {
            var duel = FindBeat(DuelAction.Guard, out _);
            var outcome = duel.Resolve(SwipeDirection.None);
            Assert.Equal(DuelOutcome.PlayerTimedOutWasted, outcome);
            Assert.Equal(3, duel.PlayerHealth);
            Assert.Equal(3, duel.EnemyHealth);
        }

        [Fact]
        public void The_duel_ends_as_soon_as_either_side_reaches_zero_health()
        {
            // Always swiping the telegraphed direction parries every attack (player never takes damage)
            // and strikes every guard (enemy takes damage), so with 1 enemy health this must end quickly;
            // the iteration cap guards against a hang if that assumption is ever wrong.
            var duel = new DuelDirector(new Rng(42), playerHealth: 1, enemyHealth: 1);
            for (int i = 0; i < 200 && !duel.IsOver; i++)
                duel.Resolve(duel.CurrentBeat.Direction);

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
                Assert.Equal(a.CurrentBeat.EnemyAction, b.CurrentBeat.EnemyAction);
                Assert.Equal(a.CurrentBeat.Direction, b.CurrentBeat.Direction);
                a.Resolve(a.CurrentBeat.Direction);
                b.Resolve(b.CurrentBeat.Direction);
            }
        }
    }
}
