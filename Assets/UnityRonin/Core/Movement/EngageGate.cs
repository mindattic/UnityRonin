using System;

namespace UnityRonin.Core.Movement
{
    public enum EngageState
    {
        Exploring,
        Engaged
    }

    /// <summary>
    /// Hysteresis gate between free exploration and a katana duel: enters at the (small) enter radius,
    /// only releases back out past the (larger) exit radius, so standing near the boundary can't flicker
    /// the player in and out of the stance every frame.
    /// </summary>
    public sealed class EngageGate
    {
        public float EnterRadius { get; }
        public float ExitRadius { get; }
        public EngageState State { get; private set; } = EngageState.Exploring;

        public EngageGate(float enterRadius, float exitRadius)
        {
            if (enterRadius <= 0f) throw new ArgumentOutOfRangeException(nameof(enterRadius));
            if (exitRadius <= enterRadius) throw new ArgumentException("exitRadius must exceed enterRadius, or the gate can flicker.");
            EnterRadius = enterRadius;
            ExitRadius = exitRadius;
        }

        public EngageState Update(float distanceToNearestEnemy)
        {
            if (State == EngageState.Exploring && distanceToNearestEnemy <= EnterRadius)
                State = EngageState.Engaged;
            else if (State == EngageState.Engaged && distanceToNearestEnemy >= ExitRadius)
                State = EngageState.Exploring;

            return State;
        }
    }
}
