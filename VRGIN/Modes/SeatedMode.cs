using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Controls.Tools;
using VRGIN.Core;
using VRGIN.Visuals;

namespace VRGIN.Modes
{
    public class SeatedMode : ControlMode
    {
        private static bool _IsFirstStart = true;

        protected GUIMonitor Monitor;

        protected IActor LockTarget;

        protected ImpersonationMode LockMode;

        public override IEnumerable<Type> Tools => base.Tools.Concat(new Type[1] { typeof(MenuTool) });

        public override ETrackingUniverseOrigin TrackingOrigin => ETrackingUniverseOrigin.TrackingUniverseSeated;

        protected override void OnStart()
        {
            base.OnStart();
            if (_IsFirstStart)
            {
                VR.Camera.SteamCam.origin.transform.position = new Vector3(0f, 0f, 0f);
                Recenter();
                _IsFirstStart = false;
            }

            Monitor = GUIMonitor.Create();
            Monitor.transform.SetParent(VR.Camera.SteamCam.origin, false);
            OpenVR.ChaperoneSetup.SetWorkingPlayAreaSize(1000f, 1000f);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if (!VR.Camera.HasValidBlueprint || !VR.Camera.Blueprint) return;
            if (LockTarget != null && LockTarget.IsValid)
            {
                VR.Camera.Blueprint.transform.position = LockTarget.Eyes.position;
                if (LockMode == ImpersonationMode.Approximately)
                    VR.Camera.Blueprint.transform.eulerAngles = new Vector3(0f, LockTarget.Eyes.eulerAngles.y, 0f);
                else
                    VR.Camera.Blueprint.transform.rotation = LockTarget.Eyes.rotation;
            }

            VR.Camera.SteamCam.origin.transform.position = VR.Camera.Blueprint.transform.position;
            if (VR.Settings.PitchLock && LockTarget == null)
            {
                VR.Camera.SteamCam.origin.transform.eulerAngles = new Vector3(0f, VR.Camera.Blueprint.transform.eulerAngles.y, 0f);
                CorrectRotationLock();
            }
            else
                VR.Camera.SteamCam.origin.transform.rotation = VR.Camera.Blueprint.transform.rotation;
        }

        protected virtual void SyncCameras() { }

        protected virtual void CorrectRotationLock() { }

        public override void Impersonate(IActor actor, ImpersonationMode mode)
        {
            base.Impersonate(actor, mode);
            SyncCameras();
            LockTarget = actor;
            LockMode = mode;
            Recenter();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Destroy(Monitor.gameObject);
        }

        protected override IEnumerable<IShortcut> CreateShortcuts()
        {
            return new List<IShortcut>
            {
                new KeyboardShortcut(VR.Shortcuts.GUIRaise, MoveGUI(0.1f)),
                new KeyboardShortcut(VR.Shortcuts.GUILower, MoveGUI(-0.1f)),
                new KeyboardShortcut(VR.Shortcuts.GUIIncreaseAngle, delegate { VR.Settings.Angle += Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIDecreaseAngle, delegate { VR.Settings.Angle -= Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIIncreaseDistance, delegate { VR.Settings.Distance += Time.deltaTime * 0.1f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIDecreaseDistance, delegate { VR.Settings.Distance -= Time.deltaTime * 0.1f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIRotateLeft, delegate { VR.Settings.Rotation += Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIRotateRight, delegate { VR.Settings.Rotation -= Time.deltaTime * 50f; }),
                new KeyboardShortcut(VR.Shortcuts.GUIChangeProjection, ChangeProjection),
                new MultiKeyboardShortcut(VR.Shortcuts.ToggleRotationLock, ToggleRotationLock),
                new MultiKeyboardShortcut(VR.Shortcuts.ImpersonateApproximately, delegate
                {
                    if (LockTarget == null || !LockTarget.IsValid)
                        Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Approximately);
                    else
                        Impersonate(null);
                }),
                new MultiKeyboardShortcut(VR.Shortcuts.ImpersonateExactly, delegate
                {
                    if (LockTarget == null || !LockTarget.IsValid)
                        Impersonate(VR.Interpreter.FindNextActorToImpersonate(), ImpersonationMode.Exactly);
                    else
                        Impersonate(null);
                }),
                new MultiKeyboardShortcut(VR.Shortcuts.ResetView, Recenter)
            }.Concat(base.CreateShortcuts());
        }

        private void ToggleRotationLock()
        {
            SyncCameras();
            VR.Settings.PitchLock = !VR.Settings.PitchLock;
        }

        private void ChangeProjection()
        {
            VR.Settings.Projection = (GUIMonitor.CurvinessState)((int)(VR.Settings.Projection + 1) % Enum.GetValues(typeof(GUIMonitor.CurvinessState)).Length);
        }

        public void Recenter()
        {
            VRLog.Info("Recenter");
            OpenVR.Chaperone.ResetZeroPose(ETrackingUniverseOrigin.TrackingUniverseSeated);
        }

        protected Action MoveGUI(float speed)
        {
            return delegate { VR.Settings.OffsetY += speed * Time.deltaTime; };
        }
    }
}
