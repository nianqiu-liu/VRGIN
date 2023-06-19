using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;

namespace Valve.VR
{
    [ExecuteInEditMode]
    public class SteamVR_RenderModel : MonoBehaviour
    {
        public class RenderModel
        {
            public Mesh mesh { get; private set; }

            public Material material { get; private set; }

            public RenderModel(Mesh mesh, Material material)
            {
                this.mesh = mesh;
                this.material = material;
            }
        }

        public sealed class RenderModelInterfaceHolder : IDisposable
        {
            private bool needsShutdown;

            private bool failedLoadInterface;

            private CVRRenderModels _instance;

            public CVRRenderModels instance
            {
                get
                {
                    if (_instance == null && !failedLoadInterface)
                    {
                        if (Application.isEditor && !Application.isPlaying) needsShutdown = SteamVR.InitializeTemporarySession(false);
                        _instance = OpenVR.RenderModels;
                        if (_instance == null)
                        {
                            Debug.LogError("<b>[SteamVR]</b> Failed to load IVRRenderModels interface version IVRRenderModels_006");
                            failedLoadInterface = true;
                        }
                    }

                    return _instance;
                }
            }

            public void Dispose()
            {
                if (needsShutdown) SteamVR.ExitTemporarySession();
            }
        }

        public SteamVR_TrackedObject.EIndex index = SteamVR_TrackedObject.EIndex.None;

        protected SteamVR_Input_Sources inputSource;

        public const string modelOverrideWarning =
            "Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.";

        [Tooltip(
            "Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.")]
        public string modelOverride;

        [Tooltip("Shader to apply to model.")] public Shader shader;

        [Tooltip("Enable to print out when render models are loaded.")]
        public bool verbose;

        [Tooltip("If available, break down into separate components instead of loading as a single mesh.")]
        public bool createComponents = true;

        [Tooltip("Update transforms of components at runtime to reflect user action.")]
        public bool updateDynamically = true;

        public RenderModel_ControllerMode_State_t controllerModeState;

        public const string k_localTransformName = "attach";

        private Dictionary<string, Transform> componentAttachPoints = new Dictionary<string, Transform>();

        private List<MeshRenderer> meshRenderers = new List<MeshRenderer>();

        public static Hashtable models = new Hashtable();

        public static Hashtable materials = new Hashtable();

        private SteamVR_Events.Action deviceConnectedAction;

        private SteamVR_Events.Action hideRenderModelsAction;

        private SteamVR_Events.Action modelSkinSettingsHaveChangedAction;

        private Dictionary<int, string> nameCache;

        public string renderModelName { get; private set; }

        public bool initializedAttachPoints { get; set; }

        private void OnModelSkinSettingsHaveChanged(VREvent_t vrEvent)
        {
            if (!string.IsNullOrEmpty(renderModelName))
            {
                renderModelName = "";
                UpdateModel();
            }
        }

        public void SetMeshRendererState(bool state)
        {
            for (var i = 0; i < meshRenderers.Count; i++)
            {
                var meshRenderer = meshRenderers[i];
                if (meshRenderer != null) meshRenderer.enabled = state;
            }
        }

        private void OnHideRenderModels(bool hidden)
        {
            SetMeshRendererState(!hidden);
        }

        private void OnDeviceConnected(int i, bool connected)
        {
            if (i == (int)index && connected) UpdateModel();
        }

        public void UpdateModel()
        {
            var system = OpenVR.System;
            if (system == null || index == SteamVR_TrackedObject.EIndex.None) return;
            var pError = ETrackedPropertyError.TrackedProp_Success;
            var stringTrackedDeviceProperty = system.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, null, 0u, ref pError);
            if (stringTrackedDeviceProperty <= 1)
            {
                Debug.LogError("<b>[SteamVR]</b> Failed to get render model name for tracked object " + index);
                return;
            }

            var stringBuilder = new StringBuilder((int)stringTrackedDeviceProperty);
            system.GetStringTrackedDeviceProperty((uint)index, ETrackedDeviceProperty.Prop_RenderModelName_String, stringBuilder, stringTrackedDeviceProperty, ref pError);
            var text = stringBuilder.ToString();
            if (renderModelName != text) StartCoroutine(SetModelAsync(text));
        }

