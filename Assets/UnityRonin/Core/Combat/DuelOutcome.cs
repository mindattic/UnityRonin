namespace UnityRonin.Core.Combat
{
    public enum DuelOutcome
    {
        /// <summary>Enemy attacked; player swiped the same direction in time. No damage.</summary>
        PlayerParried,

        /// <summary>Enemy guarded; player swiped the open direction in time. Enemy takes damage.</summary>
        PlayerStruck,

        /// <summary>Enemy attacked; player swiped the wrong direction. Player takes damage.</summary>
        PlayerHit,

        /// <summary>Enemy guarded; player swiped the wrong direction. Wasted beat, no damage either way.</summary>
        PlayerWhiffed,

        /// <summary>Enemy attacked and the player never swiped in time. Player takes damage.</summary>
        PlayerTimedOutUndefended,

        /// <summary>Enemy guarded and the player never swiped in time. Wasted beat, no damage either way.</summary>
        PlayerTimedOutWasted
    }
}
