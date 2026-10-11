namespace UnityRonin.Core.Combat
{
    /// <summary>Which side's turn it is within a duel beat.</summary>
    public enum DuelPhase
    {
        /// <summary>The enemy has telegraphed an Attack or a Guard opening; <see cref="DuelDirector.CurrentBeat"/>
        /// describes it and the player must answer within the response window.</summary>
        EnemyBeat,

        /// <summary>A parry just succeeded: the player has earned a free attack of their own choosing — no
        /// telegraph, they pick the technique — and the enemy will retreat or try to counter it.</summary>
        PlayerRiposte
    }
}
