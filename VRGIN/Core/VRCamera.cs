using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SpatialTracking;
using Valve.VR;
using VRGIN.Helpers;

namespace VRGIN.Core
{
	public class VRCamera : ProtectedBehaviour
	{
		private delegate void CameraOperation(Camera camera);

		private static VRCamera _Instance;

		private IList<CameraSlave> Slaves = new List<CameraSlave>();

		private const float MIN_FAR_CLIP_PLANE = 10f;

		private Camera _Camera;

		private Transform _origin;

		private Transform _head;

		public SteamVR_Camera SteamCam { get; private set; }

		public TrackedPoseDriver TrackedPose { get; private set; }

		public Camera Blueprint
		{
			get
			{
				if (!_Blueprint || !_Blueprint.isActiveAndEnabled)
				{
					return Slaves.Select((CameraSlave s) => s.Camera).FirstOrDefault((Camera c) => !VR.GUI.Owns(c));
				}
				return _Blueprint;
			}
		}

		private Camera _Blueprint { get; set; }

		public bool HasValidBlueprint => Slaves.Count > 0;

		public Transform Origin => SteamCam.origin;

		public Transform Head => SteamCam.head;

		public static VRCamera Instance
		{
			get
			{
				if (_Instance == null)
				{
					_Instance = new GameObject("VRGIN_Camera").AddComponent<AudioListener>().gameObject.AddComponent<VRCamera>();
					UnityEngine.Object.DontDestroyOnLoad(_Instance.gameObject);
				}
				return _Instance;
			}
		}

		public event EventHandler<InitializeCameraEventArgs> InitializeCamera = delegate
		{
		};

		public event EventHandler<CopiedCameraEventArgs> CopiedCamera = delegate
		{
		};

		protected override void OnAwake()
		{
			try
			{
				_Camera = base.gameObject.AddComponent<Camera>();
				_Camera.tag = "MainCamera";
				base.gameObject.AddComponent<SteamVR_Camera>();
				TrackedPose = base.gameObject.AddComponent<TrackedPoseDriver>();
				TrackedPose.SetPoseSource(TrackedPoseDriver.DeviceType.GenericXRDevice, TrackedPoseDriver.TrackedPose.Center);
				TrackedPose.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
				TrackedPose.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
				SteamCam = GetComponent<SteamVR_Camera>();
				_ = VR.Settings.MirrorScreen;
				SteamVR_Camera.sceneResolutionScale = VR.Settings.RenderScale;
				new GameObject("CenterEyeAnchor").transform.SetParent(SteamCam.head);
				DontDetroyRootObject(SteamCam.origin.gameObject);
			}
			catch (Exception obj)
			{
				VRLog.Error(obj);
			}
		}

		private void DontDetroyRootObject(GameObject go)
		{
			while (go.transform.parent != null)
			{
				go = go.transform.gameObject;
			}
			UnityEngine.Object.DontDestroyOnLoad(go);
		}

		public void Copy(Camera blueprint, bool master = false, bool hasOtherConsumers = false)
		{
			VRLog.Info("Copying camera: {0}", blueprint ? blueprint.name : "NULL");
			if ((bool)blueprint && (bool)blueprint.GetComponent<CameraSlave>())
			{
				VRLog.Warn("Is already slave -- NOOP");
				return;
			}
			if (master && UseNewCamera(blueprint))
			{
				_Blueprint = blueprint ?? _Camera;
				ApplyToCameras(delegate(Camera targetCamera)
				{
					targetCamera.nearClipPlane = VR.Context.NearClipPlane;
					targetCamera.farClipPlane = Mathf.Max(Blueprint.farClipPlane, 10f);
					targetCamera.clearFlags = ((Blueprint.clearFlags == CameraClearFlags.Skybox) ? CameraClearFlags.Skybox : CameraClearFlags.Color);
					targetCamera.renderingPath = Blueprint.renderingPath;
					targetCamera.clearStencilAfterLightingPass = Blueprint.clearStencilAfterLightingPass;
					targetCamera.depthTextureMode = Blueprint.depthTextureMode;
					targetCamera.layerCullDistances = Blueprint.layerCullDistances;
					targetCamera.layerCullSpherical = Blueprint.layerCullSpherical;
					targetCamera.useOcclusionCulling = Blueprint.useOcclusionCulling;
					targetCamera.allowHDR = Blueprint.allowHDR;
					targetCamera.backgroundColor = Blueprint.backgroundColor;
					Skybox component2 = Blueprint.GetComponent<Skybox>();
					if (component2 != null)
					{
						Skybox skybox = targetCamera.gameObject.GetComponent<Skybox>();
						if (skybox == null)
						{
							skybox = skybox.gameObject.AddComponent<Skybox>();
						}
						skybox.material = component2.material;
					}
				});
			}
			if ((bool)blueprint)
			{
				blueprint.gameObject.AddComponent<CameraSlave>();
				AudioListener component = blueprint.GetComponent<AudioListener>();
				if ((bool)component)
				{
					UnityEngine.Object.Destroy(component);
				}
				if (!hasOtherConsumers && blueprint.targetTexture == null && VR.Interpreter.IsIrrelevantCamera(blueprint))
				{
					blueprint.gameObject.AddComponent<CameraKiller>();
				}
			}
			if (master)
			{
				this.InitializeCamera(this, new InitializeCameraEventArgs(GetComponent<Camera>(), Blueprint));
			}
			this.CopiedCamera(this, new CopiedCameraEventArgs(blueprint));
		}

