using UnityEngine;

namespace DefaultNamespace
{
    public static class Extensions
    {
        public static Vector3 NormalizeVector(this Vector3 vec, float maxValue)
        {
            return Vector3.Max(Vector3.one * -1, Vector3.Min(Vector3.one, vec / maxValue));
        }

        public static float SafeTarget(this ArticulationDrive drive)
        {
            var driveTarget = drive.target;
            if (driveTarget >= drive.upperLimit) driveTarget -= 0.1f;
            if (driveTarget <= drive.lowerLimit) driveTarget += 0.1f;
            return driveTarget;
        }
    }
}