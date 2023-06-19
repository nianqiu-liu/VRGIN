using System.Linq;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public static class Calculator
    {
        public static float Distance(float worldValue)
        {
            return worldValue / VR.Settings.IPDScale * VR.Context.UnitToMeter;
        }

        public static float Angle(Vector3 v1, Vector3 v2)
        {
            var current = Mathf.Atan2(v1.x, v1.z) * 57.29578f;
            var target = Mathf.Atan2(v2.x, v2.z) * 57.29578f;
            return Mathf.DeltaAngle(current, target);
        }

        public static Vector3 GetForwardVector(Quaternion rotation)
        {
            var vector = rotation * Vector3.forward;
            return new Vector3[2]
            {
                Vector3.ProjectOnPlane(vector, Vector3.up),
                Vector3.ProjectOnPlane(rotation * (vector.y > 0f ? Vector3.down : Vector3.up), Vector3.up)
            }.OrderByDescending((Vector3 v) => v.sqrMagnitude).First().normalized;
        }
    }
}
