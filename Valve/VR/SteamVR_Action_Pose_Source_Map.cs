namespace Valve.VR
{
	public class SteamVR_Action_Pose_Source_Map<Source> : SteamVR_Action_In_Source_Map<Source> where Source : SteamVR_Action_Pose_Source, new()
	{
		public void SetTrackingUniverseOrigin(ETrackingUniverseOrigin newOrigin)
		{
			for (int i = 0; i < sources.Length; i++)
			{
				if (sources[i] != null)
				{
					sources[i].universeOrigin = newOrigin;
				}
			}
		}

		public virtual void UpdateValues(bool skipStateAndEventUpdates)
		{
			for (int i = 0; i < updatingSources.Count; i++)
			{
				sources[updatingSources[i]].UpdateValue(skipStateAndEventUpdates);
			}
		}
	}
}
