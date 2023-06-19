using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Action_Single_Source : SteamVR_Action_In_Source, ISteamVR_Action_Single, ISteamVR_Action_In_Source, ISteamVR_Action_Source
    {
        protected static uint actionData_size;

        public float changeTolerance = Mathf.Epsilon;

        protected InputAnalogActionData_t actionData;

        protected InputAnalogActionData_t lastActionData;

        protected SteamVR_Action_Single singleAction;

        public float axis
        {
            get
            {
                if (active) return actionData.x;
                return 0f;
            }
        }

        public float lastAxis
        {
            get
            {
                if (active) return lastActionData.x;
                return 0f;
            }
        }

        public float delta
        {
            get
            {
                if (active) return actionData.deltaX;
                return 0f;
            }
        }

        public float lastDelta
        {
            get
            {
                if (active) return lastActionData.deltaX;
                return 0f;
            }
        }

        public override bool changed { get; protected set; }

        public override bool lastChanged { get; protected set; }

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

        public event SteamVR_Action_Single.AxisHandler onAxis;

        public event SteamVR_Action_Single.ActiveChangeHandler onActiveChange;

        public event SteamVR_Action_Single.ActiveChangeHandler onActiveBindingChange;

        public event SteamVR_Action_Single.ChangeHandler onChange;

        public event SteamVR_Action_Single.UpdateHandler onUpdate;

        public override void Preinitialize(SteamVR_Action wrappingAction, SteamVR_Input_Sources forInputSource)
        {
            base.Preinitialize(wrappingAction, forInputSource);
            singleAction = (SteamVR_Action_Single)wrappingAction;
        }

        public override void Initialize()
        {
            base.Initialize();
            if (actionData_size == 0) actionData_size = (uint)Marshal.SizeOf(typeof(InputAnalogActionData_t));
        }

        public void RemoveAllListeners()
        {
            Delegate[] invocationList;
            if (onAxis != null)
            {
                invocationList = onAxis.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var @delegate in array) onAxis -= (SteamVR_Action_Single.AxisHandler)@delegate;
                }
            }

            if (onUpdate != null)
            {
                invocationList = onUpdate.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate2 in array) onUpdate -= (SteamVR_Action_Single.UpdateHandler)delegate2;
                }
            }

            if (onChange == null) return;
            invocationList = onChange.GetInvocationList();
            if (invocationList != null)
            {
                var array = invocationList;
                foreach (var delegate3 in array) onChange -= (SteamVR_Action_Single.ChangeHandler)delegate3;
            }
        }

        public override void UpdateValue()
        {
            lastActionData = actionData;
            lastActive = active;
            var analogActionData = OpenVR.Input.GetAnalogActionData(handle, ref actionData, actionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (analogActionData != 0) Debug.LogError("<b>[SteamVR]</b> GetAnalogActionData error (" + fullPath + "): " + analogActionData.ToString() + " handle: " + handle);
            updateTime = Time.realtimeSinceStartup;
            changed = false;
            if (active)
            {
                if (delta > changeTolerance || delta < 0f - changeTolerance)
                {
                    changed = true;
                    changedTime = Time.realtimeSinceStartup + actionData.fUpdateTime;
                    if (onChange != null) onChange(singleAction, inputSource, axis, delta);
                }

                if (axis != 0f && onAxis != null) onAxis(singleAction, inputSource, axis, delta);
                if (onUpdate != null) onUpdate(singleAction, inputSource, axis, delta);
            }

            if (onActiveBindingChange != null && lastActiveBinding != activeBinding) onActiveBindingChange(singleAction, inputSource, activeBinding);
            if (onActiveChange != null && lastActive != active) onActiveChange(singleAction, inputSource, activeBinding);
        }
    }
}
