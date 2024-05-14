using UnityEngine;

namespace DefaultNamespace
{
    public static class Extensions
    {
        public static Vector3 NormalizeVector(this Vector3 vec, float maxValue)
        {
            return Vector3.Max(Vector3.one * -1, Vector3.Min(Vector3.one, vec / maxValue));
        }
    }
}