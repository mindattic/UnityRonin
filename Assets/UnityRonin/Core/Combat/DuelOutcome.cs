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
        PlayerTimedOutWasted,

        /// <summary>Riposte: the enemy's counter failed to answer the player's freely-chosen attack. Enemy
        /// takes damage, and if it survives the riposte window reopens for another attack.</summary>
        PlayerRiposteLanded,

        /// <summary>Riposte: the enemy's counter validly answered the player's attack — the "flash slash"
        /// lands on the player instead. Player takes damage.</summary>
        PlayerRiposteCountered,

        /// <summary>Riposte: the enemy backed off rather than risk a counter. No damage either way.</summary>
        PlayerRiposteRetreated,

        /// <summary>Riposte: the player never drew an attack in time. No damage either way.</summary>
        PlayerRiposteTimedOut
    }
}
