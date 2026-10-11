using System;
using System.Collections.Generic;
using UnityRonin.Core.Util;

namespace UnityRonin.Core.Combat
{
    /// <summary>
    /// Pure rules for a one-on-one katana duel: deals a telegraphed beat, scores the player's drawn
    /// gesture against it, and tracks both combatants' health. No UnityEngine reference, no timing of its
    /// own — the presentation layer drives the clock and turns the raw drag into strokes (or an empty list
    /// on timeout) via <see cref="GestureClassifier"/>.
    ///
    /// Only the gesture's first stroke resolves an enemy beat: against a cutting Attack it must be the
    /// PERPENDICULAR shape (a cross-block — "/" stops "\", "|" stops "-"); against a Thrust attack it must
    /// MATCH (met head-on — a cross-block has no purchase on a straight point); against a Guard it must
    /// MATCH the open shape, cut or thrust alike. Striking a Guard opening with strokes chained onto the
    /// first — drawn in one continuous motion, so the end of one and the start of the next are the same
    /// point — lands as a bonus hit each, as long as consecutive strokes keep alternating shape; repeating
    /// a shape means re-cocking the blade from scratch, which costs a fresh, separate stroke.
    ///
    /// A successful parry doesn't score damage on the spot — it earns a <see cref="DuelPhase.PlayerRiposte"/>
    /// window instead: a free attack of the player's own choosing, answered by <see cref="ResolveRiposte"/>.
    /// The enemy then retreats (safe) or counters with a technique of its own, scored by the same
    /// <see cref="IsValidParry"/> rule mirrored — land the riposte and, if the enemy survives, the window
    /// reopens for another; get countered and the "flash slash" costs the player a hit instead. This is the
    /// duel's real combo: chained risk/reward exchanges, not a bonus tacked onto one gesture.
    /// </summary>
    public sealed class DuelDirector
    {
        private static readonly GestureSymbol[] Symbols =
        {
            GestureSymbol.Horizontal, GestureSymbol.Vertical, GestureSymbol.Rising, GestureSymbol.Falling,
            GestureSymbol.Thrust
        };

        private readonly Rng _rng;
        private readonly float _telegraphSeconds;
        private readonly float _responseWindowSeconds;
        private readonly float _retreatChance;

        public int PlayerHealth { get; private set; }
        public int EnemyHealth { get; private set; }
        public DuelPhase Phase { get; private set; } = DuelPhase.EnemyBeat;

        /// <summary>Only meaningful while <see cref="Phase"/> is <see cref="DuelPhase.EnemyBeat"/>.</summary>
        public DuelBeat CurrentBeat { get; private set; }

        /// <summary>How long the player has to draw a riposte attack once <see cref="Phase"/> becomes
        /// <see cref="DuelPhase.PlayerRiposte"/>.</summary>
        public float RiposteWindowSeconds { get; }

        public bool IsOver => PlayerHealth <= 0 || EnemyHealth <= 0;
        public bool PlayerWon => EnemyHealth <= 0 && PlayerHealth > 0;

        public DuelDirector(Rng rng, int playerHealth = 3, int enemyHealth = 3,
            float telegraphSeconds = 0.6f, float responseWindowSeconds = 0.9f,
            float retreatChance = 0.3f, float riposteWindowSeconds = 0.6f)
        {
            _rng = rng ?? throw new ArgumentNullException(nameof(rng));
            PlayerHealth = playerHealth;
            EnemyHealth = enemyHealth;
            _telegraphSeconds = telegraphSeconds;
            _responseWindowSeconds = responseWindowSeconds;
            _retreatChance = retreatChance;
            RiposteWindowSeconds = riposteWindowSeconds;
            CurrentBeat = NextBeat();
        }

        private DuelBeat NextBeat()
        {
            var action = _rng.NextFloat() < 0.5f ? DuelAction.Attack : DuelAction.Guard;
            var symbol = Symbols[_rng.NextInt(Symbols.Length)];
            return new DuelBeat(action, symbol, _telegraphSeconds, _responseWindowSeconds);
        }

        /// <summary>Scores one enemy beat against the strokes classified from the player's drawn gesture.
        /// Pass an empty list when the response window expired. Only valid while <see cref="Phase"/> is
        /// <see cref="DuelPhase.EnemyBeat"/>.</summary>
        public DuelOutcome Resolve(IReadOnlyList<GestureStroke> strokes)
        {
            if (IsOver) throw new InvalidOperationException("Duel already decided.");
            if (Phase != DuelPhase.EnemyBeat) throw new InvalidOperationException("Not awaiting a response to an enemy beat.");

            var outcome = Evaluate(strokes);

            if (outcome == DuelOutcome.PlayerHit || outcome == DuelOutcome.PlayerTimedOutUndefended)
                PlayerHealth--;
            if (outcome == DuelOutcome.PlayerStruck)
                EnemyHealth -= 1 + ComboBonusHits(strokes);

            if (outcome == DuelOutcome.PlayerParried)
                Phase = DuelPhase.PlayerRiposte; // the real reward: see class doc and ResolveRiposte
            else if (!IsOver)
                CurrentBeat = NextBeat();

            return outcome;
        }

