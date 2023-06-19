using UnityEngine;
using VRGIN.Core;
using VRGIN.Helpers;

namespace VRGIN.Visuals
{
    public class DefaultMaterialPalette : IMaterialPalette
    {
        public Material UnlitTransparentCombined { get; set; }

        public Material Sprite { get; set; }

        public Shader StandardShader { get; set; }

        public Material Unlit { get; set; }

        public Material UnlitTransparent { get; set; }

        public DefaultMaterialPalette()
        {
            Unlit = CreateUnlit();
            UnlitTransparent = CreateUnlitTransparent();
            UnlitTransparentCombined = CreateUnlitTransparentCombined();
            StandardShader = CreateStandardShader();
            Sprite = CreateSprite();
            if (!Unlit || !Unlit.shader) VRLog.Error("Could not load Unlit material!");
            if (!UnlitTransparent || !UnlitTransparent.shader) VRLog.Error("Could not load UnlitTransparent material!");
            if (!UnlitTransparentCombined || !UnlitTransparentCombined.shader) VRLog.Error("Could not load UnlitTransparentCombined material!");
            if (!StandardShader) VRLog.Error("Could not load StandardShader material!");
            if (!Sprite || !Sprite.shader)
            {
                VRLog.Error("Could not load Sprite material!");
                Sprite = UnlitTransparent;
            }
        }

        private Material CreateUnlitTransparentCombined()
        {
            return new Material(UnityHelper.GetShaderByMaterial("UnlitTransparentCombined"));
        }

        private Material CreateSprite()
        {
            return new Material(UnityHelper.GetShaderByMaterial("Sprites-Default"));
        }

        private Shader CreateStandardShader()
        {
            return Shader.Find("Standard");
        }

        private Material CreateUnlit()
        {
            return new Material(UnityHelper.GetShaderByMaterial("Unlit"));
        }

        private Material CreateUnlitTransparent()
        {
            return new Material(UnityHelper.GetShaderByMaterial("UnlitTransparent"));
        }
    }
}
