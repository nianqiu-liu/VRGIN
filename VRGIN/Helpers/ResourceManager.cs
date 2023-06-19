using System.Linq;
using UnityEngine;

namespace VRGIN.Helpers
{
    public static class ResourceManager
    {
        private static readonly string VERSION = string.Join(".", Application.unityVersion.Split('.').Take(2).ToArray());

        public static byte[] SteamVR
        {
            get
            {
                VERSION.StartsWith("2019");
                return Resource.steamvr_2019;
            }
        }

        public static byte[] Capture => SteamVR;

        public static byte[] Hands => new byte[0];
    }
}
