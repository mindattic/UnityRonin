using System;
using System.Collections.Generic;

namespace UnityRonin.Core.Combat
{
    /// <summary>
    /// Splits one continuous drawn path (finger down to release, in screen pixels) into straight strokes
    /// at each sharp direction change, and classifies each stroke's line into one of the four
    /// <see cref="GestureSymbol"/> shapes. A corner only registers once the path has travelled far enough
    /// since the last one for the turn to be a deliberate new stroke rather than drag jitter.
    /// </summary>
    public static class GestureClassifier
    {
        private const float MinStrokeLength = 24f;
        private const float CornerAngleDegrees = 40f;

        public static IReadOnlyList<GestureStroke> Classify(IReadOnlyList<GesturePoint> path)
        {
            var strokes = new List<GestureStroke>();
            if (path.Count < 2) return strokes;

            int segmentStart = 0;
            for (int i = 2; i < path.Count; i++)
            {
                float baseAngle = AngleDegrees(path[segmentStart], path[i - 1]);
                float candidateAngle = AngleDegrees(path[segmentStart], path[i]);

                if (Distance(path[segmentStart], path[i - 1]) >= MinStrokeLength
                    && AngleDelta(baseAngle, candidateAngle) >= CornerAngleDegrees)
                {
                    strokes.Add(BuildStroke(path[segmentStart], path[i - 1]));
                    segmentStart = i - 1;
                }
            }

            GesturePoint last = path[path.Count - 1];
            if (Distance(path[segmentStart], last) >= MinStrokeLength)
                strokes.Add(BuildStroke(path[segmentStart], last));
            else if (strokes.Count == 0)
                // The whole gesture never travelled far enough to be a line: a tap, not an aborted swipe.
                strokes.Add(new GestureStroke(GestureSymbol.Thrust, path[0], last));

            return strokes;
        }

        private static GestureStroke BuildStroke(GesturePoint start, GesturePoint end) =>
            new GestureStroke(SymbolFor(start, end), start, end);

        private static GestureSymbol SymbolFor(GesturePoint start, GesturePoint end)
        {
            // Folding to 0..180 means a line's shape doesn't depend on which end it was drawn from.
            float folded = Mod(AngleDegrees(start, end), 180f);

            if (folded < 22.5f || folded >= 157.5f) return GestureSymbol.Horizontal;
            if (folded >= 67.5f && folded < 112.5f) return GestureSymbol.Vertical;
            return folded < 90f ? GestureSymbol.Rising : GestureSymbol.Falling;
        }

        private static float AngleDegrees(GesturePoint from, GesturePoint to) =>
            MathF.Atan2(to.Y - from.Y, to.X - from.X) * (180f / MathF.PI);

        private static float AngleDelta(float a, float b) => MathF.Abs(Mod(a - b + 180f, 360f) - 180f);

        private static float Distance(GesturePoint a, GesturePoint b)
        {
            float dx = b.X - a.X, dy = b.Y - a.Y;
            return MathF.Sqrt(dx * dx + dy * dy);
        }

        private static float Mod(float value, float modulus)
        {
            float result = value % modulus;
            return result < 0f ? result + modulus : result;
        }
    }
}