        private IEnumerator SetModelAsync(string newRenderModelName)
        {
            meshRenderers.Clear();
            if (string.IsNullOrEmpty(newRenderModelName)) yield break;
            using (var holder = new RenderModelInterfaceHolder())
            {
                var renderModels = holder.instance;
                if (renderModels == null) yield break;
                var componentCount = renderModels.GetComponentCount(newRenderModelName);
                string[] renderModelNames;
                if (componentCount == 0)
                    renderModelNames = models[newRenderModelName] is RenderModel renderModel && !(renderModel.mesh == null) ? new string[0] : new string[1] { newRenderModelName };
                else
                {
                    renderModelNames = new string[componentCount];
                    for (var i = 0; i < componentCount; i++)
                    {
                        var componentName = renderModels.GetComponentName(newRenderModelName, (uint)i, null, 0u);
                        if (componentName == 0) continue;
                        var stringBuilder = new StringBuilder((int)componentName);
                        if (renderModels.GetComponentName(newRenderModelName, (uint)i, stringBuilder, componentName) == 0) continue;
                        var pchComponentName = stringBuilder.ToString();
                        componentName = renderModels.GetComponentRenderModelName(newRenderModelName, pchComponentName, null, 0u);
                        if (componentName == 0) continue;
                        var stringBuilder2 = new StringBuilder((int)componentName);
                        if (renderModels.GetComponentRenderModelName(newRenderModelName, pchComponentName, stringBuilder2, componentName) != 0)
                        {
                            var text = stringBuilder2.ToString();
                            if (!(models[text] is RenderModel renderModel2) || renderModel2.mesh == null) renderModelNames[i] = text;
                        }
                    }
                }

                while (true)
                {
                    var flag = false;
                    for (var j = 0; j < renderModelNames.Length; j++)
                    {
                        if (string.IsNullOrEmpty(renderModelNames[j])) continue;
                        var ppRenderModel = IntPtr.Zero;
                        switch (renderModels.LoadRenderModel_Async(renderModelNames[j], ref ppRenderModel))
                        {
                            case EVRRenderModelError.Loading:
                                flag = true;
                                break;
                            case EVRRenderModelError.None:
                            {
                                var renderModel_t = MarshalRenderModel(ppRenderModel);
                                var material = materials[renderModel_t.diffuseTextureId] as Material;
                                if (material == null || material.mainTexture == null)
                                {
                                    var ppTexture = IntPtr.Zero;
                                    var eVRRenderModelError = renderModels.LoadTexture_Async(renderModel_t.diffuseTextureId, ref ppTexture);
                                    if (eVRRenderModelError == EVRRenderModelError.Loading) flag = true;
                                }

                                break;
                            }
                        }
                    }

                    if (!flag) break;
                    yield return new WaitForSecondsRealtime(0.1f);
                }
            }

            var arg = SetModel(newRenderModelName);
            renderModelName = newRenderModelName;
            SteamVR_Events.RenderModelLoaded.Send(this, arg);
        }

        private bool SetModel(string renderModelName)
        {
            StripMesh(gameObject);
            using (var renderModelInterfaceHolder = new RenderModelInterfaceHolder())
            {
                if (createComponents)
                {
                    componentAttachPoints.Clear();
                    if (LoadComponents(renderModelInterfaceHolder, renderModelName))
                    {
                        UpdateComponents(renderModelInterfaceHolder.instance);
                        return true;
                    }

                    Debug.Log("<b>[SteamVR]</b> [" + gameObject.name + "] Render model does not support components, falling back to single mesh.");
                }

                if (!string.IsNullOrEmpty(renderModelName))
                {
                    var renderModel = models[renderModelName] as RenderModel;
                    if (renderModel == null || renderModel.mesh == null)
                    {
                        var instance = renderModelInterfaceHolder.instance;
                        if (instance == null) return false;
                        renderModel = LoadRenderModel(instance, renderModelName, renderModelName);
                        if (renderModel == null) return false;
                        models[renderModelName] = renderModel;
                    }

                    gameObject.AddComponent<MeshFilter>().mesh = renderModel.mesh;
                    var meshRenderer = gameObject.AddComponent<MeshRenderer>();
                    meshRenderer.sharedMaterial = renderModel.material;
                    meshRenderers.Add(meshRenderer);
                    return true;
                }
            }

            return false;
        }

