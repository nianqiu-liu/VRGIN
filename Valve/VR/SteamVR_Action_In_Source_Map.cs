using System.Collections.Generic;

namespace Valve.VR
{
    public class SteamVR_Action_In_Source_Map<SourceElement> : SteamVR_Action_Source_Map<SourceElement> where SourceElement : SteamVR_Action_In_Source, new()
    {
        protected List<int> updatingSources = new List<int>();

        public bool IsUpdating(SteamVR_Input_Sources inputSource)
        {
            for (var i = 0; i < updatingSources.Count; i++)
            {
                if (inputSource == (SteamVR_Input_Sources)updatingSources[i]) return true;
            }

            return false;
        }

        protected override void OnAccessSource(SteamVR_Input_Sources inputSource)
        {
            if (SteamVR_Action.startUpdatingSourceOnAccess) ForceAddSourceToUpdateList(inputSource);
        }

        public void ForceAddSourceToUpdateList(SteamVR_Input_Sources inputSource)
        {
            if (sources[(int)inputSource] == null) sources[(int)inputSource] = new SourceElement();
            if (!sources[(int)inputSource].isUpdating)
            {
                updatingSources.Add((int)inputSource);
                sources[(int)inputSource].isUpdating = true;
                if (!SteamVR_Input.isStartupFrame) sources[(int)inputSource].UpdateValue();
            }
        }

        public void UpdateValues()
        {
            for (var i = 0; i < updatingSources.Count; i++) sources[updatingSources[i]].UpdateValue();
        }
    }
}
