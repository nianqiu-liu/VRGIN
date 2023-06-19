using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Visuals
{
	public class SimulatedCursor : ProtectedBehaviour
	{
		private Texture2D _Sprite;

		private Texture2D _DefaultSprite;

		private Vector2 _Scale;

		public static SimulatedCursor Create()
		{
			return new GameObject("VRGIN_Cursor").AddComponent<SimulatedCursor>();
		}

		protected override void OnAwake()
		{
			base.OnAwake();
			_DefaultSprite = UnityHelper.LoadImage("cursor.png");
			_Scale = new Vector2(_DefaultSprite.width, _DefaultSprite.height) * 0.5f;
		}

		protected override void OnStart()
		{
			base.OnStart();
		}

		private void OnGUI()
		{
			GUI.depth = -2147483647;
			if (Cursor.visible)
			{
				Vector2 vector = new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
				GUI.DrawTexture(new Rect(vector.x, vector.y, _Scale.x, _Scale.y), _Sprite ?? _DefaultSprite);
			}
		}

		public void SetCursor(Texture2D texture)
		{
			_Sprite = texture;
		}
	}
}
