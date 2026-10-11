using UnityRonin.Core.Movement;
using Xunit;

namespace UnityRonin.Core.Tests
{
    public class CameraRelativeMotionTests
    {
        [Fact]
        public void Pushing_up_with_zero_yaw_moves_along_world_forward()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(new StickVector(0f, 1f), cameraYawDegrees: 0f);
            Assert.Equal(0f, x, 4);
            Assert.Equal(1f, z, 4);
        }

        [Fact]
        public void Pushing_up_with_camera_yawed_90_degrees_moves_along_world_right()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(new StickVector(0f, 1f), cameraYawDegrees: 90f);
            Assert.Equal(1f, x, 4);
            Assert.Equal(0f, z, 4);
        }

        [Fact]
        public void Pushing_right_with_zero_yaw_moves_along_world_right()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(new StickVector(1f, 0f), cameraYawDegrees: 0f);
            Assert.Equal(1f, x, 4);
            Assert.Equal(0f, z, 4);
        }

        [Fact]
        public void Zero_stick_never_produces_motion_regardless_of_yaw()
        {
            var (x, z) = CameraRelativeMotion.ToWorldDirection(StickVector.Zero, cameraYawDegrees: 137f);
            Assert.Equal(0f, x, 4);
            Assert.Equal(0f, z, 4);
        }
    }
}
