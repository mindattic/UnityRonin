namespace UnityRonin.Core.Combat
{
    /// <summary>What the enemy telegraphs for one beat of the duel, and how long the player has to answer it.</summary>
    public enum DuelAction
    {
        /// <summary>Enemy strikes along <see cref="DuelBeat.Symbol"/>; the player must draw the perpendicular
        /// shape — a cross-block, "/" against "\" or "|" against "-" — to parry it, or a matching Thrust to
        /// meet a Thrust attack head-on.</summary>
        Attack,

        /// <summary>Enemy guards, leaving <see cref="DuelBeat.Symbol"/> open; the player must draw that same
        /// shape to strike it.</summary>
        Guard
    }

    public readonly struct DuelBeat
    {
        public readonly DuelAction EnemyAction;
        public readonly GestureSymbol Symbol;
        public readonly float TelegraphSeconds;
        public readonly float ResponseWindowSeconds;

        public DuelBeat(DuelAction enemyAction, GestureSymbol symbol, float telegraphSeconds, float responseWindowSeconds)
        {
            EnemyAction = enemyAction;
            Symbol = symbol;
            TelegraphSeconds = telegraphSeconds;
            ResponseWindowSeconds = responseWindowSeconds;
        }
    }
}
