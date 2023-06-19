using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Helpers;

namespace Valve.VR
{
	public class SteamVR_ExternalCamera : MonoBehaviour
	{
		[Serializable]
		public struct Config
		{
			public float x;

			public float y;

			public float z;

			public float rx;

			public float ry;

			public float rz;

			public float fov;

			public float near;

			public float far;

			public float sceneResolutionScale;

			public float frameSkip;

			public float nearOffset;

			public float farOffset;

			public float hmdOffset;

			public float r;

			public float g;

			public float b;

			public float a;

			public bool disableStandardAssets;
		}

		private SteamVR_Action_Pose cameraPose;

		private SteamVR_Input_Sources cameraInputSource = SteamVR_Input_Sources.Camera;

		[Space]
		public Config config;

		public string configPath;

		[Tooltip("This will automatically activate the action set the specified pose belongs to. And deactivate it when this component is disabled.")]
		public bool autoEnableDisableActionSet = true;

		private FileSystemWatcher watcher;

		private Camera cam;

		private Transform target;

		private GameObject clipQuad;

		private Material clipMaterial;

		protected SteamVR_ActionSet activatedActionSet;

		protected SteamVR_Input_Sources activatedInputSource;

		private Material colorMat;

		private Material alphaMat;

		private Camera[] cameras;

		private Rect[] cameraRects;

		private float sceneResolutionScale;

		public void ReadConfig()
		{
			try
			{
				HmdMatrix34_t pose = default(HmdMatrix34_t);
				bool flag = false;
				object obj = config;
				string[] array = File.ReadAllLines(configPath);
				for (int i = 0; i < array.Length; i++)
				{
					string[] array2 = array[i].Split('=');
					if (array2.Length != 2)
					{
						continue;
					}
					string text = array2[0];
					if (text == "m")
					{
						string[] array3 = array2[1].Split(',');
						if (array3.Length == 12)
						{
							pose.m0 = float.Parse(array3[0]);
							pose.m1 = float.Parse(array3[1]);
							pose.m2 = float.Parse(array3[2]);
							pose.m3 = float.Parse(array3[3]);
							pose.m4 = float.Parse(array3[4]);
							pose.m5 = float.Parse(array3[5]);
							pose.m6 = float.Parse(array3[6]);
							pose.m7 = float.Parse(array3[7]);
							pose.m8 = float.Parse(array3[8]);
							pose.m9 = float.Parse(array3[9]);
							pose.m10 = float.Parse(array3[10]);
							pose.m11 = float.Parse(array3[11]);
							flag = true;
						}
					}
					else if (text == "disableStandardAssets")
					{
						FieldInfo field = obj.GetType().GetField(text);
						if (field != null)
						{
							field.SetValue(obj, bool.Parse(array2[1]));
						}
					}
					else
					{
						FieldInfo field2 = obj.GetType().GetField(text);
						if (field2 != null)
						{
							field2.SetValue(obj, float.Parse(array2[1]));
						}
					}
				}
				config = (Config)obj;
				if (flag)
				{
					SteamVR_Utils.RigidTransform rigidTransform = new SteamVR_Utils.RigidTransform(pose);
					config.x = rigidTransform.pos.x;
					config.y = rigidTransform.pos.y;
					config.z = rigidTransform.pos.z;
					Vector3 eulerAngles = rigidTransform.rot.eulerAngles;
					config.rx = eulerAngles.x;
					config.ry = eulerAngles.y;
					config.rz = eulerAngles.z;
				}
			}
			catch
			{
			}
			target = null;
			if (watcher == null)
			{
				FileInfo fileInfo = new FileInfo(configPath);
				watcher = new FileSystemWatcher(fileInfo.DirectoryName, fileInfo.Name);
				watcher.NotifyFilter = NotifyFilters.LastWrite;
				watcher.Changed += OnChanged;
				watcher.EnableRaisingEvents = true;
			}
		}

		public void SetupPose(SteamVR_Action_Pose newCameraPose, SteamVR_Input_Sources newCameraSource)
		{
			cameraPose = newCameraPose;
			cameraInputSource = newCameraSource;
			AutoEnableActionSet();
			SteamVR_Behaviour_Pose steamVR_Behaviour_Pose = base.gameObject.AddComponent<SteamVR_Behaviour_Pose>();
			steamVR_Behaviour_Pose.poseAction = newCameraPose;
			steamVR_Behaviour_Pose.inputSource = newCameraSource;
		}

