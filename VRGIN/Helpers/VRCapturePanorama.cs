using System;
using CapturePanorama;
using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public class VRCapturePanorama : global::CapturePanorama.CapturePanorama
    {
        private Camera _Camera;

        private IShortcut _Shortcut;

        protected override void OnStart()
        {
            fadeMaterial = UnityHelper.LoadFromAssetBundle<Material>(ResourceManager.Capture, "Fade material");
            convertPanoramaShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "ConvertPanoramaShader");
            convertPanoramaStereoShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "ConvertPanoramaStereoShader");
            textureToBufferShader = UnityHelper.LoadFromAssetBundle<ComputeShader>(ResourceManager.Capture, "TextureToBufferShader");
            captureStereoscopic = VR.Settings.Capture.Stereoscopic;
            interpupillaryDistance = SteamVR.instance.GetFloatProperty(ETrackedDeviceProperty.Prop_UserIpdMeters_Float) * VR.Settings.IPDScale;
            captureKey = KeyCode.None;
            _Shortcut = new MultiKeyboardShortcut(VR.Settings.Capture.Shortcut, delegate
            {
                if (!Capturing)
                {
                    var text = $"{Application.productName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
                    VRLog.Info("Panorama capture key pressed, capturing " + text);
                    CaptureScreenshotAsync(text);
                }
            });
            base.OnStart();
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            _Shortcut.Evaluate();
        }

        public override Camera[] GetCaptureCameras()
        {
            return new Camera[1] { _Camera };
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            if ((bool)_Camera) Destroy(_Camera.gameObject);
        }

        public override bool OnCaptureStart()
        {
            if (!_Camera)
            {
                _Camera = VR.Camera.Clone(VR.Settings.Capture.WithEffects);
                _Camera.gameObject.SetActive(false);
                if (VR.Settings.Capture.HideGUI) _Camera.cullingMask &= ~LayerMask.GetMask(VR.Context.GuiLayer);
            }

            _Camera.transform.position = VR.Camera.Head.position;
            if (VR.Settings.Capture.SetCameraUpright)
            {
                var forward = Vector3.ProjectOnPlane(VR.Camera.Head.forward, Vector3.up).normalized;
                if ((double)forward.magnitude < 0.1) forward = Vector3.forward;
                _Camera.transform.rotation = Quaternion.LookRotation(forward);
            }
            else
                _Camera.transform.rotation = VR.Camera.Head.rotation;

            return true;
        }

        public override void AfterRenderPanorama()
        {
            base.AfterRenderPanorama();
        }
    }
}
