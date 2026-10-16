using System;
using UnityRonin.Core.Movement;
using NUnit.Framework;

namespace UnityRonin.Core.Tests
{
    public class EngageGateTests
    {
        [Test]
        public void Starts_exploring_and_stays_exploring_outside_enter_radius()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            Assert.That(gate.Update(10f), Is.EqualTo(EngageState.Exploring));
        }

        [Test]
        public void Engages_once_the_enemy_is_within_enter_radius()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            Assert.That(gate.Update(3.5f), Is.EqualTo(EngageState.Engaged));
        }

        [Test]
        public void Stays_engaged_inside_the_hysteresis_band_so_it_cannot_flicker_at_the_boundary()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            gate.Update(3.5f);
            Assert.That(gate.Update(5f), Is.EqualTo(EngageState.Engaged));
            Assert.That(gate.Update(4.1f), Is.EqualTo(EngageState.Engaged));
        }

        [Test]
        public void Releases_back_to_exploring_only_past_the_exit_radius()
        {
            var gate = new EngageGate(enterRadius: 4f, exitRadius: 6f);
            gate.Update(3.5f);
            Assert.That(gate.Update(6.5f), Is.EqualTo(EngageState.Exploring));
        }

        [Test]
        public void Rejects_an_exit_radius_that_would_allow_flicker()
        {
            Assert.Throws<ArgumentException>(() => new EngageGate(enterRadius: 5f, exitRadius: 5f));
        }
    }
}