		public void SetupDeviceIndex(int deviceIndex)
		{
			base.gameObject.AddComponent<SteamVR_TrackedObject>().SetDeviceIndex(deviceIndex);
		}

		private void OnChanged(object source, FileSystemEventArgs e)
		{
			ReadConfig();
		}

		public void AttachToCamera(SteamVR_Camera steamVR_Camera)
		{
			Camera camera;
			if (steamVR_Camera == null)
			{
				camera = Camera.main;
				if (target == camera.transform)
				{
					return;
				}
				target = camera.transform;
			}
			else
			{
				camera = steamVR_Camera.camera;
				if (target == steamVR_Camera.head)
				{
					return;
				}
				target = steamVR_Camera.head;
			}
			Transform parent = base.transform.parent;
			Transform parent2 = target.parent;
			parent.parent = parent2;
			parent.localPosition = Vector3.zero;
			parent.localRotation = Quaternion.identity;
			parent.localScale = Vector3.one;
			camera.enabled = false;
			GameObject gameObject = UnityEngine.Object.Instantiate(camera.gameObject);
			camera.enabled = true;
			gameObject.name = "camera";
			UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<SteamVR_Camera>());
			UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<SteamVR_Fade>());
			cam = gameObject.GetComponent<Camera>();
			cam.stereoTargetEye = StereoTargetEyeMask.None;
			cam.fieldOfView = config.fov;
			cam.useOcclusionCulling = false;
			cam.enabled = false;
			cam.rect = new Rect(0f, 0f, 1f, 1f);
			colorMat = new Material(UnityHelper.GetShader("Custom/SteamVR_ColorOut"));
			alphaMat = new Material(UnityHelper.GetShader("Custom/SteamVR_AlphaOut"));
			clipMaterial = new Material(UnityHelper.GetShader("Custom/SteamVR_ClearAll"));
			Transform transform = gameObject.transform;
			transform.parent = base.transform;
			transform.localPosition = new Vector3(config.x, config.y, config.z);
			transform.localRotation = Quaternion.Euler(config.rx, config.ry, config.rz);
			transform.localScale = Vector3.one;
			while (transform.childCount > 0)
			{
				UnityEngine.Object.DestroyImmediate(transform.GetChild(0).gameObject);
			}
			clipQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
			clipQuad.name = "ClipQuad";
			UnityEngine.Object.DestroyImmediate(clipQuad.GetComponent<MeshCollider>());
			MeshRenderer component = clipQuad.GetComponent<MeshRenderer>();
			component.material = clipMaterial;
			component.shadowCastingMode = ShadowCastingMode.Off;
			component.receiveShadows = false;
			component.lightProbeUsage = LightProbeUsage.Off;
			component.reflectionProbeUsage = ReflectionProbeUsage.Off;
			Transform obj = clipQuad.transform;
			obj.parent = transform;
			obj.localScale = new Vector3(1000f, 1000f, 1f);
			obj.localRotation = Quaternion.identity;
			clipQuad.SetActive(false);
		}

		public float GetTargetDistance()
		{
			if (target == null)
			{
				return config.near + 0.01f;
			}
			Transform transform = cam.transform;
			Vector3 normalized = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
			Vector3 inPoint = target.position + new Vector3(target.forward.x, 0f, target.forward.z).normalized * config.hmdOffset;
			return Mathf.Clamp(0f - new Plane(normalized, inPoint).GetDistanceToPoint(transform.position), config.near + 0.01f, config.far - 0.01f);
		}

		public void RenderNear()
		{
			int num = Screen.width / 2;
			int num2 = Screen.height / 2;
			if (cam.targetTexture == null || cam.targetTexture.width != num || cam.targetTexture.height != num2)
			{
				RenderTexture renderTexture = new RenderTexture(num, num2, 24, RenderTextureFormat.ARGB32);
				renderTexture.antiAliasing = ((QualitySettings.antiAliasing == 0) ? 1 : QualitySettings.antiAliasing);
				cam.targetTexture = renderTexture;
			}
			cam.nearClipPlane = config.near;
			cam.farClipPlane = config.far;
			CameraClearFlags clearFlags = cam.clearFlags;
			Color backgroundColor = cam.backgroundColor;
			cam.clearFlags = CameraClearFlags.Color;
			cam.backgroundColor = Color.clear;
			clipMaterial.color = new Color(config.r, config.g, config.b, config.a);
			float num3 = Mathf.Clamp(GetTargetDistance() + config.nearOffset, config.near, config.far);
			Transform parent = clipQuad.transform.parent;
			clipQuad.transform.position = parent.position + parent.forward * num3;
			MonoBehaviour[] array = null;
			bool[] array2 = null;
			if (config.disableStandardAssets)
			{
				array = cam.gameObject.GetComponents<MonoBehaviour>();
				array2 = new bool[array.Length];
				for (int i = 0; i < array.Length; i++)
				{
					MonoBehaviour monoBehaviour = array[i];
					if (monoBehaviour.enabled && monoBehaviour.GetType().ToString().StartsWith("UnityStandardAssets."))
					{
						monoBehaviour.enabled = false;
						array2[i] = true;
					}
				}
			}
			clipQuad.SetActive(true);
			cam.Render();
			Graphics.DrawTexture(new Rect(0f, 0f, num, num2), cam.targetTexture, colorMat);
			MonoBehaviour monoBehaviour2 = cam.gameObject.GetComponent("PostProcessingBehaviour") as MonoBehaviour;
			if (monoBehaviour2 != null && monoBehaviour2.enabled)
			{
				monoBehaviour2.enabled = false;
				cam.Render();
				monoBehaviour2.enabled = true;
			}
			Graphics.DrawTexture(new Rect(num, 0f, num, num2), cam.targetTexture, alphaMat);
			clipQuad.SetActive(false);
			if (array != null)
			{
				for (int j = 0; j < array.Length; j++)
				{
					if (array2[j])
					{
						array[j].enabled = true;
					}
				}
			}
			cam.clearFlags = clearFlags;
			cam.backgroundColor = backgroundColor;
		}

		public void RenderFar()
		{
			cam.nearClipPlane = config.near;
			cam.farClipPlane = config.far;
			cam.Render();
			int num = Screen.width / 2;
			int num2 = Screen.height / 2;
			Graphics.DrawTexture(new Rect(0f, num2, num, num2), cam.targetTexture, colorMat);
		}

		private void OnGUI()
		{
		}

		private void OnEnable()
		{
			cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
			if (cameras != null)
			{
				int num = cameras.Length;
				cameraRects = new Rect[num];
				for (int i = 0; i < num; i++)
				{
					Camera camera = cameras[i];
					cameraRects[i] = camera.rect;
					if (!(camera == cam) && !(camera.targetTexture != null) && !(camera.GetComponent<SteamVR_Camera>() != null))
					{
						camera.rect = new Rect(0.5f, 0f, 0.5f, 0.5f);
					}
				}
			}
			if (config.sceneResolutionScale > 0f)
			{
				sceneResolutionScale = SteamVR_Camera.sceneResolutionScale;
				SteamVR_Camera.sceneResolutionScale = config.sceneResolutionScale;
			}
			AutoEnableActionSet();
		}

		private void AutoEnableActionSet()
		{
			if (autoEnableDisableActionSet && cameraPose != null && !cameraPose.actionSet.IsActive(cameraInputSource))
			{
				activatedActionSet = cameraPose.actionSet;
				activatedInputSource = cameraInputSource;
				cameraPose.actionSet.Activate(cameraInputSource);
			}
		}

		private void OnDisable()
		{
			if (autoEnableDisableActionSet && activatedActionSet != null)
			{
				activatedActionSet.Deactivate(activatedInputSource);
				activatedActionSet = null;
			}
			if (cameras != null)
			{
				int num = cameras.Length;
				for (int i = 0; i < num; i++)
				{
					Camera camera = cameras[i];
					if (camera != null)
					{
						camera.rect = cameraRects[i];
					}
				}
				cameras = null;
				cameraRects = null;
			}
			if (config.sceneResolutionScale > 0f)
			{
				SteamVR_Camera.sceneResolutionScale = sceneResolutionScale;
			}
		}
	}
}
