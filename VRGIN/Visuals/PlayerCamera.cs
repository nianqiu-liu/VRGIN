using UnityEngine;
using Valve.VR;
using VRGIN.Controls;
using VRGIN.Core;

namespace VRGIN.Visuals
{
    public class PlayerCamera : ProtectedBehaviour
    {
        private SteamVR_RenderModel model;

        private Controller controller;

        private bool tracking;

        private static Vector3 S_Position;

        private static Quaternion S_Rotation;

        private Vector3 posOffset;

        private Quaternion rotOffset;

        public static bool Created { get; private set; }

        public static PlayerCamera Create()
        {
            Created = true;
            return GameObject.CreatePrimitive(PrimitiveType.Cube).AddComponent<PlayerCamera>();
        }

        internal static void Remove()
        {
            if (Created)
            {
                Destroy(FindObjectOfType<PlayerCamera>().gameObject);
                Created = false;
            }
        }

        protected void OnEnable()
        {
            VRGUI.Instance.Listen();
        }

        protected void OnDisable()
        {
            VRGUI.Instance.Unlisten();
        }

        protected override void OnAwake()
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.transform.SetParent(transform, false);
            var gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gameObject.transform.SetParent(transform, false);
            transform.localScale = 0.3f * Vector3.one;
            transform.localScale = new Vector3(0.2f, 0.2f, 0.4f);
            obj.transform.localScale = Vector3.one * 0.3f;
            obj.transform.localPosition = Vector3.forward * 0.5f;
            gameObject.transform.localScale = Vector3.one * 0.3f;
            gameObject.transform.localPosition = Vector3.up * 0.5f;
            GetComponent<Collider>().isTrigger = true;
            model = new GameObject("Model").AddComponent<SteamVR_RenderModel>();
            model.transform.SetParent(VR.Camera.SteamCam.head, false);
            model.shader = VR.Context.Materials.StandardShader;
            model.SetDeviceIndex(0);
            model.gameObject.layer = LayerMask.NameToLayer(VR.Context.InvisibleLayer);
            var camera = this.gameObject.AddComponent<Camera>();
            camera.depth = 1f;
            camera.nearClipPlane = 0.3f;
            camera.cullingMask = 0x7FFFFFFF & ~VR.Context.UILayerMask;
            transform.position = S_Position;
            transform.rotation = S_Rotation;
        }

        protected override void OnUpdate()
        {
            S_Position = transform.position;
            S_Rotation = transform.rotation;
            CheckInput();
        }

        protected void CheckInput()
        {
            if (!controller) return;
            if (!tracking && controller.Input.GetPressDown(EVRButtonId.k_EButton_Axis1))
            {
                tracking = true;
                posOffset = transform.position - controller.transform.position;
                rotOffset = Quaternion.Inverse(controller.transform.rotation) * transform.rotation;
            }
            else if (tracking)
            {
                if (controller.Input.GetPressUp(EVRButtonId.k_EButton_Axis1))
                {
                    tracking = false;
                    return;
                }

                transform.position = controller.transform.position + posOffset;
                transform.rotation = controller.transform.rotation * rotOffset;
            }
        }

        public void OnTriggerEnter(Collider other)
        {
            GetComponent<Renderer>().material.color = Color.red;
            controller = other.GetComponentInParent<Controller>();
            controller.ToolEnabled = false;
        }

        public void OnTriggerExit()
        {
            GetComponent<Renderer>().material.color = Color.white;
            controller.ToolEnabled = true;
            if (!tracking) controller = null;
        }
    }
}
