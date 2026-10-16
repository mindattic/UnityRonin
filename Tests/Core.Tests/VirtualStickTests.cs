using UnityRonin.Core.Movement;
using NUnit.Framework;

namespace UnityRonin.Core.Tests
{
    public class VirtualStickTests
    {
        [Test]
        public void Input_inside_dead_zone_resolves_to_zero()
        {
            var stick = VirtualStick.Resolve(0.05f, 0.03f);
            Assert.That(stick.X, Is.EqualTo(0f));
            Assert.That(stick.Y, Is.EqualTo(0f));
        }

        [Test]
        public void Input_beyond_full_deflection_clamps_to_unit_magnitude()
        {
            var stick = VirtualStick.FromDrag(500f, 0f, maxRadiusPixels: 120f);
            Assert.That(stick.Magnitude, Is.EqualTo(1f).Within(0.0005));
        }

        [Test]
        public void Rescaling_is_continuous_from_the_dead_zone_edge_to_full_deflection()
        {
            var atDeadZone = VirtualStick.Resolve(0.12f, 0f, deadZone: 0.12f);
            var atFull = VirtualStick.Resolve(1f, 0f, deadZone: 0.12f);
            Assert.That(atDeadZone.Magnitude, Is.EqualTo(0f).Within(0.0005));
            Assert.That(atFull.Magnitude, Is.EqualTo(1f).Within(0.0005));
        }

        [Test]
        public void Direction_is_preserved_after_rescaling()
        {
            var stick = VirtualStick.Resolve(0f, 0.5f);
            Assert.That(stick.Y > 0f, Is.True);
            Assert.That(stick.X, Is.EqualTo(0f).Within(5e-6));
        }
    }
}
