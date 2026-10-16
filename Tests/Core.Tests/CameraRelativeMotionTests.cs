using UnityRonin.Core.Movement;
using NUnit.Framework;

namespace UnityRonin.Core.Tests
{
    public class CameraRelativeMotionTests
    {
        [Test]
        public void Pushing_up_with_zero_yaw_moves_along_world_forward()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(new StickVector(0f, 1f), cameraYawDegrees: 0f);
            Assert.That(x, Is.EqualTo(0f).Within(5e-5));
            Assert.That(z, Is.EqualTo(1f).Within(5e-5));
        }

        [Test]
        public void Pushing_up_with_camera_yawed_90_degrees_moves_along_world_right()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(new StickVector(0f, 1f), cameraYawDegrees: 90f);
            Assert.That(x, Is.EqualTo(1f).Within(5e-5));
            Assert.That(z, Is.EqualTo(0f).Within(5e-5));
        }

        [Test]
        public void Pushing_right_with_zero_yaw_moves_along_world_right()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(new StickVector(1f, 0f), cameraYawDegrees: 0f);
            Assert.That(x, Is.EqualTo(1f).Within(5e-5));
            Assert.That(z, Is.EqualTo(0f).Within(5e-5));
        }

        [Test]
        public void Zero_stick_never_produces_motion_regardless_of_yaw()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(StickVector.Zero, cameraYawDegrees: 137f);
            Assert.That(x, Is.EqualTo(0f).Within(5e-5));
            Assert.That(z, Is.EqualTo(0f).Within(5e-5));
        }
    }
}
