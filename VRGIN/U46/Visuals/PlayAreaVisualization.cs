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
				Object.DontDestroyOnLoad(this);
				base.transform.localScale = Vector3.zero;
				_Model = base.gameObject.AddComponent<SteamVR_RenderModel>();
				_Model.shader = VR.Context.Materials.StandardShader;
				base.gameObject.AddComponent<SteamVR_TrackedObject>();
				_Model.SetDeviceIndex(0);
			}

			protected override void OnUpdate()
			{
				base.OnUpdate();
				if (!NewParent && !base.enabled)
				{
					Object.DestroyImmediate(base.gameObject);
				}
				if ((bool)GetComponent<Renderer>())
				{
					if ((bool)NewParent)
					{
						base.transform.SetParent(NewParent, false);
						base.transform.localScale = Vector3.one;
						GetComponent<Renderer>().material.color = VR.Context.PrimaryColor;
						base.enabled = false;
					}
					else
					{
						VRLog.Info("We're too late!");
						Object.Destroy(base.gameObject);
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
			Indicator.SetParent(base.transform, false);
			HeightIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
			HeightIndicator.SetParent(base.transform, false);
			Transform[] array = new Transform[2] { Indicator, HeightIndicator };
			for (int i = 0; i < array.Length; i++)
			{
				Renderer component = array[i].GetComponent<Renderer>();
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
			PlayArea.transform.SetParent(base.transform, false);
			DirectionIndicator = CreateClone();
		}

		protected virtual Transform CreateClone()
		{
			HMDLoader hMDLoader = new GameObject("Model").AddComponent<HMDLoader>();
			hMDLoader.NewParent = PlayArea.transform;
			return hMDLoader.transform;
		}

		internal static PlayAreaVisualization Create(PlayArea playArea = null)
		{
			PlayAreaVisualization playAreaVisualization = new GameObject("Play Area Viszalization").AddComponent<PlayAreaVisualization>();
			if (playArea != null)
			{
				playAreaVisualization.Area = playArea;
			}
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

		protected virtual void OnDisable()
		{
		}

		protected virtual void OnDestroy()
		{
		}

		public void Enable()
		{
			base.gameObject.SetActive(true);
		}

		public void Disable()
		{
			base.gameObject.SetActive(false);
		}

		public void UpdatePosition()
		{
			SteamVR_Camera steamCam = VRCamera.Instance.SteamCam;
			float num = 2f;
			float y = steamCam.head.localPosition.y;
			float num2 = 1f;
			base.transform.position = Area.Position;
			base.transform.localScale = Vector3.one * Area.Scale;
			PlayArea.transform.localPosition = -new Vector3(steamCam.head.transform.localPosition.x, 0f, steamCam.head.transform.localPosition.z);
			base.transform.rotation = Quaternion.Euler(0f, Area.Rotation, 0f);
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
