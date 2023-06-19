using System;

namespace Valve.VR
{
	[Serializable]
	public abstract class SteamVR_Action_In<SourceMap, SourceElement> : SteamVR_Action<SourceMap, SourceElement>, ISteamVR_Action_In, ISteamVR_Action, ISteamVR_Action_Source, ISteamVR_Action_In_Source where SourceMap : SteamVR_Action_In_Source_Map<SourceElement>, new() where SourceElement : SteamVR_Action_In_Source, new()
	{
		public bool changed => sourceMap[SteamVR_Input_Sources.Any].changed;

		public bool lastChanged => sourceMap[SteamVR_Input_Sources.Any].changed;

		public float changedTime => sourceMap[SteamVR_Input_Sources.Any].changedTime;

		public float updateTime => sourceMap[SteamVR_Input_Sources.Any].updateTime;

		public ulong activeOrigin => sourceMap[SteamVR_Input_Sources.Any].activeOrigin;

		public ulong lastActiveOrigin => sourceMap[SteamVR_Input_Sources.Any].lastActiveOrigin;

		public SteamVR_Input_Sources activeDevice => sourceMap[SteamVR_Input_Sources.Any].activeDevice;

		public uint trackedDeviceIndex => sourceMap[SteamVR_Input_Sources.Any].trackedDeviceIndex;

		public string renderModelComponentName => sourceMap[SteamVR_Input_Sources.Any].renderModelComponentName;

		public string localizedOriginName => sourceMap[SteamVR_Input_Sources.Any].localizedOriginName;

		public virtual void UpdateValues()
		{
			sourceMap.UpdateValues();
		}

		public virtual string GetRenderModelComponentName(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].renderModelComponentName;
		}

		public virtual SteamVR_Input_Sources GetActiveDevice(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].activeDevice;
		}

		public virtual uint GetDeviceIndex(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].trackedDeviceIndex;
		}

		public virtual bool GetChanged(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].changed;
		}

		public override float GetTimeLastChanged(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].changedTime;
		}

		public string GetLocalizedOriginPart(SteamVR_Input_Sources inputSource, params EVRInputStringBits[] localizedParts)
		{
			return sourceMap[inputSource].GetLocalizedOriginPart(localizedParts);
		}

		public string GetLocalizedOrigin(SteamVR_Input_Sources inputSource)
		{
			return sourceMap[inputSource].GetLocalizedOrigin();
		}

		public override bool IsUpdating(SteamVR_Input_Sources inputSource)
		{
			return sourceMap.IsUpdating(inputSource);
		}

		public void ForceAddSourceToUpdateList(SteamVR_Input_Sources inputSource)
		{
			sourceMap.ForceAddSourceToUpdateList(inputSource);
		}

		public string GetControllerType(SteamVR_Input_Sources inputSource)
		{
			return SteamVR.instance.GetStringProperty(ETrackedDeviceProperty.Prop_ControllerType_String, GetDeviceIndex(inputSource));
		}
	}
}
