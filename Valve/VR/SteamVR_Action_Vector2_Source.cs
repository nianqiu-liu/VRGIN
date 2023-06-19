using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Action_Vector2_Source : SteamVR_Action_In_Source, ISteamVR_Action_Vector2, ISteamVR_Action_In_Source, ISteamVR_Action_Source
    {
        protected static uint actionData_size;

        public float changeTolerance = Mathf.Epsilon;

        protected InputAnalogActionData_t actionData;

        protected InputAnalogActionData_t lastActionData;

        protected SteamVR_Action_Vector2 vector2Action;

        public Vector2 axis { get; protected set; }

        public Vector2 lastAxis { get; protected set; }

        public Vector2 delta { get; protected set; }

        public Vector2 lastDelta { get; protected set; }

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

        public event SteamVR_Action_Vector2.AxisHandler onAxis;

        public event SteamVR_Action_Vector2.ActiveChangeHandler onActiveChange;

        public event SteamVR_Action_Vector2.ActiveChangeHandler onActiveBindingChange;

        public event SteamVR_Action_Vector2.ChangeHandler onChange;

        public event SteamVR_Action_Vector2.UpdateHandler onUpdate;

        public override void Preinitialize(SteamVR_Action wrappingAction, SteamVR_Input_Sources forInputSource)
        {
            base.Preinitialize(wrappingAction, forInputSource);
            vector2Action = (SteamVR_Action_Vector2)wrappingAction;
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
                    foreach (var @delegate in array) onAxis -= (SteamVR_Action_Vector2.AxisHandler)@delegate;
                }
            }

            if (onUpdate != null)
            {
                invocationList = onUpdate.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate2 in array) onUpdate -= (SteamVR_Action_Vector2.UpdateHandler)delegate2;
                }
            }

            if (onChange == null) return;
            invocationList = onChange.GetInvocationList();
            if (invocationList != null)
            {
                var array = invocationList;
                foreach (var delegate3 in array) onChange -= (SteamVR_Action_Vector2.ChangeHandler)delegate3;
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
            axis = new Vector2(actionData.x, actionData.y);
            delta = new Vector2(actionData.deltaX, actionData.deltaY);
            changed = false;
            if (active)
            {
                if (delta.magnitude > changeTolerance)
                {
                    changed = true;
                    changedTime = Time.realtimeSinceStartup + actionData.fUpdateTime;
                    if (onChange != null) onChange(vector2Action, inputSource, axis, delta);
                }

                if (axis != Vector2.zero && onAxis != null) onAxis(vector2Action, inputSource, axis, delta);
                if (onUpdate != null) onUpdate(vector2Action, inputSource, axis, delta);
            }

            if (onActiveBindingChange != null && lastActiveBinding != activeBinding) onActiveBindingChange(vector2Action, inputSource, activeBinding);
            if (onActiveChange != null && lastActive != active) onActiveChange(vector2Action, inputSource, activeBinding);
        }
    }
}
