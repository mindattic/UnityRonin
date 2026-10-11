namespace UnityRonin.Core.Combat
{
    /// <summary>What the enemy telegraphs for one beat of the duel, and how long the player has to answer it.</summary>
    public enum DuelAction
    {
        /// <summary>Enemy strikes from <see cref="DuelBeat.Direction"/>; the player must swipe the same way to parry.</summary>
        Attack,

        /// <summary>Enemy guards, leaving <see cref="DuelBeat.Direction"/> open; the player must swipe it to strike.</summary>
        Guard
    }

    public readonly struct DuelBeat
    {
        public readonly DuelAction EnemyAction;
        public readonly SwipeDirection Direction;
        public readonly float TelegraphSeconds;
        public readonly float ResponseWindowSeconds;

        public DuelBeat(DuelAction enemyAction, SwipeDirection direction, float telegraphSeconds, float responseWindowSeconds)
        {
            EnemyAction = enemyAction;
            Direction = direction;
            TelegraphSeconds = telegraphSeconds;
            ResponseWindowSeconds = responseWindowSeconds;
        }
    }
}
