using UnityRonin.Core.Util;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class RngTests
    {
        [Fact]
        public void Same_seed_replays_the_identical_sequence()
        {
            var a = new Rng(1234);
            var b = new Rng(1234);
            for (int i = 0; i < 50; i++)
                Assert.Equal(a.NextFloat(), b.NextFloat());
        }

        [Fact]
        public void NextFloat_stays_within_zero_to_one()
        {
            var rng = new Rng(7);
            for (int i = 0; i < 500; i++)
            {
                float v = rng.NextFloat();
                Assert.InRange(v, 0f, 1f);
            }
        }

        [Fact]
        public void NextInt_never_reaches_the_exclusive_bound()
        {
            var rng = new Rng(99);
            for (int i = 0; i < 500; i++)
                Assert.InRange(rng.NextInt(4), 0, 3);
        }
    }
}
