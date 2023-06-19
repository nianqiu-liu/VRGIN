using UnityEngine;

namespace VRGIN.Helpers
{
	public static class QuaternionExtensions
	{
		public static Vector3 ToPitchYawRollRad(this Quaternion rotation)
		{
			float z = Mathf.Atan2(2f * rotation.y * rotation.w - 2f * rotation.x * rotation.z, 1f - 2f * rotation.y * rotation.y - 2f * rotation.z * rotation.z);
			float x = Mathf.Atan2(2f * rotation.x * rotation.w - 2f * rotation.y * rotation.z, 1f - 2f * rotation.x * rotation.x - 2f * rotation.z * rotation.z);
			float y = Mathf.Asin(2f * rotation.x * rotation.y + 2f * rotation.z * rotation.w);
			return new Vector3(x, y, z);
		}
	}
}
