using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
    public class LookTargetController : ProtectedBehaviour
    {
        private Transform _RootNode;

        public float Offset = 0.5f;

        public Transform Target { get; private set; }

        public static LookTargetController AttachTo(IActor actor, GameObject gameObject)
        {
            var lookTargetController = gameObject.AddComponent<LookTargetController>();
            lookTargetController._RootNode = actor.Eyes;
            return lookTargetController;
        }

        protected override void OnAwake()
        {
            base.OnAwake();
            CreateTarget();
        }

        private void CreateTarget()
        {
            Target = new GameObject("VRGIN_LookTarget").transform;
            DontDestroyOnLoad(Target.gameObject);
        }

        protected override void OnUpdate()
        {
            base.OnUpdate();
            if ((bool)_RootNode && (bool)VR.Camera.SteamCam.head.transform)
            {
                var transform = VR.Camera.SteamCam.head.transform;
                var normalized = (transform.position - _RootNode.position).normalized;
                Target.transform.position = transform.position + normalized * Offset;
            }
        }

        private void OnDestroy()
        {
            Destroy(Target.gameObject);
        }
    }
}
