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
                if (active) return actionData.bState;
                return false;
            }
        }

        public bool stateDown
        {
            get
            {
                if (active && actionData.bState) return actionData.bChanged;
                return false;
            }
        }

        public bool stateUp
        {
            get
            {
                if (active && !actionData.bState) return actionData.bChanged;
                return false;
            }
        }

        public override bool changed
        {
            get
            {
                if (active) return actionData.bChanged;
                return false;
            }
            protected set { }
        }

        public bool lastState => lastActionData.bState;

        public bool lastStateDown
        {
            get
            {
                if (lastActionData.bState) return lastActionData.bChanged;
                return false;
            }
        }

        public bool lastStateUp
        {
            get
            {
                if (!lastActionData.bState) return lastActionData.bChanged;
                return false;
            }
        }

        public override bool lastChanged
        {
            get => lastActionData.bChanged;
            protected set { }
        }

        public override ulong activeOrigin
        {
            get
            {
                if (active) return actionData.activeOrigin;
                return 0uL;
            }
        }

        public override ulong lastActiveOrigin => lastActionData.activeOrigin;

        public override bool active
        {
            get
            {
                if (activeBinding) return action.actionSet.IsActive(inputSource);
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
            if (actionData_size == 0) actionData_size = (uint)Marshal.SizeOf(typeof(InputDigitalActionData_t));
        }

        public void RemoveAllListeners()
        {
            Delegate[] invocationList;
            if (onStateDown != null)
            {
                invocationList = onStateDown.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var @delegate in array) onStateDown -= (SteamVR_Action_Boolean.StateDownHandler)@delegate;
                }
            }

            if (onStateUp != null)
            {
                invocationList = onStateUp.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate2 in array) onStateUp -= (SteamVR_Action_Boolean.StateUpHandler)delegate2;
                }
            }

            if (onState == null) return;
            invocationList = onState.GetInvocationList();
            if (invocationList != null)
            {
                var array = invocationList;
                foreach (var delegate3 in array) onState -= (SteamVR_Action_Boolean.StateHandler)delegate3;
            }
        }

        public override void UpdateValue()
        {
            lastActionData = actionData;
            lastActive = active;
            var digitalActionData = OpenVR.Input.GetDigitalActionData(action.handle, ref actionData, actionData_size, inputSourceHandle);
            if (digitalActionData != 0) Debug.LogError("<b>[SteamVR]</b> GetDigitalActionData error (" + action.fullPath + "): " + digitalActionData.ToString() + " handle: " + action.handle);
            if (changed) changedTime = Time.realtimeSinceStartup + actionData.fUpdateTime;
            updateTime = Time.realtimeSinceStartup;
            if (active)
            {
                if (onStateDown != null && stateDown) onStateDown(booleanAction, inputSource);
                if (onStateUp != null && stateUp) onStateUp(booleanAction, inputSource);
                if (onState != null && state) onState(booleanAction, inputSource);
                if (onChange != null && changed) onChange(booleanAction, inputSource, state);
                if (onUpdate != null) onUpdate(booleanAction, inputSource, state);
            }

            if (onActiveBindingChange != null && lastActiveBinding != activeBinding) onActiveBindingChange(booleanAction, inputSource, activeBinding);
            if (onActiveChange != null && lastActive != active) onActiveChange(booleanAction, inputSource, activeBinding);
        }
    }
}
