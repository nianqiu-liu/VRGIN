using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Action_Vector3_Source : SteamVR_Action_In_Source, ISteamVR_Action_Vector3, ISteamVR_Action_In_Source, ISteamVR_Action_Source
    {
        protected static uint actionData_size;

        public float changeTolerance = Mathf.Epsilon;

        protected InputAnalogActionData_t actionData;

        protected InputAnalogActionData_t lastActionData;

        protected SteamVR_Action_Vector3 vector3Action;

        public Vector3 axis { get; protected set; }

        public Vector3 lastAxis { get; protected set; }

        public Vector3 delta { get; protected set; }

        public Vector3 lastDelta { get; protected set; }

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

        public event SteamVR_Action_Vector3.AxisHandler onAxis;

        public event SteamVR_Action_Vector3.ActiveChangeHandler onActiveChange;

        public event SteamVR_Action_Vector3.ActiveChangeHandler onActiveBindingChange;

        public event SteamVR_Action_Vector3.ChangeHandler onChange;

        public event SteamVR_Action_Vector3.UpdateHandler onUpdate;

        public override void Preinitialize(SteamVR_Action wrappingAction, SteamVR_Input_Sources forInputSource)
        {
            base.Preinitialize(wrappingAction, forInputSource);
            vector3Action = (SteamVR_Action_Vector3)wrappingAction;
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
                    foreach (var @delegate in array) onAxis -= (SteamVR_Action_Vector3.AxisHandler)@delegate;
                }
            }

            if (onUpdate != null)
            {
                invocationList = onUpdate.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate2 in array) onUpdate -= (SteamVR_Action_Vector3.UpdateHandler)delegate2;
                }
            }

            if (onChange == null) return;
            invocationList = onChange.GetInvocationList();
            if (invocationList != null)
            {
                var array = invocationList;
                foreach (var delegate3 in array) onChange -= (SteamVR_Action_Vector3.ChangeHandler)delegate3;
            }
        }

        public override void UpdateValue()
        {
            lastActionData = actionData;
            lastActive = active;
            lastAxis = axis;
            lastDelta = delta;
            var analogActionData = OpenVR.Input.GetAnalogActionData(handle, ref actionData, actionData_size, SteamVR_Input_Source.GetHandle(inputSource));
            if (analogActionData != 0) Debug.LogError("<b>[SteamVR]</b> GetAnalogActionData error (" + fullPath + "): " + analogActionData.ToString() + " handle: " + handle);
            updateTime = Time.realtimeSinceStartup;
            axis = new Vector3(actionData.x, actionData.y, actionData.z);
            delta = new Vector3(actionData.deltaX, actionData.deltaY, actionData.deltaZ);
            changed = false;
            if (active)
            {
                if (delta.magnitude > changeTolerance)
                {
                    changed = true;
                    changedTime = Time.realtimeSinceStartup + actionData.fUpdateTime;
                    if (onChange != null) onChange(vector3Action, inputSource, axis, delta);
                }

                if (axis != Vector3.zero && onAxis != null) onAxis(vector3Action, inputSource, axis, delta);
                if (onUpdate != null) onUpdate(vector3Action, inputSource, axis, delta);
            }

            if (onActiveBindingChange != null && lastActiveBinding != activeBinding) onActiveBindingChange(vector3Action, inputSource, activeBinding);
            if (onActiveChange != null && lastActive != active) onActiveChange(vector3Action, inputSource, activeBinding);
        }
    }
}
