using System;

namespace UnityRonin.Core.Util
{
    /// <summary>Deterministic xorshift32 PRNG so duels and demos replay identically from a seed.</summary>
    public sealed class Rng
    {
        private uint _state;

        public Rng(uint seed)
        {
            _state = seed == 0 ? 0x9E3779B9u : seed;
        }

        private uint NextUInt()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        public float NextFloat() => (NextUInt() & 0xFFFFFF) / (float)0x1000000;

        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException(nameof(exclusiveMax));
            return (int)(NextUInt() % (uint)exclusiveMax);
        }
    }
}
