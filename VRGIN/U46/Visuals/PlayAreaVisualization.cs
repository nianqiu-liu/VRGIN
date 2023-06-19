using UnityEngine;
using UnityEngine.Rendering;
using Valve.VR;
using VRGIN.Core;

namespace VRGIN.U46.Visuals
{
    public class PlayAreaVisualization : ProtectedBehaviour
    {
        private class HMDLoader : ProtectedBehaviour
        {
            public Transform NewParent;

            private SteamVR_RenderModel _Model;

            protected override void OnStart()
            {
                DontDestroyOnLoad(this);
                transform.localScale = Vector3.zero;
                _Model = gameObject.AddComponent<SteamVR_RenderModel>();
                _Model.shader = VR.Context.Materials.StandardShader;
                gameObject.AddComponent<SteamVR_TrackedObject>();
                _Model.SetDeviceIndex(0);
            }

            protected override void OnUpdate()
            {
                base.OnUpdate();
                if (!NewParent && !enabled) DestroyImmediate(gameObject);
                if ((bool)GetComponent<Renderer>())
                {
                    if ((bool)NewParent)
                    {
                        transform.SetParent(NewParent, false);
                        transform.localScale = Vector3.one;
                        GetComponent<Renderer>().material.color = VR.Context.PrimaryColor;
                        enabled = false;
                    }
                    else
                    {
                        VRLog.Info("We're too late!");
                        Destroy(gameObject);
                    }
                }
            }
        }

        public PlayArea Area = new PlayArea();

        private SteamVR_PlayArea PlayArea;

        private Transform Indicator;

        private Transform DirectionIndicator;

        private Transform HeightIndicator;

        protected override void OnAwake()
        {
            base.OnAwake();
            CreateArea();
            Indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
            Indicator.SetParent(transform, false);
            HeightIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            HeightIndicator.SetParent(transform, false);
            var array = new Transform[2] { Indicator, HeightIndicator };
            for (var i = 0; i < array.Length; i++)
            {
                var component = array[i].GetComponent<Renderer>();
                component.material = new Material(VR.Context.Materials.Sprite);
                component.reflectionProbeUsage = ReflectionProbeUsage.Off;
                component.shadowCastingMode = ShadowCastingMode.Off;
                component.receiveShadows = false;
                component.useLightProbes = false;
                component.material.color = VR.Context.PrimaryColor;
            }
        }

        protected virtual void CreateArea()
        {
            PlayArea = new GameObject("PlayArea").AddComponent<SteamVR_PlayArea>();
            PlayArea.drawInGame = true;
            PlayArea.size = SteamVR_PlayArea.Size.Calibrated;
            PlayArea.transform.SetParent(transform, false);
            DirectionIndicator = CreateClone();
        }

        protected virtual Transform CreateClone()
        {
            var hMDLoader = new GameObject("Model").AddComponent<HMDLoader>();
            hMDLoader.NewParent = PlayArea.transform;
            return hMDLoader.transform;
        }

        internal static PlayAreaVisualization Create(PlayArea playArea = null)
        {
            var playAreaVisualization = new GameObject("Play Area Viszalization").AddComponent<PlayAreaVisualization>();
            if (playArea != null) playAreaVisualization.Area = playArea;
            return playAreaVisualization;
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected virtual void OnEnable()
        {
            PlayArea.BuildMesh();
        }

        protected virtual void OnDisable() { }

        protected virtual void OnDestroy() { }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        public void UpdatePosition()
        {
            var steamCam = VRCamera.Instance.SteamCam;
            var num = 2f;
            var y = steamCam.head.localPosition.y;
            var num2 = 1f;
            transform.position = Area.Position;
            transform.localScale = Vector3.one * Area.Scale;
            PlayArea.transform.localPosition = -new Vector3(steamCam.head.transform.localPosition.x, 0f, steamCam.head.transform.localPosition.z);
            transform.rotation = Quaternion.Euler(0f, Area.Rotation, 0f);
            Indicator.localScale = Vector3.one * 0.1f + Vector3.one * Mathf.Sin(Time.time * 5f) * 0.05f;
            HeightIndicator.localScale = new Vector3(0.01f, y / num, 0.01f);
            HeightIndicator.localPosition = new Vector3(0f, y - num2 * (y / num), 0f);
        }

        protected override void OnLateUpdate()
        {
            UpdatePosition();
        }
    }
}
