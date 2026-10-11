using System;

namespace UnityRonin.Core.Movement
{
    /// <summary>Rotates a screen-space stick reading into a world-space XZ heading by the camera's yaw.</summary>
    public static class CameraRelativeMotion
    {
        public static (float X, float Z) ToWorldDirection(StickVector stick, float cameraYawDegrees)
        {
            float yaw = cameraYawDegrees * MathF.PI / 180f;
            float forwardX = MathF.Sin(yaw);
            float forwardZ = MathF.Cos(yaw);
            float rightX = MathF.Cos(yaw);
            float rightZ = -MathF.Sin(yaw);

            float x = rightX * stick.X + forwardX * stick.Y;
            float z = rightZ * stick.X + forwardZ * stick.Y;
            return (x, z);
        }
    }
}
