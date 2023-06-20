using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRGIN.Helpers;
using VRGIN.Native;
using VRGIN.Visuals;

namespace VRGIN.Core
{
    public class VRGUI : ProtectedBehaviour, IScreenGrabber
    {
        private class FastGUI : MonoBehaviour
        {
            private void OnGUI()
            {
                GUI.depth = int.MaxValue;
                if (Event.current.type == EventType.Repaint) SendMessage("OnBeforeGUI");
            }
        }

        private class SlowGUI : MonoBehaviour
        {
            private void OnGUI()
            {
                GUI.depth = int.MinValue;
                if (Event.current.type == EventType.Repaint) SendMessage("OnAfterGUI");
            }
        }

        private class SortingAwareGraphicRaycaster : GraphicRaycaster
        {
            private Canvas _Canvas;

            private Canvas Canvas
            {
                get
                {
                    if (_Canvas != null) return _Canvas;
                    _Canvas = GetComponent<Canvas>();
                    return _Canvas;
                }
            }

            public override int priority => GetOrder();

            public override int sortOrderPriority => GetOrder();

            public override int renderOrderPriority => GetOrder();

            private int GetOrder()
            {
                return -Canvas.sortingOrder;
            }
        }

        private static VRGUI _Instance;

        private IDictionary _Registry;

        private List<IScreenGrabber> _ScreenGrabbers = new List<IScreenGrabber>();

        private FieldInfo _Graphics;

        private RenderTexture _PrevRT;

        private Camera _VRGUICamera;

        private int _Listeners;

        private IDictionary<Camera, IScreenGrabber> _CameraMappings = new Dictionary<Camera, IScreenGrabber>();

        private HashSet<Camera> _CheckedCameras = new HashSet<Camera>();

        public static int Width { get; private set; }

        public static int Height { get; private set; }

        public SimulatedCursor SoftCursor { get; private set; }

        public static VRGUI Instance
        {
            get
            {
                if (!_Instance)
                {
                    _Instance = new GameObject("VRGIN_GUI").AddComponent<VRGUI>();
                    DontDestroyOnLoad(_Instance);
                    if (VR.Context.SimulateCursor)
                    {
                        _Instance.SoftCursor = SimulatedCursor.Create();
                        _Instance.SoftCursor.transform.SetParent(_Instance.transform, false);
                        VRLog.Info("Cursor is simulated");
                    }
                }

                return _Instance;
            }
        }

        public RenderTexture uGuiTexture { get; private set; }

        public RenderTexture IMGuiTexture { get; private set; }

        public IEnumerable<IScreenGrabber> ScreenGrabbers => _ScreenGrabbers;

        public bool IsInterested(Camera camera)
        {
            return FindCameraMapping(camera) != null;
        }

        internal bool Owns(Camera cam)
        {
            return _CameraMappings.ContainsKey(cam);
        }

        public void Listen()
        {
            _Listeners++;
        }

        public void Unlisten()
        {
            _Listeners--;
        }

        protected override void OnAwake()
        {
            var clientRect = WindowManager.GetClientRect();
            Width = clientRect.Right - clientRect.Left;
            Height = clientRect.Bottom - clientRect.Top;
            uGuiTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.Default);
            uGuiTexture.Create();
            IMGuiTexture = new RenderTexture(Screen.width, Screen.height, 0, RenderTextureFormat.Default);
            IMGuiTexture.Create();
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (!VR.Context.ForceIMGUIOnScreen)
            {
                transform.gameObject.AddComponent<FastGUI>();
                transform.gameObject.AddComponent<SlowGUI>();
            }

            var num = (float)Screen.height * 0.5f;
            var x = (float)Screen.width * 0.5f;
            _VRGUICamera = new GameObject("VRGIN_GUICamera").AddComponent<Camera>();
            _VRGUICamera.transform.SetParent(transform, false);
            if (VR.Context.PreferredGUI == GUIType.IMGUI)
            {
                _VRGUICamera.transform.position = new Vector3(x, num, -1f);
                _VRGUICamera.transform.rotation = Quaternion.identity;
                _VRGUICamera.orthographicSize = num;
            }

            _VRGUICamera.cullingMask = VR.Context.UILayerMask;
            _VRGUICamera.depth = 1f;
            _VRGUICamera.nearClipPlane = VR.Context.GuiNearClipPlane;
            _VRGUICamera.farClipPlane = VR.Context.GuiFarClipPlane;
            _VRGUICamera.targetTexture = uGuiTexture;
            _VRGUICamera.backgroundColor = Color.clear;
            _VRGUICamera.clearFlags = CameraClearFlags.Color;
            _VRGUICamera.orthographic = true;
            _VRGUICamera.useOcclusionCulling = false;
            _Graphics = typeof(GraphicRegistry).GetField("m_Graphics", BindingFlags.Instance | BindingFlags.NonPublic);
            _Registry = _Graphics.GetValue(GraphicRegistry.instance) as IDictionary;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += SceneLoaded;
        }

        private bool IsUnprocessed(Canvas c)
        {
            if (c.renderMode != 0)
            {
                if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera != _VRGUICamera) return c.worldCamera.targetTexture == null;
                return false;
            }

