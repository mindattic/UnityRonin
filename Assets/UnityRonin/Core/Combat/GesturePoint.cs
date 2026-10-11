namespace UnityRonin.Core.Combat
{
    /// <summary>
    /// One point of a drawn gesture path, in whatever 2D unit the presentation layer uses (screen
    /// pixels) — a plain float pair, not UnityEngine.Vector2, so <see cref="GestureClassifier"/> stays
    /// pure C#.
    /// </summary>
    public readonly struct GesturePoint
    {
        public readonly float X;
        public readonly float Y;

        public GesturePoint(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
