using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using VRGIN.Core;

namespace VRGIN.Visuals
{
    public class GUIQuad : ProtectedBehaviour
    {
        private Renderer renderer;

        public bool IsOwned;

        private IScreenGrabber _Source;

        public static GUIQuad Create(IScreenGrabber source = null)
        {
            source = source ?? VR.GUI;

            VRLog.Info("Create GUI");
            var gui = GameObject.CreatePrimitive(PrimitiveType.Quad).AddComponent<GUIQuad>();
            gui.name = "GUIQuad";
            gui.gameObject.AddComponent<Rigidbody>().isKinematic = true;
            
            if(source != VR.GUI)
            {
                gui.gameObject.SetActive(false);
                gui._Source = source;
                gui.gameObject.SetActive(true);
            }

            gui.UpdateGUI();
            return gui;
        }

        protected override void OnAwake()
        {
            renderer = GetComponent<Renderer>();
            _Source = VR.GUI;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            gameObject.layer = LayerMask.NameToLayer(VRManager.Instance.Context.GuiLayer);
        }

        protected override void OnStart()
        {
            base.OnStart();
            UpdateAspect();
        }

        protected virtual void OnEnable()
        {
            if (IsGUISource())
            {
                VRLog.Info("Start listening to GUI ({0})", name);
                GUIQuadRegistry.Register(this);
                VR.GUI.Listen();
            }
        }

        protected virtual void OnDisable()
        {
            if (VR.Quitting)
            {
                return;
            }

            if (IsGUISource())
            {
                VRLog.Info("Stop listening to GUI ({0})", name);
                GUIQuadRegistry.Unregister(this);
                VR.GUI.Unlisten();
            }
        }

        private bool IsGUISource()
        {
            return _Source == VR.GUI;
        }

        public virtual void UpdateAspect()
        {
            var y = transform.localScale.y;
            var x = y / (float)Screen.height * (float)Screen.width;
            transform.localScale = new Vector3(x, y, 1f);
        }

        public virtual void UpdateGUI()
        {
            UpdateAspect();
            if (!renderer) VRLog.Warn("No renderer!");
            try
            {
                renderer.receiveShadows = false;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                var textures = _Source.GetTextures();
                VRLog.Info("Updating GUI {0} with {1} textures", name, textures.Count());
                if (textures.Count() >= 2)
                {
                    renderer.material = VR.Context.Materials.UnlitTransparentCombined;
                    renderer.material.SetTexture("_MainTex", textures.FirstOrDefault());
                    renderer.material.SetTexture("_SubTex", textures.Last());
                }
                else
                {
                    renderer.material = VR.Context.Materials.UnlitTransparent;
                    renderer.material.SetTexture("_MainTex", textures.FirstOrDefault());
                }
            }
            catch (Exception obj)
            {
                VRLog.Info(obj);
            }
        }
    }
}
