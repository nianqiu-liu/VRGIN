using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using Valve.VR;

public static class SteamVR_Utils
{
    public class Event
    {
        public delegate void Handler(params object[] args);

        private static Hashtable listeners = new Hashtable();

        public static void Listen(string message, Handler action)
        {
            if (listeners[message] is Handler a)
                listeners[message] = (Handler)Delegate.Combine(a, action);
            else
                listeners[message] = action;
        }

        public static void Remove(string message, Handler action)
        {
            if (listeners[message] is Handler source) listeners[message] = (Handler)Delegate.Remove(source, action);
        }

        public static void Send(string message, params object[] args)
        {
            if (listeners[message] is Handler handler) handler(args);
        }
    }

    [Serializable]
    public struct RigidTransform
    {
        public Vector3 pos;

        public Quaternion rot;

        public static RigidTransform identity => new RigidTransform(Vector3.zero, Quaternion.identity);

        public static RigidTransform FromLocal(Transform t)
        {
            return new RigidTransform(t.localPosition, t.localRotation);
        }

        public RigidTransform(Vector3 pos, Quaternion rot)
        {
            this.pos = pos;
            this.rot = rot;
        }

        public RigidTransform(Transform t)
        {
            pos = t.position;
            rot = t.rotation;
        }

        public RigidTransform(Transform from, Transform to)
        {
            var quaternion = Quaternion.Inverse(from.rotation);
            rot = quaternion * to.rotation;
            pos = quaternion * (to.position - from.position);
        }

        public RigidTransform(HmdMatrix34_t pose)
        {
            var matrix = Matrix4x4.identity;
            matrix[0, 0] = pose.m0;
            matrix[0, 1] = pose.m1;
            matrix[0, 2] = 0f - pose.m2;
            matrix[0, 3] = pose.m3;
            matrix[1, 0] = pose.m4;
            matrix[1, 1] = pose.m5;
            matrix[1, 2] = 0f - pose.m6;
            matrix[1, 3] = pose.m7;
            matrix[2, 0] = 0f - pose.m8;
            matrix[2, 1] = 0f - pose.m9;
            matrix[2, 2] = pose.m10;
            matrix[2, 3] = 0f - pose.m11;
            pos = matrix.GetPosition();
            rot = matrix.GetRotation();
        }

        public RigidTransform(HmdMatrix44_t pose)
        {
            var matrix = Matrix4x4.identity;
            matrix[0, 0] = pose.m0;
            matrix[0, 1] = pose.m1;
            matrix[0, 2] = 0f - pose.m2;
            matrix[0, 3] = pose.m3;
            matrix[1, 0] = pose.m4;
            matrix[1, 1] = pose.m5;
            matrix[1, 2] = 0f - pose.m6;
            matrix[1, 3] = pose.m7;
            matrix[2, 0] = 0f - pose.m8;
            matrix[2, 1] = 0f - pose.m9;
            matrix[2, 2] = pose.m10;
            matrix[2, 3] = 0f - pose.m11;
            matrix[3, 0] = pose.m12;
            matrix[3, 1] = pose.m13;
            matrix[3, 2] = 0f - pose.m14;
            matrix[3, 3] = pose.m15;
            pos = matrix.GetPosition();
            rot = matrix.GetRotation();
        }

        public HmdMatrix44_t ToHmdMatrix44()
        {
            var matrix4x = Matrix4x4.TRS(pos, rot, Vector3.one);
            var result = default(HmdMatrix44_t);
            result.m0 = matrix4x[0, 0];
            result.m1 = matrix4x[0, 1];
            result.m2 = 0f - matrix4x[0, 2];
            result.m3 = matrix4x[0, 3];
            result.m4 = matrix4x[1, 0];
            result.m5 = matrix4x[1, 1];
            result.m6 = 0f - matrix4x[1, 2];
            result.m7 = matrix4x[1, 3];
            result.m8 = 0f - matrix4x[2, 0];
            result.m9 = 0f - matrix4x[2, 1];
            result.m10 = matrix4x[2, 2];
            result.m11 = 0f - matrix4x[2, 3];
            result.m12 = matrix4x[3, 0];
            result.m13 = matrix4x[3, 1];
            result.m14 = 0f - matrix4x[3, 2];
            result.m15 = matrix4x[3, 3];
            return result;
        }

