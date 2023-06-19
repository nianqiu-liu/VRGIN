using UnityEngine;

namespace Valve.VR
{
    [RequireComponent(typeof(AudioListener))]
    public class SteamVR_Ears : MonoBehaviour
    {
        public SteamVR_Camera vrcam;

        private bool usingSpeakers;

        private Quaternion offset;

        private void OnNewPosesApplied()
        {
            var origin = vrcam.origin;
            var quaternion = origin != null ? origin.rotation : Quaternion.identity;
            transform.rotation = quaternion * offset;
        }

        private void OnEnable()
        {
            usingSpeakers = false;
            var settings = OpenVR.Settings;
            if (settings != null)
            {
                var peError = EVRSettingsError.None;
                if (settings.GetBool("steamvr", "usingSpeakers", ref peError))
                {
                    usingSpeakers = true;
                    var @float = settings.GetFloat("steamvr", "speakersForwardYawOffsetDegrees", ref peError);
                    offset = Quaternion.Euler(0f, @float, 0f);
                }
            }

            if (usingSpeakers) SteamVR_Events.NewPosesApplied.Listen(OnNewPosesApplied);
        }

        private void OnDisable()
        {
            if (usingSpeakers) SteamVR_Events.NewPosesApplied.Remove(OnNewPosesApplied);
        }
    }
}
