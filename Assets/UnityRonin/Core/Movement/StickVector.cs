using System;

namespace UnityRonin.Core.Movement
{
    /// <summary>A resolved virtual-stick reading: -1..1 on each axis, magnitude never exceeding 1.</summary>
    public readonly struct StickVector
    {
        public readonly float X;
        public readonly float Y;

        public StickVector(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float Magnitude => MathF.Sqrt(X * X + Y * Y);

        public static readonly StickVector Zero = new StickVector(0f, 0f);
    }
}
