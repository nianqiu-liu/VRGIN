using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Valve.VR
{
	public static class SteamVR_Input_Source
	{
		public static int numSources = Enum.GetValues(typeof(SteamVR_Input_Sources)).Length;

		private static ulong[] inputSourceHandlesBySource;

		private static Dictionary<ulong, SteamVR_Input_Sources> inputSourceSourcesByHandle = new Dictionary<ulong, SteamVR_Input_Sources>();

		private static Type enumType = typeof(SteamVR_Input_Sources);

		private static Type descriptionType = typeof(DescriptionAttribute);

		private static SteamVR_Input_Sources[] allSources;

		public static ulong GetHandle(SteamVR_Input_Sources inputSource)
		{
			if ((int)inputSource < inputSourceHandlesBySource.Length)
			{
				return inputSourceHandlesBySource[(int)inputSource];
			}
			return 0uL;
		}

		public static SteamVR_Input_Sources GetSource(ulong handle)
		{
			if (inputSourceSourcesByHandle.ContainsKey(handle))
			{
				return inputSourceSourcesByHandle[handle];
			}
			return SteamVR_Input_Sources.Any;
		}

		public static SteamVR_Input_Sources[] GetAllSources()
		{
			if (allSources == null)
			{
				allSources = (SteamVR_Input_Sources[])Enum.GetValues(typeof(SteamVR_Input_Sources));
			}
			return allSources;
		}

		private static string GetPath(string inputSourceEnumName)
		{
			return ((DescriptionAttribute)enumType.GetMember(inputSourceEnumName)[0].GetCustomAttributes(descriptionType, false)[0]).Description;
		}

		public static void Initialize()
		{
			List<SteamVR_Input_Sources> list = new List<SteamVR_Input_Sources>();
			string[] names = Enum.GetNames(enumType);
			inputSourceHandlesBySource = new ulong[names.Length];
			inputSourceSourcesByHandle = new Dictionary<ulong, SteamVR_Input_Sources>();
			for (int i = 0; i < names.Length; i++)
			{
				string path = GetPath(names[i]);
				ulong pHandle = 0uL;
				EVRInputError inputSourceHandle = OpenVR.Input.GetInputSourceHandle(path, ref pHandle);
				if (inputSourceHandle != 0)
				{
					Debug.LogError("<b>[SteamVR]</b> GetInputSourceHandle (" + path + ") error: " + inputSourceHandle);
				}
				if (names[i] == SteamVR_Input_Sources.Any.ToString())
				{
					inputSourceHandlesBySource[i] = 0uL;
					inputSourceSourcesByHandle.Add(0uL, (SteamVR_Input_Sources)i);
				}
				else
				{
					inputSourceHandlesBySource[i] = pHandle;
					inputSourceSourcesByHandle.Add(pHandle, (SteamVR_Input_Sources)i);
				}
				list.Add((SteamVR_Input_Sources)i);
			}
			allSources = list.ToArray();
		}
	}
}