            return true;
        }

        protected void CatchCanvas()
        {
            _VRGUICamera.targetTexture = uGuiTexture;
            foreach (var item in (_Registry.Keys as ICollection<Canvas>).Where((Canvas c) => c).ToList().Where(IsUnprocessed))
            {
                if (VR.Interpreter.IsIgnoredCanvas(item)) continue;
                VRLog.Info("Add {0} [Layer: {1}, SortingLayer: {2}, SortingOrder: {3}, RenderMode: {4}, Camera: {5}, Position: {6} ]", item.name, LayerMask.LayerToName(item.gameObject.layer),
                           item.sortingLayerName, item.sortingOrder, item.renderMode, item.worldCamera, item.transform.position);
                item.renderMode = RenderMode.ScreenSpaceCamera;
                item.worldCamera = _VRGUICamera;
                if (((1 << item.gameObject.layer) & VR.Context.UILayerMask) == 0)
                {
                    var layer = LayerMask.NameToLayer(VR.Context.UILayer);
                    item.gameObject.layer = layer;
                    var componentsInChildren = item.gameObject.GetComponentsInChildren<Transform>();
                    for (var i = 0; i < componentsInChildren.Length; i++) componentsInChildren[i].gameObject.layer = layer;
                }

                if (VR.Context.EnforceDefaultGUIMaterials)
                {
                    var componentsInChildren2 = item.gameObject.GetComponentsInChildren<Graphic>();
                    foreach (var obj in componentsInChildren2) obj.material = obj.defaultMaterial;
                }

                if (VR.Context.GUIAlternativeSortingMode)
                {
                    var component = item.GetComponent<GraphicRaycaster>();
                    if ((bool)component)
                    {
                        DestroyImmediate(component);
                        var obj2 = item.gameObject.AddComponent<SortingAwareGraphicRaycaster>();
                        UnityHelper.SetPropertyOrField(obj2, "ignoreReversedGraphics", UnityHelper.GetPropertyOrField(component, "ignoreReversedGraphics"));
                        UnityHelper.SetPropertyOrField(obj2, "blockingObjects", UnityHelper.GetPropertyOrField(component, "blockingObjects"));
                        UnityHelper.SetPropertyOrField(obj2, "m_BlockingMask", UnityHelper.GetPropertyOrField(component, "m_BlockingMask"));
                    }
                }
            }
        }

        protected override void OnUpdate()
        {
            if (VR.Context.ConfineMouse) Cursor.lockState = CursorLockMode.Confined;
            EnsureCameraTargets();
            if (_Listeners > 0) CatchCanvas();
            if (_Listeners < 0) VRLog.Warn("Numbers don't add up!");
        }

        
        private void SceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                _CheckedCameras.Clear();
                _CameraMappings.Clear();
            }
        }

        internal void OnAfterGUI()
        {
            if (Event.current.type == EventType.Repaint) RenderTexture.active = _PrevRT;
        }

        internal void OnBeforeGUI()
        {
            if (Event.current.type == EventType.Repaint)
            {
                _PrevRT = RenderTexture.active;
                RenderTexture.active = IMGuiTexture;
                GL.Clear(true, true, Color.clear);
            }
        }

        public void AddGrabber(IScreenGrabber grabber)
        {
            if (!_ScreenGrabbers.Contains(grabber))
            {
                _ScreenGrabbers.Insert(0, grabber);
                RejudgeAll();
            }
        }

        public void RemoveGrabber(IScreenGrabber grabber)
        {
            _ScreenGrabbers.Remove(grabber);
            RejudgeAll();
        }

        private void RejudgeAll()
        {
            foreach (var item in _CheckedCameras.ToList()) AddCamera(item);
        }

        public void AddCamera(Camera camera)
        {
            VRLog.Info("Trying to find a GUI mapping for camera {0}", camera.name);
            var screenGrabber = FindCameraMapping(camera);
            if (screenGrabber != null)
            {
                _CameraMappings[camera] = screenGrabber;
                screenGrabber.OnAssign(camera);
                VRLog.Info("Assigned camera {0} to {1}", camera.name, screenGrabber);
            }

            _CheckedCameras.Add(camera);
        }

        private void EnsureCameraTargets()
        {
            var list = new List<Camera>();
            foreach (var cameraMapping in _CameraMappings)
            {
                if (!cameraMapping.Key)
                    list.Add(cameraMapping.Key);
                else if (cameraMapping.Key.targetTexture != cameraMapping.Value.GetTextures().FirstOrDefault()) cameraMapping.Key.targetTexture = cameraMapping.Value.GetTextures().FirstOrDefault();
            }

            foreach (var item in list) _CameraMappings.Remove(item);
        }

        private IScreenGrabber FindCameraMapping(Camera camera)
        {
            foreach (var screenGrabber in _ScreenGrabbers)
            {
                if (screenGrabber.Check(camera)) return screenGrabber;
            }

            if (Check(camera)) return this;
            return null;
        }

        public bool Check(Camera camera)
        {
            return VR.Interpreter.IsUICamera(camera);
        }

        public IEnumerable<RenderTexture> GetTextures()
        {
            yield return uGuiTexture;
            yield return IMGuiTexture;
        }

        public void OnAssign(Camera camera)
        {
            _VRGUICamera.depth = Mathf.Min(_VRGUICamera.depth, camera.depth - 1f);
        }
    }
}
