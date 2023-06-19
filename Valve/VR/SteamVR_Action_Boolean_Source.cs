using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Valve.VR
{
	public class SteamVR_Action_Boolean_Source : SteamVR_Action_In_Source, ISteamVR_Action_Boolean, ISteamVR_Action_In_Source, ISteamVR_Action_Source
	{
		protected static uint actionData_size;

		protected InputDigitalActionData_t actionData;

		protected InputDigitalActionData_t lastActionData;

		protected SteamVR_Action_Boolean booleanAction;

		public bool state
		{
			get
			{
				if (active)
				{
					return actionData.bState;
				}
				return false;
			}
		}

		public bool stateDown
		{
			get
			{
				if (active && actionData.bState)
				{
					return actionData.bChanged;
				}
				return false;
			}
		}

		public bool stateUp
		{
			get
			{
				if (active && !actionData.bState)
				{
					return actionData.bChanged;
				}
				return false;
			}
		}

		public override bool changed
		{
			get
			{
				if (active)
				{
					return actionData.bChanged;
				}
				return false;
			}
			protected set
			{
			}
		}

		public bool lastState => lastActionData.bState;

		public bool lastStateDown
		{
			get
			{
				if (lastActionData.bState)
				{
					return lastActionData.bChanged;
				}
				return false;
			}
		}

		public bool lastStateUp
		{
			get
			{
				if (!lastActionData.bState)
				{
					return lastActionData.bChanged;
				}
				return false;
			}
		}

		public override bool lastChanged
		{
			get
			{
				return lastActionData.bChanged;
			}
			protected set
			{
			}
		}

		public override ulong activeOrigin
		{
			get
			{
				if (active)
				{
					return actionData.activeOrigin;
				}
				return 0uL;
			}
		}

		public override ulong lastActiveOrigin => lastActionData.activeOrigin;

		public override bool active
		{
			get
			{
				if (activeBinding)
				{
					return action.actionSet.IsActive(base.inputSource);
				}
				return false;
			}
		}

		public override bool activeBinding => actionData.bActive;

		public override bool lastActive { get; protected set; }

		public override bool lastActiveBinding => lastActionData.bActive;

		public event SteamVR_Action_Boolean.StateDownHandler onStateDown;

		public event SteamVR_Action_Boolean.StateUpHandler onStateUp;

		public event SteamVR_Action_Boolean.StateHandler onState;

		public event SteamVR_Action_Boolean.ActiveChangeHandler onActiveChange;

		public event SteamVR_Action_Boolean.ActiveChangeHandler onActiveBindingChange;

		public event SteamVR_Action_Boolean.ChangeHandler onChange;

		public event SteamVR_Action_Boolean.UpdateHandler onUpdate;

		public override void Preinitialize(SteamVR_Action wrappingAction, SteamVR_Input_Sources forInputSource)
		{
			base.Preinitialize(wrappingAction, forInputSource);
			booleanAction = (SteamVR_Action_Boolean)wrappingAction;
		}

		public override void Initialize()
		{
			base.Initialize();
			if (actionData_size == 0)
			{
				actionData_size = (uint)Marshal.SizeOf(typeof(InputDigitalActionData_t));
			}
		}

		public void RemoveAllListeners()
		{
			Delegate[] invocationList;
			if (this.onStateDown != null)
			{
				invocationList = this.onStateDown.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate @delegate in array)
					{
						onStateDown -= (SteamVR_Action_Boolean.StateDownHandler)@delegate;
					}
				}
			}
			if (this.onStateUp != null)
			{
				invocationList = this.onStateUp.GetInvocationList();
				if (invocationList != null)
				{
					Delegate[] array = invocationList;
					foreach (Delegate delegate2 in array)
					{
						onStateUp -= (SteamVR_Action_Boolean.StateUpHandler)delegate2;
					}
				}
			}
			if (this.onState == null)
			{
				return;
			}
			invocationList = this.onState.GetInvocationList();
			if (invocationList != null)
			{
				Delegate[] array = invocationList;
				foreach (Delegate delegate3 in array)
				{
					onState -= (SteamVR_Action_Boolean.StateHandler)delegate3;
				}
			}
		}

		public override void UpdateValue()
		{
			lastActionData = actionData;
			lastActive = active;
			EVRInputError digitalActionData = OpenVR.Input.GetDigitalActionData(action.handle, ref actionData, actionData_size, inputSourceHandle);
			if (digitalActionData != 0)
			{
				Debug.LogError("<b>[SteamVR]</b> GetDigitalActionData error (" + action.fullPath + "): " + digitalActionData.ToString() + " handle: " + action.handle);
			}
			if (changed)
			{
				base.changedTime = Time.realtimeSinceStartup + actionData.fUpdateTime;
			}
			base.updateTime = Time.realtimeSinceStartup;
			if (active)
			{
				if (this.onStateDown != null && stateDown)
				{
					this.onStateDown(booleanAction, base.inputSource);
				}
				if (this.onStateUp != null && stateUp)
				{
					this.onStateUp(booleanAction, base.inputSource);
				}
				if (this.onState != null && state)
				{
					this.onState(booleanAction, base.inputSource);
				}
				if (this.onChange != null && changed)
				{
					this.onChange(booleanAction, base.inputSource, state);
				}
				if (this.onUpdate != null)
				{
					this.onUpdate(booleanAction, base.inputSource, state);
				}
			}
			if (this.onActiveBindingChange != null && lastActiveBinding != activeBinding)
			{
				this.onActiveBindingChange(booleanAction, base.inputSource, activeBinding);
			}
			if (this.onActiveChange != null && lastActive != active)
			{
				this.onActiveChange(booleanAction, base.inputSource, activeBinding);
			}
		}
	}
}
