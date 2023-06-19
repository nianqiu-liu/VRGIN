using UnityEngine;
using Valve.VR;

namespace VRGIN.Core
{
    public class PlayArea
    {
        public float Scale { get; set; }

        public Vector3 Position { get; set; }

        public float Rotation { get; set; }

        public float Height
        {
            get => Position.y;
            set => Position = new Vector3(Position.x, value, Position.z);
        }

        public PlayArea()
        {
            Scale = 1f;
        }

        public void Apply()
        {
            var quaternion = Quaternion.Euler(0f, Rotation, 0f);
            var steamCam = VR.Camera.SteamCam;
            steamCam.origin.position = Position - quaternion * new Vector3(steamCam.head.transform.localPosition.x, 0f, steamCam.head.transform.localPosition.z) * Scale;
            steamCam.origin.rotation = quaternion;
            VR.Settings.IPDScale = Scale;
        }

        public void Reset()
        {
            Position = new Vector3(VR.Camera.Head.position.x, VR.Camera.Origin.position.y, VR.Camera.Head.position.z);
            Scale = VR.Settings.IPDScale;
            Rotation = VR.Camera.Origin.rotation.eulerAngles.y;
        }
    }
}
