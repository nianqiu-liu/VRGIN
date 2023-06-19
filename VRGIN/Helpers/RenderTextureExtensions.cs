using System.IO;
using UnityEngine;

namespace VRGIN.Helpers
{
    public static class RenderTextureExtensions
    {
        public static void SaveToFile(this RenderTexture renderTexture, string name)
        {
            var active = RenderTexture.active;
            RenderTexture.active = renderTexture;
            var texture2D = new Texture2D(renderTexture.width, renderTexture.height);
            texture2D.ReadPixels(new Rect(0f, 0f, texture2D.width, texture2D.height), 0, 0);
            var bytes = texture2D.EncodeToPNG();
            File.WriteAllBytes(name, bytes);
            Object.Destroy(texture2D);
            RenderTexture.active = active;
        }
    }
}