        private RenderModel LoadRenderModel(CVRRenderModels renderModels, string renderModelName, string baseName)
        {
            var ppRenderModel = IntPtr.Zero;
            while (true)
            {
                var eVRRenderModelError = renderModels.LoadRenderModel_Async(renderModelName, ref ppRenderModel);
                switch (eVRRenderModelError)
                {
                    case EVRRenderModelError.Loading:
                        break;
                    default:
                        Debug.LogError($"<b>[SteamVR]</b> Failed to load render model {renderModelName} - {eVRRenderModelError.ToString()}");
                        return null;
                    case EVRRenderModelError.None:
                    {
                        var renderModel_t = MarshalRenderModel(ppRenderModel);
                        var array = new Vector3[renderModel_t.unVertexCount];
                        var array2 = new Vector3[renderModel_t.unVertexCount];
                        var array3 = new Vector2[renderModel_t.unVertexCount];
                        var typeFromHandle = typeof(RenderModel_Vertex_t);
                        for (var i = 0; i < renderModel_t.unVertexCount; i++)
                        {
                            var renderModel_Vertex_t = (RenderModel_Vertex_t)Marshal.PtrToStructure(new IntPtr(renderModel_t.rVertexData.ToInt64() + i * Marshal.SizeOf(typeFromHandle)), typeFromHandle);
                            array[i] = new Vector3(renderModel_Vertex_t.vPosition.v0, renderModel_Vertex_t.vPosition.v1, 0f - renderModel_Vertex_t.vPosition.v2);
                            array2[i] = new Vector3(renderModel_Vertex_t.vNormal.v0, renderModel_Vertex_t.vNormal.v1, 0f - renderModel_Vertex_t.vNormal.v2);
                            array3[i] = new Vector2(renderModel_Vertex_t.rfTextureCoord0, renderModel_Vertex_t.rfTextureCoord1);
                        }

                        var num = renderModel_t.unTriangleCount * 3;
                        var array4 = new short[num];
                        Marshal.Copy(renderModel_t.rIndexData, array4, 0, array4.Length);
                        var array5 = new int[num];
                        for (var j = 0; j < renderModel_t.unTriangleCount; j++)
                        {
                            array5[j * 3] = array4[j * 3 + 2];
                            array5[j * 3 + 1] = array4[j * 3 + 1];
                            array5[j * 3 + 2] = array4[j * 3];
                        }

                        var mesh = new Mesh();
                        mesh.vertices = array;
                        mesh.normals = array2;
                        mesh.uv = array3;
                        mesh.triangles = array5;
                        var material = materials[renderModel_t.diffuseTextureId] as Material;
                        if (material == null || material.mainTexture == null)
                        {
                            var ppTexture = IntPtr.Zero;
                            while (true)
                            {
                                eVRRenderModelError = renderModels.LoadTexture_Async(renderModel_t.diffuseTextureId, ref ppTexture);
                                switch (eVRRenderModelError)
                                {
                                    case EVRRenderModelError.Loading:
                                        goto IL_0230;
                                    case EVRRenderModelError.None:
                                    {
                                        var renderModel_TextureMap_t = MarshalRenderModel_TextureMap(ppTexture);
                                        var texture2D = new Texture2D(renderModel_TextureMap_t.unWidth, renderModel_TextureMap_t.unHeight, TextureFormat.RGBA32, false);
                                        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Direct3D11)
                                        {
                                            texture2D.Apply();
                                            var nativeTexturePtr = texture2D.GetNativeTexturePtr();
                                            while (true)
                                            {
                                                eVRRenderModelError = renderModels.LoadIntoTextureD3D11_Async(renderModel_t.diffuseTextureId, nativeTexturePtr);
                                                if (eVRRenderModelError != EVRRenderModelError.Loading) break;
                                                Sleep();
                                            }
                                        }
                                        else
                                        {
                                            var array6 = new byte[renderModel_TextureMap_t.unWidth * renderModel_TextureMap_t.unHeight * 4];
                                            Marshal.Copy(renderModel_TextureMap_t.rubTextureMapData, array6, 0, array6.Length);
                                            var array7 = new Color32[renderModel_TextureMap_t.unWidth * renderModel_TextureMap_t.unHeight];
                                            var num2 = 0;
                                            for (var k = 0; k < renderModel_TextureMap_t.unHeight; k++)
                                            {
                                                for (var l = 0; l < renderModel_TextureMap_t.unWidth; l++)
                                                {
                                                    var r = array6[num2++];
                                                    var g = array6[num2++];
                                                    var b = array6[num2++];
                                                    var a = array6[num2++];
                                                    array7[k * renderModel_TextureMap_t.unWidth + l] = new Color32(r, g, b, a);
                                                }
                                            }

                                            texture2D.SetPixels32(array7);
                                            texture2D.Apply();
                                        }

                                        material = new Material(shader != null ? shader : Shader.Find("Standard"));
                                        material.mainTexture = texture2D;
                                        materials[renderModel_t.diffuseTextureId] = material;
                                        renderModels.FreeTexture(ppTexture);
                                        break;
                                    }
                                    default:
                                        Debug.Log("<b>[SteamVR]</b> Failed to load render model texture for render model " + renderModelName + ". Error: " + eVRRenderModelError);
                                        break;
                                }

                                break;
                            IL_0230:
                                Sleep();
                            }
                        }

                        StartCoroutine(FreeRenderModel(ppRenderModel));
                        return new RenderModel(mesh, material);
                    }
                }

                Sleep();
            }
        }

        private IEnumerator FreeRenderModel(IntPtr pRenderModel)
        {
            yield return new WaitForSeconds(1f);
            using var renderModelInterfaceHolder = new RenderModelInterfaceHolder();
            renderModelInterfaceHolder.instance.FreeRenderModel(pRenderModel);
        }

        public Transform FindTransformByName(string componentName, Transform inTransform = null)
        {
            if (inTransform == null) inTransform = transform;
            for (var i = 0; i < inTransform.childCount; i++)
            {
                var child = inTransform.GetChild(i);
                if (child.name == componentName) return child;
            }

            return null;
        }

        public Transform GetComponentTransform(string componentName)
        {
            if (componentName == null) return transform;
            if (componentAttachPoints.ContainsKey(componentName)) return componentAttachPoints[componentName];
            return null;
        }

        private void StripMesh(GameObject go)
        {
            var component = go.GetComponent<MeshRenderer>();
            if (component != null) DestroyImmediate(component);
            var component2 = go.GetComponent<MeshFilter>();
            if (component2 != null) DestroyImmediate(component2);
        }

        private bool LoadComponents(RenderModelInterfaceHolder holder, string renderModelName)
        {
            var transform = this.transform;
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                child.gameObject.SetActive(false);
                StripMesh(child.gameObject);
            }

            if (string.IsNullOrEmpty(renderModelName)) return true;
            var instance = holder.instance;
            if (instance == null) return false;
            var componentCount = instance.GetComponentCount(renderModelName);
            if (componentCount == 0) return false;
            for (var j = 0; j < componentCount; j++)
            {
                var componentName = instance.GetComponentName(renderModelName, (uint)j, null, 0u);
                if (componentName == 0) continue;
                var stringBuilder = new StringBuilder((int)componentName);
                if (instance.GetComponentName(renderModelName, (uint)j, stringBuilder, componentName) == 0) continue;
                var text = stringBuilder.ToString();
                transform = FindTransformByName(text);
                if (transform != null)
                {
                    transform.gameObject.SetActive(true);
                    componentAttachPoints[text] = FindTransformByName("attach", transform);
                }
                else
                {
                    transform = new GameObject(text).transform;
                    transform.parent = this.transform;
                    transform.gameObject.layer = gameObject.layer;
                    var transform2 = new GameObject("attach").transform;
                    transform2.parent = transform;
                    transform2.localPosition = Vector3.zero;
                    transform2.localRotation = Quaternion.identity;
                    transform2.localScale = Vector3.one;
                    transform2.gameObject.layer = gameObject.layer;
                    componentAttachPoints[text] = transform2;
                }

                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                transform.localScale = Vector3.one;
                componentName = instance.GetComponentRenderModelName(renderModelName, text, null, 0u);
                if (componentName == 0) continue;
                var stringBuilder2 = new StringBuilder((int)componentName);
                if (instance.GetComponentRenderModelName(renderModelName, text, stringBuilder2, componentName) == 0) continue;
                var text2 = stringBuilder2.ToString();
                var renderModel = models[text2] as RenderModel;
                if (renderModel == null || renderModel.mesh == null)
                {
                    if (verbose) Debug.Log("<b>[SteamVR]</b> Loading render model " + text2);
                    renderModel = LoadRenderModel(instance, text2, renderModelName);
                    if (renderModel == null) continue;
                    models[text2] = renderModel;
                }

                transform.gameObject.AddComponent<MeshFilter>().mesh = renderModel.mesh;
                var meshRenderer = transform.gameObject.AddComponent<MeshRenderer>();
                meshRenderer.sharedMaterial = renderModel.material;
                meshRenderers.Add(meshRenderer);
            }

            return true;
        }

        private SteamVR_RenderModel()
        {
            deviceConnectedAction = SteamVR_Events.DeviceConnectedAction(OnDeviceConnected);
            hideRenderModelsAction = SteamVR_Events.HideRenderModelsAction(OnHideRenderModels);
            modelSkinSettingsHaveChangedAction = SteamVR_Events.SystemAction(EVREventType.VREvent_ModelSkinSettingsHaveChanged, OnModelSkinSettingsHaveChanged);
        }

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(modelOverride))
            {
                Debug.Log(
                    "<b>[SteamVR]</b> Model override is really only meant to be used in the scene view for lining things up; using it at runtime is discouraged.  Use tracked device index instead to ensure the correct model is displayed for all users.");
                enabled = false;
                return;
            }

            var system = OpenVR.System;
            if (system != null && system.IsTrackedDeviceConnected((uint)index)) UpdateModel();
            deviceConnectedAction.enabled = true;
            hideRenderModelsAction.enabled = true;
            modelSkinSettingsHaveChangedAction.enabled = true;
        }

        private void OnDisable()
        {
            deviceConnectedAction.enabled = false;
            hideRenderModelsAction.enabled = false;
            modelSkinSettingsHaveChangedAction.enabled = false;
        }

        private void Update()
        {
            if (updateDynamically) UpdateComponents(OpenVR.RenderModels);
        }

        public void UpdateComponents(CVRRenderModels renderModels)
        {
            if (renderModels == null || this.transform.childCount == 0) return;
            if (nameCache == null) nameCache = new Dictionary<int, string>();
            for (var i = 0; i < this.transform.childCount; i++)
            {
                var child = this.transform.GetChild(i);
                if (!nameCache.TryGetValue(child.GetInstanceID(), out var value))
                {
                    value = child.name;
                    nameCache.Add(child.GetInstanceID(), value);
                }

                var pComponentState = default(RenderModel_ComponentState_t);
                if (!renderModels.GetComponentStateForDevicePath(renderModelName, value, SteamVR_Input_Source.GetHandle(inputSource), ref controllerModeState, ref pComponentState)) continue;
                child.localPosition = pComponentState.mTrackingToComponentRenderModel.GetPosition();
                child.localRotation = pComponentState.mTrackingToComponentRenderModel.GetRotation();
                Transform transform = null;
                for (var j = 0; j < child.childCount; j++)
                {
                    var child2 = child.GetChild(j);
                    var instanceID = child2.GetInstanceID();
                    if (!nameCache.TryGetValue(instanceID, out var value2))
                    {
                        value2 = child2.name;
                        nameCache.Add(instanceID, value);
                    }

                    if (value2 == "attach") transform = child2;
                }

                if (transform != null)
                {
                    transform.position = this.transform.TransformPoint(pComponentState.mTrackingToComponentLocal.GetPosition());
                    transform.rotation = this.transform.rotation * pComponentState.mTrackingToComponentLocal.GetRotation();
                    initializedAttachPoints = true;
                }

                var flag = (pComponentState.uProperties & 2) != 0;
                if (flag != child.gameObject.activeSelf) child.gameObject.SetActive(flag);
            }
        }

        public void SetDeviceIndex(int newIndex)
        {
            index = (SteamVR_TrackedObject.EIndex)newIndex;
            modelOverride = "";
            if (enabled) UpdateModel();
        }

        public void SetInputSource(SteamVR_Input_Sources newInputSource)
        {
            inputSource = newInputSource;
        }

        private static void Sleep()
        {
            Thread.Sleep(1);
        }

        private RenderModel_t MarshalRenderModel(IntPtr pRenderModel)
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var renderModel_t_Packed = (RenderModel_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t_Packed));
                var unpacked = default(RenderModel_t);
                renderModel_t_Packed.Unpack(ref unpacked);
                return unpacked;
            }

            return (RenderModel_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_t));
        }

        private RenderModel_TextureMap_t MarshalRenderModel_TextureMap(IntPtr pRenderModel)
        {
            if (Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix)
            {
                var renderModel_TextureMap_t_Packed = (RenderModel_TextureMap_t_Packed)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_TextureMap_t_Packed));
                var unpacked = default(RenderModel_TextureMap_t);
                renderModel_TextureMap_t_Packed.Unpack(ref unpacked);
                return unpacked;
            }

            return (RenderModel_TextureMap_t)Marshal.PtrToStructure(pRenderModel, typeof(RenderModel_TextureMap_t));
        }
    }
}