        public HmdMatrix34_t ToHmdMatrix34()
        {
            var matrix4x = Matrix4x4.TRS(pos, rot, Vector3.one);
            var result = default(HmdMatrix34_t);
            result.m0 = matrix4x[0, 0];
            result.m1 = matrix4x[0, 1];
            result.m2 = 0f - matrix4x[0, 2];
            result.m3 = matrix4x[0, 3];
            result.m4 = matrix4x[1, 0];
            result.m5 = matrix4x[1, 1];
            result.m6 = 0f - matrix4x[1, 2];
            result.m7 = matrix4x[1, 3];
            result.m8 = 0f - matrix4x[2, 0];
            result.m9 = 0f - matrix4x[2, 1];
            result.m10 = matrix4x[2, 2];
            result.m11 = 0f - matrix4x[2, 3];
            return result;
        }

        public override bool Equals(object o)
        {
            if (o is RigidTransform rigidTransform)
            {
                if (pos == rigidTransform.pos) return rot == rigidTransform.rot;
                return false;
            }

            return false;
        }

        public override int GetHashCode()
        {
            return pos.GetHashCode() ^ rot.GetHashCode();
        }

        public static bool operator ==(RigidTransform a, RigidTransform b)
        {
            if (a.pos == b.pos) return a.rot == b.rot;
            return false;
        }

        public static bool operator !=(RigidTransform a, RigidTransform b)
        {
            if (!(a.pos != b.pos)) return a.rot != b.rot;
            return true;
        }

        public static RigidTransform operator *(RigidTransform a, RigidTransform b)
        {
            var result = default(RigidTransform);
            result.rot = a.rot * b.rot;
            result.pos = a.pos + a.rot * b.pos;
            return result;
        }

        public void Inverse()
        {
            rot = Quaternion.Inverse(rot);
            pos = -(rot * pos);
        }

        public RigidTransform GetInverse()
        {
            var result = new RigidTransform(pos, rot);
            result.Inverse();
            return result;
        }

        public void Multiply(RigidTransform a, RigidTransform b)
        {
            rot = a.rot * b.rot;
            pos = a.pos + a.rot * b.pos;
        }

        public Vector3 InverseTransformPoint(Vector3 point)
        {
            return Quaternion.Inverse(rot) * (point - pos);
        }

        public Vector3 TransformPoint(Vector3 point)
        {
            return pos + rot * point;
        }

        public static Vector3 operator *(RigidTransform t, Vector3 v)
        {
            return t.TransformPoint(v);
        }

        public static RigidTransform Interpolate(RigidTransform a, RigidTransform b, float t)
        {
            return new RigidTransform(Vector3.Lerp(a.pos, b.pos, t), Quaternion.Slerp(a.rot, b.rot, t));
        }

        public void Interpolate(RigidTransform to, float t)
        {
            pos = Lerp(pos, to.pos, t);
            rot = Slerp(rot, to.rot, t);
        }
    }

    public delegate object SystemFn(CVRSystem system, params object[] args);

    private const string secretKey = "foobar";

    public static bool IsValid(Vector3 vector)
    {
        if (!float.IsNaN(vector.x) && !float.IsNaN(vector.y)) return !float.IsNaN(vector.z);
        return false;
    }

    public static bool IsValid(Quaternion rotation)
    {
        if (!float.IsNaN(rotation.x) && !float.IsNaN(rotation.y) && !float.IsNaN(rotation.z) && !float.IsNaN(rotation.w))
        {
            if (rotation.x == 0f && rotation.y == 0f && rotation.z == 0f) return rotation.w != 0f;
            return true;
        }

        return false;
    }

    public static Quaternion Slerp(Quaternion A, Quaternion B, float t)
    {
        var num = Mathf.Clamp(A.x * B.x + A.y * B.y + A.z * B.z + A.w * B.w, -1f, 1f);
        if (num < 0f)
        {
            B = new Quaternion(0f - B.x, 0f - B.y, 0f - B.z, 0f - B.w);
            num = 0f - num;
        }

        float num4;
        float num5;
        if (1f - num > 0.0001f)
        {
            var num2 = Mathf.Acos(num);
            var num3 = Mathf.Sin(num2);
            num4 = Mathf.Sin((1f - t) * num2) / num3;
            num5 = Mathf.Sin(t * num2) / num3;
        }
        else
        {
            num4 = 1f - t;
            num5 = t;
        }

        return new Quaternion(num4 * A.x + num5 * B.x, num4 * A.y + num5 * B.y, num4 * A.z + num5 * B.z, num4 * A.w + num5 * B.w);
    }

