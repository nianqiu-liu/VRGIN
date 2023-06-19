using System;
using System.Collections.Generic;
using Valve.Newtonsoft.Json;

namespace Valve.VR
{
	[Serializable]
	public class SteamVR_Input_ActionFile_ActionSet
	{
		[JsonIgnore]
		private const string actionSetInstancePrefix = "instance_";

		public string name;

		public string usage;

		private const string nameTemplate = "/actions/{0}";

		[JsonIgnore]
		public List<SteamVR_Input_ActionFile_Action> actionsInList = new List<SteamVR_Input_ActionFile_Action>();

		[JsonIgnore]
		public List<SteamVR_Input_ActionFile_Action> actionsOutList = new List<SteamVR_Input_ActionFile_Action>();

		[JsonIgnore]
		public List<SteamVR_Input_ActionFile_Action> actionsList = new List<SteamVR_Input_ActionFile_Action>();

		[JsonIgnore]
		public string codeFriendlyName => SteamVR_Input_ActionFile.GetCodeFriendlyName(name);

		[JsonIgnore]
		public string shortName
		{
			get
			{
				if (name.LastIndexOf('/') == name.Length - 1)
				{
					return string.Empty;
				}
				return SteamVR_Input_ActionFile.GetShortName(name);
			}
		}

		public void SetNewShortName(string newShortName)
		{
			name = GetPathFromName(newShortName);
		}

		public static string CreateNewName()
		{
			return GetPathFromName("NewSet");
		}

		public static string GetPathFromName(string name)
		{
			return $"/actions/{name}";
		}

		public static SteamVR_Input_ActionFile_ActionSet CreateNew()
		{
			return new SteamVR_Input_ActionFile_ActionSet
			{
				name = CreateNewName()
			};
		}

		public SteamVR_Input_ActionFile_ActionSet GetCopy()
		{
			return new SteamVR_Input_ActionFile_ActionSet
			{
				name = name,
				usage = usage
			};
		}

		public override bool Equals(object obj)
		{
			if (obj is SteamVR_Input_ActionFile_ActionSet)
			{
				SteamVR_Input_ActionFile_ActionSet steamVR_Input_ActionFile_ActionSet = (SteamVR_Input_ActionFile_ActionSet)obj;
				if (steamVR_Input_ActionFile_ActionSet == this)
				{
					return true;
				}
				if (steamVR_Input_ActionFile_ActionSet.name == name)
				{
					return true;
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
