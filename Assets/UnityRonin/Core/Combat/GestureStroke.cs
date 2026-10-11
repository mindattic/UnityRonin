namespace UnityRonin.Core.Combat
{
    /// <summary>One classified straight segment of a drawn gesture path.</summary>
    public readonly struct GestureStroke
    {
        public readonly GestureSymbol Symbol;
        public readonly GesturePoint Start;
        public readonly GesturePoint End;

        public GestureStroke(GestureSymbol symbol, GesturePoint start, GesturePoint end)
        {
            Symbol = symbol;
            Start = start;
            End = end;
        }
    }
}
