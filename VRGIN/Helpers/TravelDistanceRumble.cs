using System;
using UnityEngine;

namespace VRGIN.Helpers
{
	public class TravelDistanceRumble : IRumbleSession, IComparable<IRumbleSession>
	{
		private Transform _Transform;

		private float _Distance;

		protected Vector3 PrevPosition;

		protected Vector3 CurrentPosition;

		private bool _UseLocalPosition;

		public bool UseLocalPosition
		{
			get
			{
				return _UseLocalPosition;
			}
			set
			{
				_UseLocalPosition = value;
				Reset();
			}
		}

		public bool IsOver { get; private set; }

		public ushort MicroDuration { get; set; }

		public float MilliInterval
		{
			get
			{
				CurrentPosition = (_UseLocalPosition ? _Transform.localPosition : _Transform.position);
				if (DistanceTraveled > _Distance)
				{
					PrevPosition = CurrentPosition;
					return 0f;
				}
				return float.MaxValue;
			}
		}

		protected virtual float DistanceTraveled => Vector3.Distance(PrevPosition, CurrentPosition);

		public void Reset()
		{
			PrevPosition = (_UseLocalPosition ? _Transform.localPosition : _Transform.position);
		}

		public TravelDistanceRumble(ushort intensity, float distance, Transform transform)
		{
			MicroDuration = intensity;
			_Transform = transform;
			_Distance = distance;
			PrevPosition = transform.position;
		}

		public int CompareTo(IRumbleSession other)
		{
			return MicroDuration.CompareTo(other.MicroDuration);
		}

		public void Consume()
		{
		}

		public void Close()
		{
			IsOver = true;
		}
	}
}
