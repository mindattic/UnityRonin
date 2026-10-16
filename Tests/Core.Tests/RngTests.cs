using UnityRonin.Core.Util;
using NUnit.Framework;

namespace UnityRonin.Core.Tests
{
    public class RngTests
    {
        [Test]
        public void Same_seed_replays_the_identical_sequence()
        {
            var a = new Rng(1234);
            var b = new Rng(1234);
            for (int i = 0; i < 50; i++)
                Assert.That(b.NextFloat(), Is.EqualTo(a.NextFloat()));
        }

        [Test]
        public void NextFloat_stays_within_zero_to_one()
        {
            var rng = new Rng(7);
            for (int i = 0; i < 500; i++)
            {
                float v = rng.NextFloat();
                Assert.That(v, Is.InRange(0f, 1f));
            }
        }

        [Test]
        public void NextInt_never_reaches_the_exclusive_bound()
        {
            var rng = new Rng(99);
            for (int i = 0; i < 500; i++)
                Assert.That(rng.NextInt(4), Is.InRange(0, 3));
        }
    }
}