		private bool UseNewCamera(Camera blueprint)
		{
			if ((bool)_Blueprint && _Blueprint != _Camera && _Blueprint != blueprint && _Blueprint.name == "Main Camera")
			{
				VRLog.Info("Using {0} over {1} as main camera", _Blueprint.name, blueprint.name);
				return false;
			}
			return true;
		}

		private void UpdateCameraConfig()
		{
			int num = Slaves.Aggregate(0, (int cull, CameraSlave cam) => cull | cam.cullingMask);
			num |= VR.Interpreter.DefaultCullingMask;
			num &= ~LayerMask.GetMask(VR.Context.UILayer, VR.Context.InvisibleLayer);
			num &= ~VR.Context.IgnoreMask;
			VRLog.Info("The camera sees {0} ({1})", string.Join(", ", UnityHelper.GetLayerNames(num)), string.Join(", ", Slaves.Select((CameraSlave s) => s.name).ToArray()));
			GetComponent<Camera>().cullingMask = num;
		}

		public void CopyFX(Camera blueprint)
		{
			CopyFX(blueprint.gameObject, base.gameObject, true);
			FixEffectOrder();
		}

		public void FixEffectOrder()
		{
			if (!SteamCam)
			{
				SteamCam = GetComponent<SteamVR_Camera>();
			}
			SteamCam.ForceLast();
			SteamCam = GetComponent<SteamVR_Camera>();
		}

		private void CopyFX(GameObject source, GameObject target, bool disabledSourceFx = false)
		{
			foreach (MonoBehaviour cameraEffect in target.GetCameraEffects())
			{
				UnityEngine.Object.DestroyImmediate(cameraEffect);
			}
			int num = target.GetComponents<Component>().Length;
			VRLog.Info("Copying FX to {0}...", target.name);
			foreach (MonoBehaviour cameraEffect2 in source.GetCameraEffects())
			{
				if (VR.Interpreter.IsAllowedEffect(cameraEffect2))
				{
					VRLog.Info("Copy FX: {0} (enabled={1})", cameraEffect2.GetType().Name, cameraEffect2.enabled);
					MonoBehaviour monoBehaviour = target.CopyComponentFrom(cameraEffect2);
					if ((bool)monoBehaviour)
					{
						VRLog.Info("Attached!");
					}
					monoBehaviour.enabled = cameraEffect2.enabled;
				}
				else
				{
					VRLog.Info("Skipping image effect {0}", cameraEffect2.GetType().Name);
				}
				if (disabledSourceFx)
				{
					cameraEffect2.enabled = false;
				}
			}
			VRLog.Info("{0} components before the additions, {1} after", num, target.GetComponents<Component>().Length);
		}

		private void ApplyToCameras(CameraOperation operation)
		{
			operation(SteamCam.GetComponent<Camera>());
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if ((bool)SteamCam.origin)
			{
				SteamCam.origin.localScale = Vector3.one * VR.Settings.IPDScale;
			}
		}

		public void Refresh()
		{
			CopyFX(Blueprint);
		}

		internal Camera Clone(bool copyEffects = true)
		{
			Camera camera = new GameObject("VRGIN_Camera_Clone").CopyComponentFrom(SteamCam.GetComponent<Camera>());
			if (copyEffects)
			{
				CopyFX(SteamCam.gameObject, camera.gameObject);
			}
			camera.transform.position = base.transform.position;
			camera.transform.rotation = base.transform.rotation;
			camera.nearClipPlane = 0.01f;
			return camera;
		}

		internal void RegisterSlave(CameraSlave slave)
		{
			VRLog.Info("Camera went online: {0}", slave.name);
			Slaves.Add(slave);
			UpdateCameraConfig();
		}

		internal void UnregisterSlave(CameraSlave slave)
		{
			VRLog.Info("Camera went offline: {0}", slave.name);
			Slaves.Remove(slave);
			UpdateCameraConfig();
		}

		public void SyncSkybox()
		{
			if (HasValidBlueprint && (bool)_Camera)
			{
				if (_Blueprint.clearFlags == CameraClearFlags.Skybox && _Camera.clearFlags != CameraClearFlags.Skybox)
				{
					_Camera.clearFlags = _Blueprint.clearFlags;
				}
				else if (_Blueprint.clearFlags == CameraClearFlags.Color && _Camera.clearFlags != CameraClearFlags.Color)
				{
					_Camera.clearFlags = _Blueprint.clearFlags;
				}
				if (_Camera.clearFlags == CameraClearFlags.Color)
				{
					_Camera.backgroundColor = _Blueprint.backgroundColor;
				}
			}
		}
	}
}
