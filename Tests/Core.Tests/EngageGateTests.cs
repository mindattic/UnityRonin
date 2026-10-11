using System;
using UnityRonin.Core.Movement;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class EngageGateTests
    {
        [Fact]
        public void Starts_exploring_and_stays_exploring_outside_enter_radius()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            Assert.Equal(EngageState.Exploring, gate.Update(10f));
        }

        [Fact]
        public void Engages_once_the_enemy_is_within_enter_radius()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            Assert.Equal(EngageState.Engaged, gate.Update(3.5f));
        }

        [Fact]
        public void Stays_engaged_inside_the_hysteresis_band_so_it_cannot_flicker_at_the_boundary()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            gate.Update(3.5f);
            Assert.Equal(EngageState.Engaged, gate.Update(5f));
            Assert.Equal(EngageState.Engaged, gate.Update(4.1f));
        }

        [Fact]
        public void Releases_back_to_exploring_only_past_the_exit_radius()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            gate.Update(3.5f);
            Assert.Equal(EngageState.Exploring, gate.Update(6.5f));
        }

        [Fact]
        public void Rejects_an_exit_radius_that_would_allow_flicker()
        {
            Assert.Throws<ArgumentException>(() => new EngageGate(enterRadius: 5f, exitRadius: 5f));
        }
    }
}
