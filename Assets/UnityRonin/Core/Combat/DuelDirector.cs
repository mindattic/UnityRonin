using System;
using UnityRonin.Core.Util;

namespace UnityRonin.Core.Combat
{
    /// <summary>
    /// Pure rules for a one-on-one katana duel: deals a telegraphed beat, scores the player's swipe
    /// against it, and tracks both combatants' health. No UnityEngine reference, no timing of its own —
    /// the presentation layer drives the clock and supplies the swipe (or SwipeDirection.None on timeout).
    /// </summary>
    public sealed class DuelDirector
    {
        private static readonly SwipeDirection[] Directions =
        {
            SwipeDirection.Up, SwipeDirection.Down, SwipeDirection.Left, SwipeDirection.Right
        };

        private readonly Rng _rng;
        private readonly float _telegraphSeconds;
        private readonly float _responseWindowSeconds;

        public int PlayerHealth { get; private set; }
        public int EnemyHealth { get; private set; }
        public DuelBeat CurrentBeat { get; private set; }
        public bool IsOver => PlayerHealth <= 0 || EnemyHealth <= 0;
        public bool PlayerWon => EnemyHealth <= 0 && PlayerHealth > 0;

        public DuelDirector(Rng rng, int playerHealth = 3, int enemyHealth = 3,
            float telegraphSeconds = 0.6f, float responseWindowSeconds = 0.9f)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            PlayerHealth = playerHealth;
            EnemyHealth = enemyHealth;
            _telegraphSeconds = telegraphSeconds;
            _responseWindowSeconds = responseWindowSeconds;
            CurrentBeat = NextBeat();
        }

        private DuelBeat NextBeat()
        {
            var action = _rng.NextFloat() < 0.5f ? DuelAction.Attack : DuelAction.Guard;
            var direction = Directions[_rng.NextInt(Directions.Length)];
            return new DuelBeat(action, direction, _telegraphSeconds, _responseWindowSeconds);
        }

        /// <summary>Scores one beat. Pass <see cref="SwipeDirection.None"/> when the response window expired.</summary>
        public DuelOutcome Resolve(SwipeDirection playerSwipe)
        {
            if (IsOver) throw new InvalidOperationException("Duel already decided.");

            var outcome = Evaluate(playerSwipe);

            if (outcome == DuelOutcome.PlayerHit || outcome == DuelOutcome.PlayerTimedOutUndefended)
                PlayerHealth--;
            if (outcome == DuelOutcome.PlayerStruck)
                EnemyHealth--;

            if (!IsOver) CurrentBeat = NextBeat();
            return outcome;
        }

        private DuelOutcome Evaluate(SwipeDirection playerSwipe)
        {
            bool matched = playerSwipe != SwipeDirection.None && playerSwipe == CurrentBeat.Direction;

            if (CurrentBeat.EnemyAction == DuelAction.Attack)
            {
                if (matched) return DuelOutcome.PlayerParried;
                return playerSwipe == SwipeDirection.None ? DuelOutcome.PlayerTimedOutUndefended : DuelOutcome.PlayerHit;
            }

            if (matched) return DuelOutcome.PlayerStruck;
            return playerSwipe == SwipeDirection.None ? DuelOutcome.PlayerTimedOutWasted : DuelOutcome.PlayerWhiffed;
        }
    }
}
