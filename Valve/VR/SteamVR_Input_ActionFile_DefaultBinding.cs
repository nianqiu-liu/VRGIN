using System;

namespace Valve.VR
{
	[Serializable]
	public class SteamVR_Input_ActionFile_DefaultBinding
	{
		public string controller_type;

		public string binding_url;

		public SteamVR_Input_ActionFile_DefaultBinding GetCopy()
		{
			return new SteamVR_Input_ActionFile_DefaultBinding
			{
				controller_type = controller_type,
				binding_url = binding_url
			};
		}
	}
}
