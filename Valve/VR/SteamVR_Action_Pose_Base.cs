using System;
using UnityEngine;

namespace Valve.VR
{
	[Serializable]
	public abstract class SteamVR_Action_Pose_Base<SourceMap, SourceElement> : SteamVR_Action_In<SourceMap, SourceElement>, ISteamVR_Action_Pose, ISteamVR_Action_In_Source, ISteamVR_Action_Source where SourceMap : SteamVR_Action_Pose_Source_Map<SourceElement>, new() where SourceElement : SteamVR_Action_Pose_Source, new()
	{
		public Vector3 localPosition => sourceMap[SteamVR_Input_Sources.Any].localPosition;

		public Quaternion localRotation => sourceMap[SteamVR_Input_Sources.Any].localRotation;

		public ETrackingResult trackingState => sourceMap[SteamVR_Input_Sources.Any].trackingState;

		public Vector3 velocity => sourceMap[SteamVR_Input_Sources.Any].velocity;

		public Vector3 angularVelocity => sourceMap[SteamVR_Input_Sources.Any].angularVelocity;

		public bool poseIsValid => sourceMap[SteamVR_Input_Sources.Any].poseIsValid;

		public bool deviceIsConnected => sourceMap[SteamVR_Input_Sources.Any].deviceIsConnected;

		public Vector3 lastLocalPosition => sourceMap[SteamVR_Input_Sources.Any].lastLocalPosition;

		public Quaternion lastLocalRotation => sourceMap[SteamVR_Input_Sources.Any].lastLocalRotation;

		public ETrackingResult lastTrackingState => sourceMap[SteamVR_Input_Sources.Any].lastTrackingState;

		public Vector3 lastVelocity => sourceMap[SteamVR_Input_Sources.Any].lastVelocity;

		public Vector3 lastAngularVelocity => sourceMap[SteamVR_Input_Sources.Any].lastAngularVelocity;

		public bool lastPoseIsValid => sourceMap[SteamVR_Input_Sources.Any].lastPoseIsValid;

		public bool lastDeviceIsConnected => sourceMap[SteamVR_Input_Sources.Any].lastDeviceIsConnected;

		protected static void SetUniverseOrigin(ETrackingUniverseOrigin newOrigin)
		{
			for (int i = 0; i < SteamVR_Input.actionsPose.Length; i++)
			{
				SteamVR_Input.actionsPose[i].sourceMap.SetTrackingUniverseOrigin(newOrigin);
			}
			for (int j = 0; j < SteamVR_Input.actionsSkeleton.Length; j++)
			{
				SteamVR_Input.actionsSkeleton[j].sourceMap.SetTrackingUniverseOrigin(newOrigin);
			}
		}

		public SteamVR_Action_Pose_Base()
		{
		}

		public virtual void UpdateValues(bool skipStateAndEventUpdates)
		{
			sourceMap.UpdateValues(skipStateAndEventUpdates);
		}

		public bool GetVelocitiesAtTimeOffset(SteamVR_Input_Sources inputSource, float secondsFromNow, out Vector3 velocity, out Vector3 angularVelocity)
		{
			return sourceMap[inputSource].GetVelocitiesAtTimeOffset(secondsFromNow, out velocity, out angularVelocity);
		}

		public bool GetPoseAtTimeOffset(SteamVR_Input_Sources inputSource, float secondsFromNow, out Vector3 localPosition, out Quaternion localRotation, out Vector3 velocity, out Vector3 angularVelocity)
		{
			return sourceMap[inputSource].GetPoseAtTimeOffset(secondsFromNow, out localPosition, out localRotation, out velocity, out angularVelocity);
		}

		public virtual void UpdateTransform(SteamVR_Input_Sources inputSource, Transform transformToUpdate)
		{
			sourceMap[inputSource].UpdateTransform(transformToUpdate);
		}

		public Vector3 GetLocalPosition(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].localPosition;
		}

		public Quaternion GetLocalRotation(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].localRotation;
		}

		public Vector3 GetVelocity(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].velocity;
		}

		public Vector3 GetAngularVelocity(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].angularVelocity;
		}

		public bool GetDeviceIsConnected(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].deviceIsConnected;
		}

		public bool GetPoseIsValid(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].poseIsValid;
		}

		public ETrackingResult GetTrackingResult(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].trackingState;
		}

		public Vector3 GetLastLocalPosition(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastLocalPosition;
		}

		public Quaternion GetLastLocalRotation(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastLocalRotation;
		}

		public Vector3 GetLastVelocity(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastVelocity;
		}

		public Vector3 GetLastAngularVelocity(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastAngularVelocity;
		}

		public bool GetLastDeviceIsConnected(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastDeviceIsConnected;
		}

		public bool GetLastPoseIsValid(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastPoseIsValid;
		}

		public ETrackingResult GetLastTrackingResult(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].lastTrackingState;
		}
	}
}