        /// <summary>Scores the player's freely-chosen riposte attack during a <see cref="DuelPhase.PlayerRiposte"/>
        /// window. Pass an empty list when the window expired. Only valid while <see cref="Phase"/> is
        /// <see cref="DuelPhase.PlayerRiposte"/>.</summary>
        public DuelOutcome ResolveRiposte(IReadOnlyList<GestureStroke> strokes)
        {
            if (IsOver) throw new InvalidOperationException("Duel already decided.");
            if (Phase != DuelPhase.PlayerRiposte) throw new InvalidOperationException("Not in a riposte window.");

            var outcome = EvaluateRiposte(strokes);

            if (outcome == DuelOutcome.PlayerRiposteCountered) PlayerHealth--;
            if (outcome == DuelOutcome.PlayerRiposteLanded) EnemyHealth -= 1 + ComboBonusHits(strokes);

            if (outcome == DuelOutcome.PlayerRiposteLanded && !IsOver)
            {
                // The enemy's counter failed and it's still standing: the chain continues, same phase, no
                // new enemy beat yet.
            }
            else
            {
                Phase = DuelPhase.EnemyBeat;
                if (!IsOver) CurrentBeat = NextBeat();
            }

            return outcome;
        }

        private DuelOutcome EvaluateRiposte(IReadOnlyList<GestureStroke> strokes)
        {
            if (strokes.Count == 0) return DuelOutcome.PlayerRiposteTimedOut;
            if (_rng.NextFloat() < _retreatChance) return DuelOutcome.PlayerRiposteRetreated;

            var counterSymbol = Symbols[_rng.NextInt(Symbols.Length)];
            return IsValidParry(strokes[0].Symbol, counterSymbol)
                ? DuelOutcome.PlayerRiposteCountered
                : DuelOutcome.PlayerRiposteLanded;
        }

        private DuelOutcome Evaluate(IReadOnlyList<GestureStroke> strokes)
        {
            bool hasStroke = strokes.Count > 0;
            bool matched = hasStroke && IsCorrectResponse(CurrentBeat, strokes[0].Symbol);

            if (CurrentBeat.EnemyAction == DuelAction.Attack)
            {
                if (matched) return DuelOutcome.PlayerParried;
                return hasStroke ? DuelOutcome.PlayerHit : DuelOutcome.PlayerTimedOutUndefended;
            }

            if (matched) return DuelOutcome.PlayerStruck;
            return hasStroke ? DuelOutcome.PlayerWhiffed : DuelOutcome.PlayerTimedOutWasted;
        }

        /// <summary>Extra hits from strokes chained after the one that resolved the beat: each must differ
        /// from the one before it, or the chain breaks there.</summary>
        private static int ComboBonusHits(IReadOnlyList<GestureStroke> strokes)
        {
            int bonus = 0;
            for (int i = 1; i < strokes.Count; i++)
            {
                if (strokes[i].Symbol == strokes[i - 1].Symbol) break;
                bonus++;
            }
            return bonus;
        }

        private static bool IsCorrectResponse(DuelBeat beat, GestureSymbol firstStroke) =>
            beat.EnemyAction == DuelAction.Attack
                ? IsValidParry(beat.Symbol, firstStroke)
                : beat.Symbol == firstStroke;

        // A cut is cross-blocked by the perpendicular shape; a thrust has no perpendicular to offer and is
        // only stopped by meeting its point head-on with a thrust of your own. Mirrored for the enemy's own
        // counter against a player riposte attack (see EvaluateRiposte): "attack" is whichever side is
        // striking, "response" is whichever side is defending.
        private static bool IsValidParry(GestureSymbol attack, GestureSymbol response) =>
            attack == GestureSymbol.Thrust ? response == GestureSymbol.Thrust : IsPerpendicular(attack, response);

        private static bool IsPerpendicular(GestureSymbol a, GestureSymbol b) => (a, b) switch
        {
            (GestureSymbol.Horizontal, GestureSymbol.Vertical) => true,
            (GestureSymbol.Vertical, GestureSymbol.Horizontal) => true,
            (GestureSymbol.Rising, GestureSymbol.Falling) => true,
            (GestureSymbol.Falling, GestureSymbol.Rising) => true,
            _ => false
        };
    }
}
