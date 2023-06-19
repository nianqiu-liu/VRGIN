using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Visuals;

namespace VRGIN.Modes
{
	public abstract class ControlMode : ProtectedBehaviour
	{
		private static bool _ControllerFound;

		private static int cnter;

		public abstract ETrackingUniverseOrigin TrackingOrigin { get; }

		public Controller Left { get; private set; }

		public Controller Right { get; private set; }

		protected IEnumerable<IShortcut> Shortcuts { get; private set; }

		public virtual IEnumerable<Type> Tools => new List<Type>();

		public virtual IEnumerable<Type> LeftTools => new List<Type>();

		public virtual IEnumerable<Type> RightTools => new List<Type>();

		internal event EventHandler<EventArgs> ControllersCreated = delegate
		{
		};

		public virtual void Impersonate(IActor actor)
		{
			Impersonate(actor, ImpersonationMode.Approximately);
		}

		public virtual void Impersonate(IActor actor, ImpersonationMode mode)
		{
			if (actor != null)
			{
				actor.HasHead = false;
			}
		}

		public virtual void MoveToPosition(Vector3 targetPosition, bool ignoreHeight = true)
		{
			MoveToPosition(targetPosition, VR.Camera.SteamCam.head.rotation, ignoreHeight);
		}

		public virtual void MoveToPosition(Vector3 targetPosition, Quaternion rotation = default(Quaternion), bool ignoreHeight = true)
		{
			Vector3 forwardVector = Calculator.GetForwardVector(rotation);
			Vector3 forwardVector2 = Calculator.GetForwardVector(VR.Camera.SteamCam.head.rotation);
			VR.Camera.SteamCam.origin.rotation *= Quaternion.FromToRotation(forwardVector2, forwardVector);
			float y = (ignoreHeight ? 0f : targetPosition.y);
			float y2 = (ignoreHeight ? 0f : VR.Camera.SteamCam.head.position.y);
			targetPosition = new Vector3(targetPosition.x, y, targetPosition.z);
			Vector3 vector = new Vector3(VR.Camera.SteamCam.head.position.x, y2, VR.Camera.SteamCam.head.position.z);
			VR.Camera.SteamCam.origin.position += targetPosition - vector;
		}

		protected override void OnStart()
		{
			CreateControllers();
			Shortcuts = CreateShortcuts();
			OpenVR.Compositor.SetTrackingSpace(TrackingOrigin);
			InitializeScreenCapture();
		}

		protected virtual void OnEnable()
		{
			SteamVR_Events.DeviceConnected.Listen(OnDeviceConnected);
			VRLog.Info("Enabled {0}", GetType().Name);
		}

		protected virtual void OnDisable()
		{
			VRLog.Info("Disabled {0}", GetType().Name);
			SteamVR_Events.DeviceConnected.Remove(OnDeviceConnected);
		}

		protected virtual void CreateControllers()
		{
			SteamVR_Camera steamCam = VR.Camera.SteamCam;
			steamCam.origin.gameObject.SetActive(false);
			Left = CreateLeftController();
			Left.transform.SetParent(steamCam.origin, false);
			Right = CreateRightController();
			Right.transform.SetParent(steamCam.origin, false);
			Left.Other = Right;
			Right.Other = Left;
			Left.InputSources = SteamVR_Input_Sources.LeftHand;
			Right.InputSources = SteamVR_Input_Sources.RightHand;
			steamCam.origin.gameObject.SetActive(true);
			VRLog.Info("---- Initialize left tools");
			InitializeTools(Left, true);
			VRLog.Info("---- Initialize right tools");
			InitializeTools(Right, false);
			this.ControllersCreated(this, new EventArgs());
			UnityEngine.Object.DontDestroyOnLoad(steamCam.origin.gameObject);
		}

		public virtual void OnDestroy()
		{
			VRLog.Debug("ControlMode OnDestroy called.");
			UnityEngine.Object.Destroy(Left);
			UnityEngine.Object.Destroy(Right);
			if (Shortcuts == null)
			{
				return;
			}
			foreach (IShortcut shortcut in Shortcuts)
			{
				shortcut.Dispose();
			}
		}

