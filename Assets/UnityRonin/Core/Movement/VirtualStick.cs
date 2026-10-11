using System;

namespace UnityRonin.Core.Movement
{
    /// <summary>
    /// Turns a raw thumb drag into a dead-zoned, clamped, continuously-rescaled stick reading, so the
    /// transparent on-screen stick never jitters near its center and never exceeds full deflection.
    /// </summary>
    public static class VirtualStick
    {
        public static StickVector Resolve(float rawX, float rawY, float deadZone = 0.12f)
        {
            float mag = MathF.Sqrt(rawX * rawX + rawY * rawY);
            if (mag < deadZone || mag <= 0f) return StickVector.Zero;

            float clamped = MathF.Min(mag, 1f);
            float rescaled = (clamped - deadZone) / (1f - deadZone);
            float nx = rawX / mag * rescaled;
            float ny = rawY / mag * rescaled;
            return new StickVector(nx, ny);
        }

        /// <summary>Resolves a stick reading from a raw pixel drag against the stick's max travel radius.</summary>
        public static StickVector FromDrag(float dragX, float dragY, float maxRadiusPixels, float deadZone = 0.12f)
        {
            if (maxRadiusPixels <= 0f) return StickVector.Zero;
            return Resolve(dragX / maxRadiusPixels, dragY / maxRadiusPixels, deadZone);
        }
    }
}