    public static Vector3 Lerp(Vector3 A, Vector3 B, float t)
    {
        return new Vector3(Lerp(A.x, B.x, t), Lerp(A.y, B.y, t), Lerp(A.z, B.z, t));
    }

    public static float Lerp(float A, float B, float t)
    {
        return A + (B - A) * t;
    }

    public static double Lerp(double A, double B, double t)
    {
        return A + (B - A) * t;
    }

    public static float InverseLerp(Vector3 A, Vector3 B, Vector3 result)
    {
        return Vector3.Dot(result - A, B - A);
    }

    public static float InverseLerp(float A, float B, float result)
    {
        return (result - A) / (B - A);
    }

    public static double InverseLerp(double A, double B, double result)
    {
        return (result - A) / (B - A);
    }

    public static float Saturate(float A)
    {
        if (!(A < 0f))
        {
            if (!(A > 1f)) return A;
            return 1f;
        }

        return 0f;
    }

    public static Vector2 Saturate(Vector2 A)
    {
        return new Vector2(Saturate(A.x), Saturate(A.y));
    }

    public static float Abs(float A)
    {
        if (!(A < 0f)) return A;
        return 0f - A;
    }

    public static Vector2 Abs(Vector2 A)
    {
        return new Vector2(Abs(A.x), Abs(A.y));
    }

    private static float _copysign(float sizeval, float signval)
    {
        if (Mathf.Sign(signval) != 1f) return 0f - Mathf.Abs(sizeval);
        return Mathf.Abs(sizeval);
    }

    public static Quaternion GetRotation(this Matrix4x4 matrix)
    {
        var result = default(Quaternion);
        result.w = Mathf.Sqrt(Mathf.Max(0f, 1f + matrix.m00 + matrix.m11 + matrix.m22)) / 2f;
        result.x = Mathf.Sqrt(Mathf.Max(0f, 1f + matrix.m00 - matrix.m11 - matrix.m22)) / 2f;
        result.y = Mathf.Sqrt(Mathf.Max(0f, 1f - matrix.m00 + matrix.m11 - matrix.m22)) / 2f;
        result.z = Mathf.Sqrt(Mathf.Max(0f, 1f - matrix.m00 - matrix.m11 + matrix.m22)) / 2f;
        result.x = _copysign(result.x, matrix.m21 - matrix.m12);
        result.y = _copysign(result.y, matrix.m02 - matrix.m20);
        result.z = _copysign(result.z, matrix.m10 - matrix.m01);
        return result;
    }

    public static Vector3 GetPosition(this Matrix4x4 matrix)
    {
        var m = matrix.m03;
        var m2 = matrix.m13;
        var m3 = matrix.m23;
        return new Vector3(m, m2, m3);
    }

    public static Vector3 GetScale(this Matrix4x4 m)
    {
        var x = Mathf.Sqrt(m.m00 * m.m00 + m.m01 * m.m01 + m.m02 * m.m02);
        var y = Mathf.Sqrt(m.m10 * m.m10 + m.m11 * m.m11 + m.m12 * m.m12);
        var z = Mathf.Sqrt(m.m20 * m.m20 + m.m21 * m.m21 + m.m22 * m.m22);
        return new Vector3(x, y, z);
    }

    public static float GetLossyScale(Transform t)
    {
        return t.lossyScale.x;
    }

    public static string GetBadMD5Hash(string usedString)
    {
        return GetBadMD5Hash(Encoding.UTF8.GetBytes(usedString + "foobar"));
    }

    public static string GetBadMD5Hash(byte[] bytes)
    {
        var array = new MD5CryptoServiceProvider().ComputeHash(bytes);
        var stringBuilder = new StringBuilder();
        for (var i = 0; i < array.Length; i++) stringBuilder.Append(array[i].ToString("x2"));
        return stringBuilder.ToString();
    }

