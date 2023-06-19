using System;
using UnityEngine;

namespace Valve.VR
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
    public class SteamVR_Frustum : MonoBehaviour
    {
        public SteamVR_TrackedObject.EIndex index;

        public float fovLeft = 45f;

        public float fovRight = 45f;

        public float fovTop = 45f;

        public float fovBottom = 45f;

        public float nearZ = 0.5f;

        public float farZ = 2.5f;

        public void UpdateModel()
        {
            fovLeft = Mathf.Clamp(fovLeft, 1f, 89f);
            fovRight = Mathf.Clamp(fovRight, 1f, 89f);
            fovTop = Mathf.Clamp(fovTop, 1f, 89f);
            fovBottom = Mathf.Clamp(fovBottom, 1f, 89f);
            farZ = Mathf.Max(farZ, nearZ + 0.01f);
            nearZ = Mathf.Clamp(nearZ, 0.01f, farZ - 0.01f);
            var num = Mathf.Sin((0f - fovLeft) * ((float)Math.PI / 180f));
            var num2 = Mathf.Sin(fovRight * ((float)Math.PI / 180f));
            var num3 = Mathf.Sin(fovTop * ((float)Math.PI / 180f));
            var num4 = Mathf.Sin((0f - fovBottom) * ((float)Math.PI / 180f));
            var num5 = Mathf.Cos((0f - fovLeft) * ((float)Math.PI / 180f));
            var num6 = Mathf.Cos(fovRight * ((float)Math.PI / 180f));
            var num7 = Mathf.Cos(fovTop * ((float)Math.PI / 180f));
            var num8 = Mathf.Cos((0f - fovBottom) * ((float)Math.PI / 180f));
            var array = new Vector3[8]
            {
                new Vector3(num * nearZ / num5, num3 * nearZ / num7, nearZ),
                new Vector3(num2 * nearZ / num6, num3 * nearZ / num7, nearZ),
                new Vector3(num2 * nearZ / num6, num4 * nearZ / num8, nearZ),
                new Vector3(num * nearZ / num5, num4 * nearZ / num8, nearZ),
                new Vector3(num * farZ / num5, num3 * farZ / num7, farZ),
                new Vector3(num2 * farZ / num6, num3 * farZ / num7, farZ),
                new Vector3(num2 * farZ / num6, num4 * farZ / num8, farZ),
                new Vector3(num * farZ / num5, num4 * farZ / num8, farZ)
            };
            var array2 = new int[48]
            {
                0, 4, 7, 0, 7, 3, 0, 7, 4, 0,
                3, 7, 1, 5, 6, 1, 6, 2, 1, 6,
                5, 1, 2, 6, 0, 4, 5, 0, 5, 1,
                0, 5, 4, 0, 1, 5, 2, 3, 7, 2,
                7, 6, 2, 7, 3, 2, 6, 7
            };
            var num9 = 0;
            var array3 = new Vector3[array2.Length];
            var array4 = new Vector3[array2.Length];
            for (var i = 0; i < array2.Length / 3; i++)
            {
                var vector = array[array2[i * 3]];
                var vector2 = array[array2[i * 3 + 1]];
                var vector3 = array[array2[i * 3 + 2]];
                array4[i * 3 + 2] = array4[i * 3 + 1] = array4[i * 3] = Vector3.Cross(vector2 - vector, vector3 - vector).normalized;
                array3[i * 3] = vector;
                array3[i * 3 + 1] = vector2;
                array3[i * 3 + 2] = vector3;
                array2[i * 3] = num9++;
                array2[i * 3 + 1] = num9++;
                array2[i * 3 + 2] = num9++;
            }

            var mesh = new Mesh();
            mesh.vertices = array3;
            mesh.normals = array4;
            mesh.triangles = array2;
            GetComponent<MeshFilter>().mesh = mesh;
        }

        private void OnDeviceConnected(int i, bool connected)
        {
            if (i != (int)index) return;
            GetComponent<MeshFilter>().mesh = null;
            if (!connected) return;
            var system = OpenVR.System;
            if (system != null && system.GetTrackedDeviceClass((uint)i) == ETrackedDeviceClass.TrackingReference)
            {
                var pError = ETrackedPropertyError.TrackedProp_Success;
                var floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewLeftDegrees_Float, ref pError);
                if (pError == ETrackedPropertyError.TrackedProp_Success) fovLeft = floatTrackedDeviceProperty;
                floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewRightDegrees_Float, ref pError);
                if (pError == ETrackedPropertyError.TrackedProp_Success) fovRight = floatTrackedDeviceProperty;
                floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewTopDegrees_Float, ref pError);
                if (pError == ETrackedPropertyError.TrackedProp_Success) fovTop = floatTrackedDeviceProperty;
                floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_FieldOfViewBottomDegrees_Float, ref pError);
                if (pError == ETrackedPropertyError.TrackedProp_Success) fovBottom = floatTrackedDeviceProperty;
                floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_TrackingRangeMinimumMeters_Float, ref pError);
                if (pError == ETrackedPropertyError.TrackedProp_Success) nearZ = floatTrackedDeviceProperty;
                floatTrackedDeviceProperty = system.GetFloatTrackedDeviceProperty((uint)i, ETrackedDeviceProperty.Prop_TrackingRangeMaximumMeters_Float, ref pError);
                if (pError == ETrackedPropertyError.TrackedProp_Success) farZ = floatTrackedDeviceProperty;
                UpdateModel();
            }
        }

        private void OnEnable()
        {
            GetComponent<MeshFilter>().mesh = null;
            SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
        }

        private void OnDisable()
        {
            SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
            GetComponent<MeshFilter>().mesh = null;
        }
    }
}
