namespace UnityRonin.Core.Combat
{
    /// <summary>
    /// One shape a drawn gesture segment classifies as: four line strokes, named for their ASCII glyphs
    /// ("-" horizontal, "|" vertical, "/" rising, "\" falling — which end of the line the player started
    /// from isn't part of the symbol; only <see cref="GestureClassifier"/>'s stroke order, not the symbol
    /// itself, captures that), plus <see cref="Thrust"/>, a tap with no line direction at all.
    /// </summary>
    public enum GestureSymbol
    {
        Horizontal,
        Vertical,
        Rising,
        Falling,

        /// <summary>A point-blank tap — the gesture's start and end are the same point, so it has no line
        /// shape to be perpendicular to. See <see cref="DuelDirector"/> for how it's parried.</summary>
        Thrust
    }
}
