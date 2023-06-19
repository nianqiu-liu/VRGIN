using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VRGIN.Core
{
	public class GameInterpreter : ProtectedBehaviour
	{
		public virtual IEnumerable<IActor> Actors
		{
			get
			{
				yield break;
			}
		}

		public virtual bool IsEveryoneHeaded => Actors.All((IActor a) => a.HasHead);

		public virtual int DefaultCullingMask => LayerMask.GetMask("Default");

		protected override void OnLevel(int level)
		{
			base.OnLevel(level);
			VRLog.Info("Loaded level {0}", level);
		}

		public virtual IActor FindImpersonatedActor()
		{
			return Actors.FirstOrDefault((IActor a) => !a.HasHead);
		}

		public virtual IActor FindNextActorToImpersonate()
		{
			List<IActor> list = Actors.ToList();
			IActor actor2 = FindImpersonatedActor();
			if (actor2 != null)
			{
				list.Remove(actor2);
			}
			return list.OrderByDescending((IActor actor) => Vector3.Dot((actor.Eyes.position - VR.Camera.transform.position).normalized, VR.Camera.SteamCam.head.forward)).FirstOrDefault();
		}

		public virtual Camera FindCamera()
		{
			return Camera.main;
		}

		public virtual IEnumerable<Camera> FindSubCameras()
		{
			return Camera.allCameras.Where((Camera c) => c.targetTexture == null).Except(new Camera[1] { Camera.main });
		}

		public CameraJudgement JudgeCamera(Camera camera)
		{
			if (camera.name.Contains("VRGIN") || camera.name == "poseUpdater")
			{
				return CameraJudgement.Ignore;
			}
			return JudgeCameraInternal(camera);
		}

		protected virtual CameraJudgement JudgeCameraInternal(Camera camera)
		{
			bool flag = VR.GUI.IsInterested(camera);
			if (camera.targetTexture == null)
			{
				if (flag)
				{
					return CameraJudgement.GUIAndCamera;
				}
				if (camera.CompareTag("MainCamera"))
				{
					return CameraJudgement.MainCamera;
				}
				return CameraJudgement.SubCamera;
			}
			if (!flag)
			{
				return CameraJudgement.Ignore;
			}
			return CameraJudgement.GUI;
		}

		public virtual bool IsBody(Collider collider)
		{
			return false;
		}

		public virtual bool IsIgnoredCanvas(Canvas canvas)
		{
			return false;
		}

		public virtual bool IsAllowedEffect(MonoBehaviour effect)
		{
			return !VR.Settings.EffectBlacklist.Contains(effect.GetType().Name);
		}

		public virtual bool IsIrrelevantCamera(Camera blueprint)
		{
			return true;
		}

		public virtual bool IsUICamera(Camera camera)
		{
			return camera.GetComponent("UICamera");
		}
	}
}
