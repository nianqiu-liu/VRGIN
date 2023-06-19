using UnityEngine;

namespace VRGIN.Helpers
{
    public class AxisBoundTravelDistanceRumble : TravelDistanceRumble
    {
        private Vector3 _Axis;

        protected override float DistanceTraveled => Mathf.Abs(Vector3.Dot(CurrentPosition - PrevPosition, _Axis));

        public AxisBoundTravelDistanceRumble(ushort intensity, float distance, Transform transform, Vector3 axis)
            : base(intensity, distance, transform)
        {
            _Axis = axis;
        }
    }
}
