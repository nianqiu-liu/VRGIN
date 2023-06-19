using UnityEngine;

namespace VRGIN.Helpers
{
    public static class QuaternionExtensions
    {
        public static Vector3 ToPitchYawRollRad(this Quaternion rotation)
        {
            var z = Mathf.Atan2(2f * rotation.y * rotation.w - 2f * rotation.x * rotation.z, 1f - 2f * rotation.y * rotation.y - 2f * rotation.z * rotation.z);
            var x = Mathf.Atan2(2f * rotation.x * rotation.w - 2f * rotation.y * rotation.z, 1f - 2f * rotation.x * rotation.x - 2f * rotation.z * rotation.z);
            var y = Mathf.Asin(2f * rotation.x * rotation.y + 2f * rotation.z * rotation.w);
            return new Vector3(x, y, z);
        }
    }
}