		protected virtual void InitializeTools(Controller controller, bool isLeft)
		{
			IEnumerable<Type> enumerable = Tools.Concat(isLeft ? LeftTools : RightTools).Distinct();
			foreach (Type item in enumerable)
			{
				controller.AddTool(item);
			}
			VRLog.Info("{0} tools added", enumerable.Count());
		}

		protected virtual Controller CreateLeftController()
		{
			return LeftController.Create();
		}

		protected virtual Controller CreateRightController()
		{
			return RightController.Create();
		}

		protected virtual IEnumerable<IShortcut> CreateShortcuts()
		{
			return new List<IShortcut>
			{
				new KeyboardShortcut(VR.Shortcuts.ShrinkWorld, delegate
				{
					VR.Settings.IPDScale += Time.deltaTime;
				}),
				new KeyboardShortcut(VR.Shortcuts.EnlargeWorld, delegate
				{
					VR.Settings.IPDScale -= Time.deltaTime;
				}),
				new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + D"), delegate
				{
					UnityHelper.DumpScene("dump.json");
				}),
				new MultiKeyboardShortcut(new KeyStroke("Ctrl + C"), new KeyStroke("Ctrl + I"), delegate
				{
					UnityHelper.DumpScene("dump.json", true);
				}),
				new MultiKeyboardShortcut(VR.Shortcuts.ToggleUserCamera, ToggleUserCamera),
				new MultiKeyboardShortcut(VR.Shortcuts.SaveSettings, delegate
				{
					VR.Settings.Save();
				}),
				new KeyboardShortcut(VR.Shortcuts.LoadSettings, delegate
				{
					VR.Settings.Reload();
				}),
				new KeyboardShortcut(VR.Shortcuts.ResetSettings, delegate
				{
					VR.Settings.Reset();
				}),
				new KeyboardShortcut(VR.Shortcuts.ApplyEffects, delegate
				{
					VR.Manager.ToggleEffects();
				})
			};
		}

		protected virtual void ToggleUserCamera()
		{
			if (!PlayerCamera.Created)
			{
				VRLog.Info("Create user camera");
				PlayerCamera.Create();
			}
			else
			{
				VRLog.Info("Remove user camera");
				PlayerCamera.Remove();
			}
		}

		protected virtual void InitializeScreenCapture()
		{
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			OpenVR.Compositor.SetTrackingSpace(TrackingOrigin);
			SteamVR_Camera steamCam = VRCamera.Instance.SteamCam;
			int num = 0;
			bool isEveryoneHeaded = VR.Interpreter.IsEveryoneHeaded;
			foreach (IActor actor in VR.Interpreter.Actors)
			{
				if (actor.HasHead)
				{
					if (isEveryoneHeaded)
					{
						Vector3 position = actor.Eyes.position;
						Vector3 forward = actor.Eyes.forward;
						Vector3 position2 = steamCam.head.position;
						Vector3 forward2 = steamCam.head.forward;
						if (Vector3.Distance(position, position2) * VR.Context.UnitToMeter < 0.15f && Vector3.Dot(forward, forward2) > 0.6f)
						{
							actor.HasHead = false;
						}
					}
				}
				else if (Vector3.Distance(actor.Eyes.position, steamCam.head.position) * VR.Context.UnitToMeter > 0.3f)
				{
					actor.HasHead = true;
				}
				num++;
			}
			CheckInput();
		}

		protected void CheckInput()
		{
			foreach (IShortcut shortcut in Shortcuts)
			{
				shortcut.Evaluate();
			}
		}

		private void OnDeviceConnected(int idx, bool connected)
		{
			if (_ControllerFound)
			{
				return;
			}
			VRLog.Info("Device connected: {0}", (uint)idx);
			if (connected && idx != 0)
			{
				CVRSystem system = OpenVR.System;
				if (system != null && system.GetTrackedDeviceClass((uint)idx) == ETrackedDeviceClass.Controller)
				{
					_ControllerFound = true;
					ChangeModeOnControllersDetected();
				}
			}
		}

		protected virtual void ChangeModeOnControllersDetected()
		{
		}
	}
}
