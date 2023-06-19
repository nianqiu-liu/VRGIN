using System;
using System.Collections.Generic;

namespace Valve.VR
{
    [Serializable]
    public class SteamVR_Input_BindingFile_Source
    {
        public string path;

        public string mode;

        public SteamVR_Input_BindingFile_Source_Input_StringDictionary parameters = new SteamVR_Input_BindingFile_Source_Input_StringDictionary();

        public SteamVR_Input_BindingFile_Source_Input inputs = new SteamVR_Input_BindingFile_Source_Input();

        protected const string outputKeyName = "output";

        public string GetOutput()
        {
            foreach (var input in inputs)
            {
                foreach (var item in input.Value)
                {
                    if (item.Key == "output") return item.Value;
                }
            }

            return null;
        }

        public override bool Equals(object obj)
        {
            if (obj is SteamVR_Input_BindingFile_Source)
            {
                var steamVR_Input_BindingFile_Source = (SteamVR_Input_BindingFile_Source)obj;
                if (steamVR_Input_BindingFile_Source.mode == mode && steamVR_Input_BindingFile_Source.path == path)
                {
                    var flag = false;
                    if (parameters != null && steamVR_Input_BindingFile_Source.parameters != null)
                    {
                        if (parameters.Equals(steamVR_Input_BindingFile_Source.parameters)) flag = true;
                    }
                    else if (parameters == null && steamVR_Input_BindingFile_Source.parameters == null) flag = true;

                    if (flag)
                    {
                        var result = false;
                        if (inputs != null && steamVR_Input_BindingFile_Source.inputs != null)
                        {
                            if (inputs.Equals(steamVR_Input_BindingFile_Source.inputs)) result = true;
                        }
                        else if (inputs == null && steamVR_Input_BindingFile_Source.inputs == null) result = true;

                        return result;
                    }
                }

                return false;
            }

            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
