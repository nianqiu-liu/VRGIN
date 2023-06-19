using System;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Menu : MonoBehaviour
    {
        public Texture cursor;

        public Texture background;

        public Texture logo;

        public float logoHeight;

        public float menuOffset;

        public Vector2 scaleLimits = new Vector2(0.1f, 5f);

        public float scaleRate = 0.5f;

        private SteamVR_Overlay overlay;

        private Camera overlayCam;

        private Vector4 uvOffset;

        private float distance;

        private string scaleLimitX;

        private string scaleLimitY;

        private string scaleRateText;

        private CursorLockMode savedCursorLockState;

        private bool savedCursorVisible;

        public RenderTexture texture
        {
            get
            {
                if (!overlay) return null;
                return overlay.texture as RenderTexture;
            }
        }

        public float scale { get; private set; }

        private void Awake()
        {
            scaleLimitX = $"{scaleLimits.x:N1}";
            scaleLimitY = $"{scaleLimits.y:N1}";
            scaleRateText = $"{scaleRate:N1}";
            var instance = SteamVR_Overlay.instance;
            if (instance != null)
            {
                uvOffset = instance.uvOffset;
                distance = instance.distance;
            }
        }

        private void OnGUI()
        {
            if (overlay == null) return;
            var renderTexture = overlay.texture as RenderTexture;
            var active = RenderTexture.active;
            RenderTexture.active = renderTexture;
            if (Event.current.type == EventType.Repaint) GL.Clear(false, true, Color.clear);
            var screenRect = new Rect(0f, 0f, renderTexture.width, renderTexture.height);
            if (Screen.width < renderTexture.width)
            {
                screenRect.width = Screen.width;
                overlay.uvOffset.x = (0f - (float)(renderTexture.width - Screen.width)) / (float)(2 * renderTexture.width);
            }

            if (Screen.height < renderTexture.height)
            {
                screenRect.height = Screen.height;
                overlay.uvOffset.y = (float)(renderTexture.height - Screen.height) / (float)(2 * renderTexture.height);
            }

            GUILayout.BeginArea(screenRect);
            if (background != null)
                GUI.DrawTexture(new Rect((screenRect.width - (float)background.width) / 2f, (screenRect.height - (float)background.height) / 2f, background.width, background.height), background);
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical();
            if (logo != null)
            {
                GUILayout.Space(screenRect.height / 2f - logoHeight);
                GUILayout.Box(logo);
            }

            GUILayout.Space(menuOffset);
            var num = GUILayout.Button("[Esc] - Close menu");
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Scale: {scale:N4}");
            var num2 = GUILayout.HorizontalSlider(scale, scaleLimits.x, scaleLimits.y);
            if (num2 != scale) SetScale(num2);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Scale limits:");
            var text = GUILayout.TextField(scaleLimitX);
            if (text != scaleLimitX && float.TryParse(text, out scaleLimits.x)) scaleLimitX = text;
            var text2 = GUILayout.TextField(scaleLimitY);
            if (text2 != scaleLimitY && float.TryParse(text2, out scaleLimits.y)) scaleLimitY = text2;
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Scale rate:");
            var text3 = GUILayout.TextField(scaleRateText);
            if (text3 != scaleRateText && float.TryParse(text3, out scaleRate)) scaleRateText = text3;
            GUILayout.EndHorizontal();
            if (SteamVR.active)
            {
                var instance = SteamVR.instance;
                GUILayout.BeginHorizontal();
                var sceneResolutionScale = SteamVR_Camera.sceneResolutionScale;
                var num3 = (int)(instance.sceneWidth * sceneResolutionScale);
                var num4 = (int)(instance.sceneHeight * sceneResolutionScale);
                var num5 = (int)(100f * sceneResolutionScale);
                GUILayout.Label($"Scene quality: {num3}x{num4} ({num5}%)");
                var num6 = Mathf.RoundToInt(GUILayout.HorizontalSlider(num5, 50f, 200f));
                if (num6 != num5) SteamVR_Camera.sceneResolutionScale = (float)num6 / 100f;
                GUILayout.EndHorizontal();
            }

            var steamVR_Camera = SteamVR_Render.Top();
            if (steamVR_Camera != null)
            {
                steamVR_Camera.wireframe = GUILayout.Toggle(steamVR_Camera.wireframe, "Wireframe");
                if (SteamVR.settings.trackingSpace == ETrackingUniverseOrigin.TrackingUniverseSeated)
                {
                    if (GUILayout.Button("Switch to Standing")) SteamVR.settings.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseStanding;
                    if (GUILayout.Button("Center View")) OpenVR.Chaperone?.ResetZeroPose(SteamVR.settings.trackingSpace);
                }
                else if (GUILayout.Button("Switch to Seated")) SteamVR.settings.trackingSpace = ETrackingUniverseOrigin.TrackingUniverseSeated;
            }

            if (GUILayout.Button("Exit")) Application.Quit();
            GUILayout.Space(menuOffset);
            var environmentVariable = Environment.GetEnvironmentVariable("VR_OVERRIDE");
            if (environmentVariable != null) GUILayout.Label("VR_OVERRIDE=" + environmentVariable);
            GUILayout.Label("Graphics device: " + SystemInfo.graphicsDeviceVersion);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            if (cursor != null)
            {
                var x = Input.mousePosition.x;
                var y = (float)Screen.height - Input.mousePosition.y;
                float width = cursor.width;
                float height = cursor.height;
                GUI.DrawTexture(new Rect(x, y, width, height), cursor);
            }

            RenderTexture.active = active;
            if (num) HideMenu();
        }

        public void ShowMenu()
        {
            var instance = SteamVR_Overlay.instance;
            if (instance == null) return;
            var renderTexture = instance.texture as RenderTexture;
            if (renderTexture == null)
            {
                Debug.LogError("<b>[SteamVR]</b> Menu requires overlay texture to be a render texture.", this);
                return;
            }

            SaveCursorState();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            overlay = instance;
            uvOffset = instance.uvOffset;
            distance = instance.distance;
            var array = FindObjectsOfType(typeof(Camera)) as Camera[];
            foreach (var camera in array)
            {
                if (camera.enabled && camera.targetTexture == renderTexture)
                {
                    overlayCam = camera;
                    overlayCam.enabled = false;
                    break;
                }
            }

            var steamVR_Camera = SteamVR_Render.Top();
            if (steamVR_Camera != null) scale = steamVR_Camera.origin.localScale.x;
        }

        public void HideMenu()
        {
            RestoreCursorState();
            if (overlayCam != null) overlayCam.enabled = true;
            if (overlay != null)
            {
                overlay.uvOffset = uvOffset;
                overlay.distance = distance;
                overlay = null;
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Joystick1Button7))
            {
                if (overlay == null)
                    ShowMenu();
                else
                    HideMenu();
            }
            else if (Input.GetKeyDown(KeyCode.Home))
                SetScale(1f);
            else if (Input.GetKey(KeyCode.PageUp))
                SetScale(Mathf.Clamp(scale + scaleRate * Time.deltaTime, scaleLimits.x, scaleLimits.y));
            else if (Input.GetKey(KeyCode.PageDown)) SetScale(Mathf.Clamp(scale - scaleRate * Time.deltaTime, scaleLimits.x, scaleLimits.y));
        }

        private void SetScale(float scale)
        {
            this.scale = scale;
            var steamVR_Camera = SteamVR_Render.Top();
            if (steamVR_Camera != null) steamVR_Camera.origin.localScale = new Vector3(scale, scale, scale);
        }

        private void SaveCursorState()
        {
            savedCursorVisible = Cursor.visible;
            savedCursorLockState = Cursor.lockState;
        }

        private void RestoreCursorState()
        {
            Cursor.visible = savedCursorVisible;
            Cursor.lockState = savedCursorLockState;
        }
    }
}
