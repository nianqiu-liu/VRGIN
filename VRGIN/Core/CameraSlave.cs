using System;
using UnityEngine;

namespace VRGIN.Core
{
    public class CameraSlave : ProtectedBehaviour
    {
        public Camera Camera => GetComponent<Camera>();

        public float nearClipPlane { get; private set; }

        public float farClipPlane { get; private set; }

        public CameraClearFlags clearFlags { get; private set; }

        public RenderingPath renderingPath { get; private set; }

        public bool clearStencilAfterLightingPass { get; private set; }

        public DepthTextureMode depthTextureMode { get; private set; }

        public float[] layerCullDistances { get; private set; }

        public bool layerCullSpherical { get; private set; }

        public bool useOcclusionCulling { get; private set; }

        public Color backgroundColor { get; private set; }

        public int cullingMask { get; private set; }

        protected override void OnAwake()
        {
            base.OnAwake();
            var camera = Camera;
            if (!camera)
            {
                VRLog.Error("No camera found! {0}", name);
                Destroy(this);
                return;
            }

            nearClipPlane = camera.nearClipPlane;
            farClipPlane = camera.farClipPlane;
            clearFlags = camera.clearFlags;
            renderingPath = camera.renderingPath;
            clearStencilAfterLightingPass = camera.clearStencilAfterLightingPass;
            depthTextureMode = camera.depthTextureMode;
            layerCullDistances = camera.layerCullDistances;
            layerCullSpherical = camera.layerCullSpherical;
            useOcclusionCulling = camera.useOcclusionCulling;
            backgroundColor = camera.backgroundColor;
            cullingMask = camera.cullingMask;
        }

        public void OnEnable()
        {
            try
            {
                VR.Camera.RegisterSlave(this);
            }
            catch (Exception obj)
            {
                VRLog.Error(obj);
            }
        }

        public void OnDisable()
        {
            if (VR.Quitting)
            {
                return;
            }
            try
            {
                VR.Camera.UnregisterSlave(this);
            }
            catch (Exception obj)
            {
                VRLog.Error(obj);
            }
        }
    }
}
