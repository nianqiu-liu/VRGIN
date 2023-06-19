using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace Valve.VR
{
	public class SteamVR_Behaviour : MonoBehaviour
	{
		private const string openVRDeviceName = "OpenVR";

		public static bool forcingInitialization = false;

		private static SteamVR_Behaviour _instance;

		public bool initializeSteamVROnAwake = true;

		public bool doNotDestroy = true;

		[HideInInspector]
		public SteamVR_Render steamvr_render;

		internal static bool isPlaying = false;

		private static bool initializing = false;

		private Coroutine initializeCoroutine;

		protected static int lastFrameCount = -1;

		public static SteamVR_Behaviour instance
		{
			get
			{
				if (_instance == null)
				{
					Initialize(false);
				}
				return _instance;
			}
		}

		public static void Initialize(bool forceUnityVRToOpenVR = false)
		{
			if (!(_instance == null) || initializing)
			{
				return;
			}
			initializing = true;
			GameObject gameObject = null;
			if (forceUnityVRToOpenVR)
			{
				forcingInitialization = true;
			}
			SteamVR_Render steamVR_Render = UnityEngine.Object.FindObjectOfType<SteamVR_Render>();
			if (steamVR_Render != null)
			{
				gameObject = steamVR_Render.gameObject;
			}
			SteamVR_Behaviour steamVR_Behaviour = UnityEngine.Object.FindObjectOfType<SteamVR_Behaviour>();
			if (steamVR_Behaviour != null)
			{
				gameObject = steamVR_Behaviour.gameObject;
			}
			if (gameObject == null)
			{
				GameObject gameObject2 = new GameObject("[SteamVR]");
				_instance = gameObject2.AddComponent<SteamVR_Behaviour>();
				_instance.steamvr_render = gameObject2.AddComponent<SteamVR_Render>();
			}
			else
			{
				steamVR_Behaviour = gameObject.GetComponent<SteamVR_Behaviour>();
				if (steamVR_Behaviour == null)
				{
					steamVR_Behaviour = gameObject.AddComponent<SteamVR_Behaviour>();
				}
				if (steamVR_Render != null)
				{
					steamVR_Behaviour.steamvr_render = steamVR_Render;
				}
				else
				{
					steamVR_Behaviour.steamvr_render = gameObject.GetComponent<SteamVR_Render>();
					if (steamVR_Behaviour.steamvr_render == null)
					{
						steamVR_Behaviour.steamvr_render = gameObject.AddComponent<SteamVR_Render>();
					}
				}
				_instance = steamVR_Behaviour;
			}
			if (_instance != null && _instance.doNotDestroy)
			{
				UnityEngine.Object.DontDestroyOnLoad(_instance.transform.root.gameObject);
			}
			initializing = false;
		}

		protected void Awake()
		{
			isPlaying = true;
			if (initializeSteamVROnAwake && !forcingInitialization)
			{
				InitializeSteamVR(false);
			}
		}

		public void InitializeSteamVR(bool forceUnityVRToOpenVR = false)
		{
			if (forceUnityVRToOpenVR)
			{
				forcingInitialization = true;
				if (initializeCoroutine != null)
				{
					StopCoroutine(initializeCoroutine);
				}
				if (XRSettings.loadedDeviceName == "OpenVR")
				{
					EnableOpenVR();
				}
				else
				{
					initializeCoroutine = StartCoroutine(DoInitializeSteamVR(forceUnityVRToOpenVR));
				}
			}
			else
			{
				SteamVR.Initialize(false);
			}
		}

		private IEnumerator DoInitializeSteamVR(bool forceUnityVRToOpenVR = false)
		{
			XRSettings.LoadDeviceByName("OpenVR");
			yield return null;
			EnableOpenVR();
		}

		private void EnableOpenVR()
		{
			XRSettings.enabled = true;
			SteamVR.Initialize(false);
			initializeCoroutine = null;
			forcingInitialization = false;
		}

		protected void OnEnable()
		{
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Combine(Camera.onPreCull, new Camera.CameraCallback(OnCameraPreCull));
			SteamVR_Events.System(EVREventType.VREvent_Quit).Listen(OnQuit);
		}

		protected void OnDisable()
		{
			Camera.onPreCull = (Camera.CameraCallback)Delegate.Remove(Camera.onPreCull, new Camera.CameraCallback(OnCameraPreCull));
			SteamVR_Events.System(EVREventType.VREvent_Quit).Remove(OnQuit);
		}

		protected void OnCameraPreCull(Camera cam)
		{
			if (cam.stereoEnabled)
			{
				PreCull();
			}
		}

		protected void PreCull()
		{
			if (OpenVR.Input != null && Time.frameCount != lastFrameCount)
			{
				lastFrameCount = Time.frameCount;
				SteamVR_Input.OnPreCull();
			}
		}

		protected void FixedUpdate()
		{
			if (OpenVR.Input != null)
			{
				SteamVR_Input.FixedUpdate();
			}
		}

		protected void LateUpdate()
		{
			if (OpenVR.Input != null)
			{
				SteamVR_Input.LateUpdate();
			}
		}

		protected void Update()
		{
			if (OpenVR.Input != null)
			{
				SteamVR_Input.Update();
			}
		}

		protected void OnQuit(VREvent_t vrEvent)
		{
			Application.Quit();
		}
	}
}
