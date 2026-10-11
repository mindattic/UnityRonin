using UnityRonin.Core.Movement;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class VirtualStickTests
    {
        [Fact]
        public void Input_inside_dead_zone_resolves_to_zero()
        {
            var stick = VirtualStick.Resolve(0.05f, 0.03f);
            Assert.Equal(0f, stick.X);
            Assert.Equal(0f, stick.Y);
        }

        [Fact]
        public void Input_beyond_full_deflection_clamps_to_unit_magnitude()
        {
            var stick = VirtualStick.FromDrag(500f, 0f, maxRadiusPixels: 120f);
            Assert.Equal(1f, stick.Magnitude, 3);
        }

        [Fact]
        public void Rescaling_is_continuous_from_the_dead_zone_edge_to_full_deflection()
        {
            var atDeadZone = VirtualStick.Resolve(0.12f, 0f, deadZone: 0.12f);
            var atFull = VirtualStick.Resolve(1f, 0f, deadZone: 0.12f);
            Assert.Equal(0f, atDeadZone.Magnitude, 3);
            Assert.Equal(1f, atFull.Magnitude, 3);
        }

        [Fact]
        public void Direction_is_preserved_after_rescaling()
        {
            var stick = VirtualStick.Resolve(0f, 0.5f);
            Assert.True(stick.Y > 0f);
            Assert.Equal(0f, stick.X, 5);
        }
    }
}
