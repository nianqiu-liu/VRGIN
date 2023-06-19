using System.Collections.Generic;
using UnityEngine;
using VRGIN.Core;

namespace VRGIN.Helpers
{
	public class CameraConsumer : IScreenGrabber
	{
		private RenderTexture _Texture;

		private bool _SpareMainCamera;

		private bool _SoftMode;

		public bool Check(Camera camera)
		{
			if (!camera.GetComponent("UICamera") && !camera.name.Contains("VR") && camera.targetTexture == null)
			{
				if (camera.CompareTag("MainCamera"))
				{
					return !_SpareMainCamera;
				}
				return true;
			}
			return false;
		}

		public IEnumerable<RenderTexture> GetTextures()
		{
			yield return _Texture;
		}

		public void OnAssign(Camera camera)
		{
			if (_SoftMode)
			{
				camera.cullingMask = 0;
				camera.nearClipPlane = 1f;
				camera.farClipPlane = 1f;
			}
			else
			{
				camera.enabled = false;
			}
		}

		public CameraConsumer(bool spareMainCamera = false, bool softMode = false)
		{
			_SoftMode = softMode;
			_SpareMainCamera = spareMainCamera;
			_Texture = new RenderTexture(1, 1, 0);
		}
	}
}
