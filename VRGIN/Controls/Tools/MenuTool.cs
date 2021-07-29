using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR;
using VRGIN.Core;
using VRGIN.Helpers;
using VRGIN.Native;
using VRGIN.Visuals;

namespace VRGIN.Controls.Tools
{
    public class MenuTool : Tool
    {
        private float pressDownTime;

        private Vector2 touchDownPosition;

        private WindowsInterop.POINT touchDownMousePosition;

        private float timeAbandoned;

        private double _DeltaX;

        private double _DeltaY;

        public GUIQuad Gui { get; private set; }

        public override Texture2D Image => UnityHelper.LoadImage("icon_settings.png");

        public void TakeGUI(GUIQuad quad)
        {
            if ((bool)quad && !Gui && !quad.IsOwned)
            {
                Gui = quad;
                Gui.transform.parent = transform;
                Gui.transform.SetParent(transform, true);
                Gui.transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
                Gui.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.IsOwned = true;
            }
        }

        public void AbandonGUI()
        {
            if ((bool)Gui)
            {
                timeAbandoned = Time.unscaledTime;
                Gui.IsOwned = false;
                Gui.transform.SetParent(VR.Camera.SteamCam.origin, true);
                Gui = null;
            }
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            Gui = GUIQuad.Create(null);
            Gui.transform.parent = transform;
            Gui.transform.localScale = Vector3.one * 0.3f;
            Gui.transform.localPosition = new Vector3(0f, 0.05f, -0.06f);
            Gui.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Gui.IsOwned = true;
            DontDestroyOnLoad(Gui.gameObject);
            Gui.gameObject.SetActive(enabled);
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        protected override void OnDestroy()
        {
            if (VR.Quitting)
            {
                return;
            }
            DestroyImmediate(Gui.gameObject);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (Gui)
            {
                Gui.gameObject.SetActive(false);
            }

            if (pressDownTime != 0)
            {
                pressDownTime = 0;
                VR.Input.Mouse.LeftButtonUp();
            }
        }
        protected override void OnEnable()
        {
            base.OnEnable();
            if ((bool)Gui) Gui.gameObject.SetActive(true);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            var controller = Controller;
            if (controller.GetPressDown(12884901888uL))
            {
                VR.Input.Mouse.LeftButtonDown();
                pressDownTime = Time.unscaledTime;
            }

            if (controller.GetPressUp(4uL))
            {
                if ((bool)Gui)
                    AbandonGUI();
                else
                    TakeGUI(GUIQuadRegistry.Quads.FirstOrDefault((GUIQuad q) => !q.IsOwned));
            }

            if (controller.GetTouchDown(EVRButtonId.k_EButton_Axis0))
            {
                touchDownPosition = controller.GetAxis(EVRButtonId.k_EButton_Axis0);
                touchDownMousePosition = MouseOperations.GetClientCursorPosition();
            }

            if (controller.GetTouch(EVRButtonId.k_EButton_Axis0) && Time.unscaledTime - pressDownTime > 0.3f)
            {
                var axis = controller.GetAxis(EVRButtonId.k_EButton_Axis0);
                var vector = axis - (VR.HMD == HMDType.Oculus ? Vector2.zero : touchDownPosition);
                var num = VR.HMD == HMDType.Oculus ? Time.unscaledDeltaTime * 5f : 1f;
                _DeltaX += (double)(vector.x * (float)VRGUI.Width) * 0.1 * (double)num;
                _DeltaY += (double)((0f - vector.y) * (float)VRGUI.Height) * 0.2 * (double)num;
                var num2 = (int)(_DeltaX > 0.0 ? Math.Floor(_DeltaX) : Math.Ceiling(_DeltaX));
                var num3 = (int)(_DeltaY > 0.0 ? Math.Floor(_DeltaY) : Math.Ceiling(_DeltaY));
                _DeltaX -= num2;
                _DeltaY -= num3;
                VR.Input.Mouse.MoveMouseBy(num2, num3);
                touchDownPosition = axis;
            }

            if (controller.GetPressUp(12884901888uL))
            {
                VR.Input.Mouse.LeftButtonUp();
                pressDownTime = 0f;
            }
        }

        public override List<HelpText> GetHelpTexts()
        {
            return new List<HelpText>(new HelpText[3]
            {
                HelpText.Create("Tap to click", FindAttachPosition("trackpad"), new Vector3(0f, 0.02f, 0.05f)),
                HelpText.Create("Slide to move cursor", FindAttachPosition("trackpad"), new Vector3(0.05f, 0.02f, 0f), new Vector3(0.015f, 0f, 0f)),
                HelpText.Create("Attach/Remove menu", FindAttachPosition("lgrip"), new Vector3(-0.06f, 0f, -0.05f))
            });
        }
    }
}
