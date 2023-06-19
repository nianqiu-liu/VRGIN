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
			LookTargetController lookTargetController = gameObject.AddComponent<LookTargetController>();
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
			Object.DontDestroyOnLoad(Target.gameObject);
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			if ((bool)_RootNode && (bool)VR.Camera.SteamCam.head.transform)
			{
				Transform transform = VR.Camera.SteamCam.head.transform;
				Vector3 normalized = (transform.position - _RootNode.position).normalized;
				Target.transform.position = transform.position + normalized * Offset;
			}
		}

		private void OnDestroy()
		{
			Object.Destroy(Target.gameObject);
		}
	}
}