    public static string GetBadMD5HashFromFile(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        return GetBadMD5Hash(File.ReadAllText(filePath) + "foobar");
    }

    public static string SanitizePath(string path, bool allowLeadingSlash = true)
    {
        if (path.Contains("\\\\")) path = path.Replace("\\\\", "\\");
        if (path.Contains("//")) path = path.Replace("//", "/");
        if (!allowLeadingSlash && (path[0] == '/' || path[0] == '\\')) path = path.Substring(1);
        return path;
    }

    public static Type FindType(string typeName)
    {
        var type = Type.GetType(typeName);
        if (type != null) return type;
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        for (var i = 0; i < assemblies.Length; i++)
        {
            type = assemblies[i].GetType(typeName);
            if (type != null) return type;
        }

        return null;
    }

    public static object CallSystemFn(SystemFn fn, params object[] args)
    {
        var flag = !SteamVR.active && !SteamVR.usingNativeSupport;
        if (flag)
        {
            var peError = EVRInitError.None;
            OpenVR.Init(ref peError, EVRApplicationType.VRApplication_Utility);
        }

        var system = OpenVR.System;
        var result = system != null ? fn(system, args) : null;
        if (flag) OpenVR.Shutdown();
        return result;
    }

    public static void TakeStereoScreenshot(uint screenshotHandle, GameObject target, int cellSize, float ipd, ref string previewFilename, ref string VRFilename)
    {
        var texture2D = new Texture2D(4096, 4096, TextureFormat.ARGB32, false);
        var stopwatch = new Stopwatch();
        Camera camera = null;
        stopwatch.Start();
        var camera2 = target.GetComponent<Camera>();
        if (camera2 == null)
        {
            if (camera == null) camera = new GameObject().AddComponent<Camera>();
            camera2 = camera;
        }

        var texture2D2 = new Texture2D(2048, 2048, TextureFormat.ARGB32, false);
        var renderTexture = new RenderTexture(2048, 2048, 24);
        var targetTexture = camera2.targetTexture;
        var orthographic = camera2.orthographic;
        var fieldOfView = camera2.fieldOfView;
        var aspect = camera2.aspect;
        var stereoTargetEye = camera2.stereoTargetEye;
        camera2.stereoTargetEye = StereoTargetEyeMask.None;
        camera2.fieldOfView = 60f;
        camera2.orthographic = false;
        camera2.targetTexture = renderTexture;
        camera2.aspect = 1f;
        camera2.Render();
        RenderTexture.active = renderTexture;
        texture2D2.ReadPixels(new Rect(0f, 0f, renderTexture.width, renderTexture.height), 0, 0);
        RenderTexture.active = null;
        camera2.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(renderTexture);
        var steamVR_SphericalProjection = camera2.gameObject.AddComponent<SteamVR_SphericalProjection>();
        var localPosition = target.transform.localPosition;
        var localRotation = target.transform.localRotation;
        var position = target.transform.position;
        var quaternion = Quaternion.Euler(0f, target.transform.rotation.eulerAngles.y, 0f);
        var transform = camera2.transform;
        var num = 1024 / cellSize;
        var num2 = 90f / (float)num;
        var num3 = num2 / 2f;
        var renderTexture2 = new RenderTexture(cellSize, cellSize, 24);
        renderTexture2.wrapMode = TextureWrapMode.Clamp;
        renderTexture2.antiAliasing = 8;
        camera2.fieldOfView = num2;
        camera2.orthographic = false;
        camera2.targetTexture = renderTexture2;
        camera2.aspect = aspect;
        camera2.stereoTargetEye = StereoTargetEyeMask.None;
        for (var i = 0; i < num; i++)
        {
            var num4 = 90f - (float)i * num2 - num3;
            var num5 = 4096 / renderTexture2.width;
            var num6 = 360f / (float)num5;
            var num7 = num6 / 2f;
            var num8 = i * 1024 / num;
            for (var j = 0; j < 2; j++)
            {
                if (j == 1)
                {
                    num4 = 0f - num4;
                    num8 = 2048 - num8 - cellSize;
                }

                for (var k = 0; k < num5; k++)
                {
                    var num9 = -180f + (float)k * num6 + num7;
                    var destX = k * 4096 / num5;
                    var num10 = 0;
                    var num11 = (0f - ipd) / 2f * Mathf.Cos(num4 * ((float)Math.PI / 180f));
                    for (var l = 0; l < 2; l++)
                    {
                        if (l == 1)
                        {
                            num10 = 2048;
                            num11 = 0f - num11;
                        }

                        var vector = quaternion * Quaternion.Euler(0f, num9, 0f) * new Vector3(num11, 0f, 0f);
                        transform.position = position + vector;
                        var quaternion2 = Quaternion.Euler(num4, num9, 0f);
                        transform.rotation = quaternion * quaternion2;
                        var vector2 = quaternion2 * Vector3.forward;
                        var num12 = num9 - num6 / 2f;
                        var num13 = num12 + num6;
                        var num14 = num4 + num2 / 2f;
                        var num15 = num14 - num2;
                        var y = (num12 + num13) / 2f;
                        var x = Mathf.Abs(num14) < Mathf.Abs(num15) ? num14 : num15;
                        var vector3 = Quaternion.Euler(x, num12, 0f) * Vector3.forward;
                        var vector4 = Quaternion.Euler(x, num13, 0f) * Vector3.forward;
                        var vector5 = Quaternion.Euler(num14, y, 0f) * Vector3.forward;
                        var vector6 = Quaternion.Euler(num15, y, 0f) * Vector3.forward;
                        var vector7 = vector3 / Vector3.Dot(vector3, vector2);
                        var vector8 = vector4 / Vector3.Dot(vector4, vector2);
                        var vector9 = vector5 / Vector3.Dot(vector5, vector2);
                        var vector10 = vector6 / Vector3.Dot(vector6, vector2);
                        var vector11 = vector8 - vector7;
                        var vector12 = vector10 - vector9;
                        var magnitude = vector11.magnitude;
                        var magnitude2 = vector12.magnitude;
                        var num16 = 1f / magnitude;
                        var num17 = 1f / magnitude2;
                        var uAxis = vector11 * num16;
                        var vAxis = vector12 * num17;
                        steamVR_SphericalProjection.Set(vector2, num12, num13, num14, num15, uAxis, vector7, num16, vAxis, vector9, num17);
                        camera2.aspect = magnitude / magnitude2;
                        camera2.Render();
                        RenderTexture.active = renderTexture2;
                        texture2D.ReadPixels(new Rect(0f, 0f, renderTexture2.width, renderTexture2.height), destX, num8 + num10);
                        RenderTexture.active = null;
                    }

                    var flProgress = ((float)i * ((float)num5 * 2f) + (float)k + (float)(j * num5)) / ((float)num * ((float)num5 * 2f));
                    OpenVR.Screenshots.UpdateScreenshotProgress(screenshotHandle, flProgress);
                }
            }
        }

        OpenVR.Screenshots.UpdateScreenshotProgress(screenshotHandle, 1f);
        previewFilename += ".png";
        VRFilename += ".png";
        texture2D2.Apply();
        File.WriteAllBytes(previewFilename, texture2D2.EncodeToPNG());
        texture2D.Apply();
        File.WriteAllBytes(VRFilename, texture2D.EncodeToPNG());
        if (camera2 != camera)
        {
            camera2.targetTexture = targetTexture;
            camera2.orthographic = orthographic;
            camera2.fieldOfView = fieldOfView;
            camera2.aspect = aspect;
            camera2.stereoTargetEye = stereoTargetEye;
            target.transform.localPosition = localPosition;
            target.transform.localRotation = localRotation;
        }
        else
            camera.targetTexture = null;

        UnityEngine.Object.DestroyImmediate(renderTexture2);
        UnityEngine.Object.DestroyImmediate(steamVR_SphericalProjection);
        stopwatch.Stop();
        UnityEngine.Debug.Log($"Screenshot took {stopwatch.Elapsed} seconds.");
        if (camera != null) UnityEngine.Object.DestroyImmediate(camera.gameObject);
        UnityEngine.Object.DestroyImmediate(texture2D2);
        UnityEngine.Object.DestroyImmediate(texture2D);
    }
}
