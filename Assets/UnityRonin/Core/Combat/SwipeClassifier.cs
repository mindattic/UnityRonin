using System;

namespace UnityRonin.Core.Combat
{
    /// <summary>Classifies a raw finger drag (start to release, in pixels) into one of four cardinal swipes.</summary>
    public static class SwipeClassifier
    {
        public static SwipeDirection Classify(float dx, float dy, float minDistancePixels = 60f)
        {
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance < minDistancePixels) return SwipeDirection.None;

            return MathF.Abs(dx) >= MathF.Abs(dy)
                ? (dx >= 0f ? SwipeDirection.Right : SwipeDirection.Left)
                : (dy >= 0f ? SwipeDirection.Up : SwipeDirection.Down);
        }
    }
}
