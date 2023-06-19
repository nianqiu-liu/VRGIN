using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Valve.VR
{
    public class SteamVR_LoadLevel : MonoBehaviour
    {
        private static SteamVR_LoadLevel _active;

        public string levelName;

        public string internalProcessPath;

        public string internalProcessArgs;

        public bool loadAdditive;

        public bool loadAsync = true;

        public Texture loadingScreen;

        public Texture progressBarEmpty;

        public Texture progressBarFull;

        public float loadingScreenWidthInMeters = 6f;

        public float progressBarWidthInMeters = 3f;

        public float loadingScreenDistance;

        public Transform loadingScreenTransform;

        public Transform progressBarTransform;

        public Texture front;

        public Texture back;

        public Texture left;

        public Texture right;

        public Texture top;

        public Texture bottom;

        public Color backgroundColor = Color.black;

        public bool showGrid;

        public float fadeOutTime = 0.5f;

        public float fadeInTime = 0.5f;

        public float postLoadSettleTime;

        public float loadingScreenFadeInTime = 1f;

        public float loadingScreenFadeOutTime = 0.25f;

        private float fadeRate = 1f;

        private float alpha;

        private AsyncOperation async;

        private RenderTexture renderTexture;

        private ulong loadingScreenOverlayHandle;

        private ulong progressBarOverlayHandle;

        public bool autoTriggerOnEnable;

        public static bool loading => _active != null;

        public static float progress
        {
            get
            {
                if (!(_active != null) || _active.async == null) return 0f;
                return _active.async.progress;
            }
        }

        public static Texture progressTexture
        {
            get
            {
                if (!(_active != null)) return null;
                return _active.renderTexture;
            }
        }

        private void OnEnable()
        {
            if (autoTriggerOnEnable) Trigger();
        }

        public void Trigger()
        {
            if (!loading && !string.IsNullOrEmpty(levelName)) StartCoroutine(LoadLevel());
        }

        public static void Begin(string levelName, bool showGrid = false, float fadeOutTime = 0.5f, float r = 0f, float g = 0f, float b = 0f, float a = 1f)
        {
            var steamVR_LoadLevel = new GameObject("loader").AddComponent<SteamVR_LoadLevel>();
            steamVR_LoadLevel.levelName = levelName;
            steamVR_LoadLevel.showGrid = showGrid;
            steamVR_LoadLevel.fadeOutTime = fadeOutTime;
            steamVR_LoadLevel.backgroundColor = new Color(r, g, b, a);
            steamVR_LoadLevel.Trigger();
        }

        private void OnGUI()
        {
            if (_active != this || !(progressBarEmpty != null) || !(progressBarFull != null)) return;
            if (progressBarOverlayHandle == 0L) progressBarOverlayHandle = GetOverlayHandle("progressBar", progressBarTransform != null ? progressBarTransform : transform, progressBarWidthInMeters);
            if (progressBarOverlayHandle != 0L)
            {
                var num = async != null ? async.progress : 0f;
                var width = progressBarFull.width;
                var height = progressBarFull.height;
                if (renderTexture == null)
                {
                    renderTexture = new RenderTexture(width, height, 0);
                    renderTexture.Create();
                }

                var active = RenderTexture.active;
                RenderTexture.active = renderTexture;
                if (Event.current.type == EventType.Repaint) GL.Clear(false, true, Color.clear);
                GUILayout.BeginArea(new Rect(0f, 0f, width, height));
                GUI.DrawTexture(new Rect(0f, 0f, width, height), progressBarEmpty);
                GUI.DrawTextureWithTexCoords(new Rect(0f, 0f, num * (float)width, height), progressBarFull, new Rect(0f, 0f, num, 1f));
                GUILayout.EndArea();
                RenderTexture.active = active;
                var overlay = OpenVR.Overlay;
                if (overlay != null)
                {
                    var pTexture = default(Texture_t);
                    pTexture.handle = renderTexture.GetNativeTexturePtr();
                    pTexture.eType = SteamVR.instance.textureType;
                    pTexture.eColorSpace = EColorSpace.Auto;
                    overlay.SetOverlayTexture(progressBarOverlayHandle, ref pTexture);
                }
            }
        }

        private void Update()
        {
            if (_active != this) return;
            alpha = Mathf.Clamp01(alpha + fadeRate * Time.deltaTime);
            var overlay = OpenVR.Overlay;
            if (overlay != null)
            {
                if (loadingScreenOverlayHandle != 0L) overlay.SetOverlayAlpha(loadingScreenOverlayHandle, alpha);
                if (progressBarOverlayHandle != 0L) overlay.SetOverlayAlpha(progressBarOverlayHandle, alpha);
            }
        }

        private IEnumerator LoadLevel()
        {
            if (loadingScreen != null && loadingScreenDistance > 0f)
            {
                var transform = this.transform;
                if (Camera.main != null) transform = Camera.main.transform;
                var quaternion = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
                var position = transform.position + quaternion * new Vector3(0f, 0f, loadingScreenDistance);
                var obj = loadingScreenTransform != null ? loadingScreenTransform : this.transform;
                obj.position = position;
                obj.rotation = quaternion;
            }

            _active = this;
            SteamVR_Events.Loading.Send(true);
            if (loadingScreenFadeInTime > 0f)
                fadeRate = 1f / loadingScreenFadeInTime;
            else
                alpha = 1f;
            var overlay = OpenVR.Overlay;
            if (loadingScreen != null && overlay != null)
            {
                loadingScreenOverlayHandle = GetOverlayHandle("loadingScreen", loadingScreenTransform != null ? loadingScreenTransform : transform, loadingScreenWidthInMeters);
                if (loadingScreenOverlayHandle != 0L)
                {
                    var pTexture = default(Texture_t);
                    pTexture.handle = loadingScreen.GetNativeTexturePtr();
                    pTexture.eType = SteamVR.instance.textureType;
                    pTexture.eColorSpace = EColorSpace.Auto;
                    overlay.SetOverlayTexture(loadingScreenOverlayHandle, ref pTexture);
                }
            }

            var fadedForeground = false;
            SteamVR_Events.LoadingFadeOut.Send(fadeOutTime);
            var compositor2 = OpenVR.Compositor;
            if (compositor2 != null)
            {
                if (front != null)
                {
                    SteamVR_Skybox.SetOverride(front, back, left, right, top, bottom);
                    compositor2.FadeGrid(fadeOutTime, true);
                    yield return new WaitForSeconds(fadeOutTime);
                }
                else if (backgroundColor != Color.clear)
                {
                    if (showGrid)
                    {
                        compositor2.FadeToColor(0f, backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a, true);
                        compositor2.FadeGrid(fadeOutTime, true);
                        yield return new WaitForSeconds(fadeOutTime);
                    }
                    else
                    {
                        compositor2.FadeToColor(fadeOutTime, backgroundColor.r, backgroundColor.g, backgroundColor.b, backgroundColor.a, false);
                        yield return new WaitForSeconds(fadeOutTime + 0.1f);
                        compositor2.FadeGrid(0f, true);
                        fadedForeground = true;
                    }
                }
            }

            SteamVR_Render.pauseRendering = true;
            while (alpha < 1f) yield return null;
            this.transform.parent = null;
            DontDestroyOnLoad(gameObject);
            if (!string.IsNullOrEmpty(internalProcessPath))
            {
                UnityEngine.Debug.Log("<b>[SteamVR]</b> Launching external application...");
                var applications = OpenVR.Applications;
                if (applications == null)
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> Failed to get OpenVR.Applications interface!");
                else
                {
                    var currentDirectory = Directory.GetCurrentDirectory();
                    var text = Path.Combine(currentDirectory, internalProcessPath);
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> LaunchingInternalProcess");
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> ExternalAppPath = " + internalProcessPath);
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> FullPath = " + text);
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> ExternalAppArgs = " + internalProcessArgs);
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> WorkingDirectory = " + currentDirectory);
                    UnityEngine.Debug.Log("<b>[SteamVR]</b> LaunchInternalProcessError: " + applications.LaunchInternalProcess(text, internalProcessArgs, currentDirectory));
                    Process.GetCurrentProcess().Kill();
                }
            }
            else
            {
                var mode = loadAdditive ? LoadSceneMode.Additive : LoadSceneMode.Single;
                if (loadAsync)
                {
                    Application.backgroundLoadingPriority = ThreadPriority.Low;
                    async = SceneManager.LoadSceneAsync(levelName, mode);
                    while (!async.isDone) yield return null;
                }
                else
                    SceneManager.LoadScene(levelName, mode);
            }

            yield return null;
            GC.Collect();
            yield return null;
            Shader.WarmupAllShaders();
            yield return new WaitForSeconds(postLoadSettleTime);
            SteamVR_Render.pauseRendering = false;
            if (loadingScreenFadeOutTime > 0f)
                fadeRate = -1f / loadingScreenFadeOutTime;
            else
                alpha = 0f;
            SteamVR_Events.LoadingFadeIn.Send(fadeInTime);
            compositor2 = OpenVR.Compositor;
            if (compositor2 != null)
            {
                if (fadedForeground)
                {
                    compositor2.FadeGrid(0f, false);
                    compositor2.FadeToColor(fadeInTime, 0f, 0f, 0f, 0f, false);
                    yield return new WaitForSeconds(fadeInTime);
                }
                else
                {
                    compositor2.FadeGrid(fadeInTime, false);
                    yield return new WaitForSeconds(fadeInTime);
                    if (front != null) SteamVR_Skybox.ClearOverride();
                }
            }

            while (alpha > 0f) yield return null;
            if (overlay != null)
            {
                if (progressBarOverlayHandle != 0L) overlay.HideOverlay(progressBarOverlayHandle);
                if (loadingScreenOverlayHandle != 0L) overlay.HideOverlay(loadingScreenOverlayHandle);
            }

            Destroy(gameObject);
            _active = null;
            SteamVR_Events.Loading.Send(false);
        }

        private ulong GetOverlayHandle(string overlayName, Transform transform, float widthInMeters = 1f)
        {
            var pOverlayHandle = 0uL;
            var overlay = OpenVR.Overlay;
            if (overlay == null) return pOverlayHandle;
            var pchOverlayKey = SteamVR_Overlay.key + "." + overlayName;
            var eVROverlayError = overlay.FindOverlay(pchOverlayKey, ref pOverlayHandle);
            if (eVROverlayError != 0) eVROverlayError = overlay.CreateOverlay(pchOverlayKey, overlayName, ref pOverlayHandle);
            if (eVROverlayError == EVROverlayError.None)
            {
                overlay.ShowOverlay(pOverlayHandle);
                overlay.SetOverlayAlpha(pOverlayHandle, alpha);
                overlay.SetOverlayWidthInMeters(pOverlayHandle, widthInMeters);
                if (SteamVR.instance.textureType == ETextureType.DirectX)
                {
                    var pOverlayTextureBounds = default(VRTextureBounds_t);
                    pOverlayTextureBounds.uMin = 0f;
                    pOverlayTextureBounds.vMin = 1f;
                    pOverlayTextureBounds.uMax = 1f;
                    pOverlayTextureBounds.vMax = 0f;
                    overlay.SetOverlayTextureBounds(pOverlayHandle, ref pOverlayTextureBounds);
                }

                var steamVR_Camera = loadingScreenDistance == 0f ? SteamVR_Render.Top() : null;
                if (steamVR_Camera != null && steamVR_Camera.origin != null)
                {
                    var rigidTransform = new SteamVR_Utils.RigidTransform(steamVR_Camera.origin, transform);
                    rigidTransform.pos.x /= steamVR_Camera.origin.localScale.x;
                    rigidTransform.pos.y /= steamVR_Camera.origin.localScale.y;
                    rigidTransform.pos.z /= steamVR_Camera.origin.localScale.z;
                    var pmatTrackingOriginToOverlayTransform = rigidTransform.ToHmdMatrix34();
                    overlay.SetOverlayTransformAbsolute(pOverlayHandle, SteamVR.settings.trackingSpace, ref pmatTrackingOriginToOverlayTransform);
                }
                else
                {
                    var pmatTrackingOriginToOverlayTransform2 = new SteamVR_Utils.RigidTransform(transform).ToHmdMatrix34();
                    overlay.SetOverlayTransformAbsolute(pOverlayHandle, SteamVR.settings.trackingSpace, ref pmatTrackingOriginToOverlayTransform2);
                }
            }

            return pOverlayHandle;
        }
    }
}
