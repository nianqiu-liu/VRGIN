using System;
using UnityEngine;
using Valve.VR;

namespace VRGIN.Helpers
{
    public class VelocityRumble : IRumbleSession, IComparable<IRumbleSession>
    {
        private readonly ushort _MicroDuration;

        private readonly float _MilliInterval;

        private readonly float _MaxVelocity;

        private readonly ushort _MaxMicroDuration;

        private readonly float _MaxMilliInterval;

        public bool IsOver { get; set; }

        public ushort MicroDuration => (ushort)((float)(int)_MicroDuration + Device.GetVelocity().magnitude / _MaxVelocity * (float)(_MaxMicroDuration - _MicroDuration));

        public float MilliInterval => Mathf.Lerp(_MilliInterval, _MaxMilliInterval, Device.GetVelocity().magnitude / _MaxVelocity);

        public SteamVR_Behaviour_Pose Device { get; set; }

        public VelocityRumble(SteamVR_Behaviour_Pose device, ushort microDuration, float milliInterval, float maxVelocity, ushort maxMicroDuration, float maxMilliInterval)
        {
            Device = device;
            _MaxMilliInterval = maxMilliInterval;
            _MaxMicroDuration = maxMicroDuration;
            _MaxVelocity = maxVelocity;
            _MilliInterval = milliInterval;
            _MicroDuration = microDuration;
        }

        public int CompareTo(IRumbleSession other)
        {
            return MicroDuration.CompareTo(other.MicroDuration);
        }

        public void Consume() { }
    }
}
