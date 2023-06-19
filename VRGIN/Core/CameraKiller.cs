using System.Linq;
using UnityEngine;
using VRGIN.Helpers;

namespace VRGIN.Core
{
    public class CameraKiller : ProtectedBehaviour
    {
        private MonoBehaviour[] _CameraEffects = new MonoBehaviour[0];

        private Camera _Camera;

        protected override void OnStart()
        {
            base.OnStart();
            _CameraEffects = gameObject.GetCameraEffects().ToArray();
            _Camera = GetComponent<Camera>();
            _Camera.cullingMask = 0;
            _Camera.depth = -9999f;
            _Camera.useOcclusionCulling = false;
            _Camera.clearFlags = CameraClearFlags.Nothing;
        }

        public void OnPreCull()
        {
            _Camera.enabled = false;
        }

        public void OnGUI()
        {
            if (Event.current.type == EventType.Repaint) _Camera.enabled = true;
        }
    }
}
