using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using Valve.Newtonsoft.Json;

namespace Valve.VR
{
	public class SteamVR_Input
	{
		public delegate void PosesUpdatedHandler(bool skipSendingEvents);

		public delegate void SkeletonsUpdatedHandler(bool skipSendingEvents);

		public const string defaultInputGameObjectName = "[SteamVR Input]";

		private const string localizationKeyName = "localization";

		public static bool fileInitialized;

		public static bool initialized;

		public static bool preInitialized;

		public static SteamVR_Input_ActionFile actionFile;

		public static string actionFileHash;

		protected static bool initializing;

		protected static int startupFrame;

		public static SteamVR_ActionSet[] actionSets;

		public static SteamVR_Action[] actions;

		public static ISteamVR_Action_In[] actionsIn;

		public static ISteamVR_Action_Out[] actionsOut;

		public static SteamVR_Action_Boolean[] actionsBoolean;

		public static SteamVR_Action_Single[] actionsSingle;

		public static SteamVR_Action_Vector2[] actionsVector2;

		public static SteamVR_Action_Vector3[] actionsVector3;

		public static SteamVR_Action_Pose[] actionsPose;

		public static SteamVR_Action_Skeleton[] actionsSkeleton;

		public static SteamVR_Action_Vibration[] actionsVibration;

		public static ISteamVR_Action_In[] actionsNonPoseNonSkeletonIn;

		protected static Dictionary<string, SteamVR_ActionSet> actionSetsByPath;

		protected static Dictionary<string, SteamVR_ActionSet> actionSetsByPathLowered;

		protected static Dictionary<string, SteamVR_Action> actionsByPath;

		protected static Dictionary<string, SteamVR_Action> actionsByPathLowered;

		protected static Dictionary<string, SteamVR_ActionSet> actionSetsByPathCache;

		protected static Dictionary<string, SteamVR_Action> actionsByPathCache;

		protected static Dictionary<string, SteamVR_Action> actionsByNameCache;

		protected static Dictionary<string, SteamVR_ActionSet> actionSetsByNameCache;

		private static uint sizeVRActiveActionSet_t;

		private static VRActiveActionSet_t[] setCache;

		public static bool isStartupFrame
		{
			get
			{
				if (Time.frameCount >= startupFrame - 1)
				{
					return Time.frameCount <= startupFrame + 1;
				}
				return false;
			}
		}

		public static event Action onNonVisualActionsUpdated;

		public static event PosesUpdatedHandler onPosesUpdated;

		public static event SkeletonsUpdatedHandler onSkeletonsUpdated;

		static SteamVR_Input()
		{
			fileInitialized = false;
			initialized = false;
			preInitialized = false;
			initializing = false;
			startupFrame = 0;
			actionSetsByPath = new Dictionary<string, SteamVR_ActionSet>();
			actionSetsByPathLowered = new Dictionary<string, SteamVR_ActionSet>();
			actionsByPath = new Dictionary<string, SteamVR_Action>();
			actionsByPathLowered = new Dictionary<string, SteamVR_Action>();
			actionSetsByPathCache = new Dictionary<string, SteamVR_ActionSet>();
			actionsByPathCache = new Dictionary<string, SteamVR_Action>();
			actionsByNameCache = new Dictionary<string, SteamVR_Action>();
			actionSetsByNameCache = new Dictionary<string, SteamVR_ActionSet>();
			sizeVRActiveActionSet_t = 0u;
			setCache = new VRActiveActionSet_t[1];
			FindPreinitializeMethod();
		}

		public static void ForcePreinitialize()
		{
			FindPreinitializeMethod();
		}

		private static void FindPreinitializeMethod()
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type type = assemblies[i].GetType("Valve.VR.SteamVR_Actions");
				if (type != null)
				{
					MethodInfo method = type.GetMethod("PreInitialize");
					if (method != null)
					{
						method.Invoke(null, null);
						break;
					}
				}
			}
		}

		public static void Initialize(bool force = false)
		{
			if (initialized && !force)
			{
				return;
			}
			initializing = true;
			startupFrame = Time.frameCount;
			SteamVR_ActionSet_Manager.Initialize();
			SteamVR_Input_Source.Initialize();
			for (int i = 0; i < actions.Length; i++)
			{
				actions[i].Initialize(true);
			}
			for (int j = 0; j < actionSets.Length; j++)
			{
				actionSets[j].Initialize(true);
			}
			if (SteamVR_Settings.instance.activateFirstActionSetOnStart)
			{
				if (actionSets.Length != 0)
				{
					actionSets[0].Activate(SteamVR_Input_Sources.Any, 0, false);
				}
				else
				{
					Debug.LogError("<b>[SteamVR]</b> No action sets to activate.");
				}
			}
			SteamVR_Action_Pose.SetTrackingUniverseOrigin(SteamVR_Settings.instance.trackingSpace);
			initialized = true;
			initializing = false;
		}

		public static void PreinitializeFinishActionSets()
		{
			for (int i = 0; i < actionSets.Length; i++)
			{
				actionSets[i].FinishPreInitialize();
			}
		}

		public static void PreinitializeActionSetDictionaries()
		{
			actionSetsByPath.Clear();
			actionSetsByPathLowered.Clear();
			actionSetsByPathCache.Clear();
			for (int i = 0; i < actionSets.Length; i++)
			{
				SteamVR_ActionSet steamVR_ActionSet = actionSets[i];
				actionSetsByPath.Add(steamVR_ActionSet.fullPath, steamVR_ActionSet);
				actionSetsByPathLowered.Add(steamVR_ActionSet.fullPath.ToLower(), steamVR_ActionSet);
			}
		}

		public static void PreinitializeActionDictionaries()
		{
			actionsByPath.Clear();
			actionsByPathLowered.Clear();
			actionsByPathCache.Clear();
			for (int i = 0; i < actions.Length; i++)
			{
				SteamVR_Action steamVR_Action = actions[i];
				actionsByPath.Add(steamVR_Action.fullPath, steamVR_Action);
				actionsByPathLowered.Add(steamVR_Action.fullPath.ToLower(), steamVR_Action);
			}
		}

		public static void Update()
		{
			if (initialized && !isStartupFrame)
			{
				if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnUpdate))
				{
					UpdateNonVisualActions();
				}
				if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnUpdate))
				{
					UpdateVisualActions(false);
				}
			}
		}

		public static void LateUpdate()
		{
			if (initialized && !isStartupFrame)
			{
				if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnLateUpdate))
				{
					UpdateNonVisualActions();
				}
				if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnLateUpdate))
				{
					UpdateVisualActions(false);
				}
				else
				{
					UpdateSkeletonActions(true);
				}
			}
		}

		public static void FixedUpdate()
		{
			if (initialized && !isStartupFrame)
			{
				if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnFixedUpdate))
				{
					UpdateNonVisualActions();
				}
				if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnFixedUpdate))
				{
					UpdateVisualActions(false);
				}
			}
		}

		public static void OnPreCull()
		{
			if (initialized && !isStartupFrame)
			{
				if (SteamVR.settings.IsInputUpdateMode(SteamVR_UpdateModes.OnPreCull))
				{
					UpdateNonVisualActions();
				}
				if (SteamVR.settings.IsPoseUpdateMode(SteamVR_UpdateModes.OnPreCull))
				{
					UpdateVisualActions(false);
				}
			}
		}

		public static void UpdateVisualActions(bool skipStateAndEventUpdates = false)
		{
			if (initialized)
			{
				SteamVR_ActionSet_Manager.UpdateActionStates(false);
				UpdatePoseActions(skipStateAndEventUpdates);
				UpdateSkeletonActions(skipStateAndEventUpdates);
			}
		}

		public static void UpdatePoseActions(bool skipSendingEvents = false)
		{
			if (initialized)
			{
				for (int i = 0; i < actionsPose.Length; i++)
				{
					actionsPose[i].UpdateValues(skipSendingEvents);
				}
				if (SteamVR_Input.onPosesUpdated != null)
				{
					SteamVR_Input.onPosesUpdated(false);
				}
			}
		}

		public static void UpdateSkeletonActions(bool skipSendingEvents = false)
		{
			if (initialized)
			{
				for (int i = 0; i < actionsSkeleton.Length; i++)
				{
					actionsSkeleton[i].UpdateValue(skipSendingEvents);
				}
				if (SteamVR_Input.onSkeletonsUpdated != null)
				{
					SteamVR_Input.onSkeletonsUpdated(skipSendingEvents);
				}
			}
		}

		public static void UpdateNonVisualActions()
		{
			if (initialized)
			{
				SteamVR_ActionSet_Manager.UpdateActionStates(false);
				for (int i = 0; i < actionsNonPoseNonSkeletonIn.Length; i++)
				{
					actionsNonPoseNonSkeletonIn[i].UpdateValues();
				}
				if (SteamVR_Input.onNonVisualActionsUpdated != null)
				{
					SteamVR_Input.onNonVisualActionsUpdated();
				}
			}
		}

		protected static void ShowBindingHintsForSets(VRActiveActionSet_t[] sets, ulong highlightAction = 0uL)
		{
			if (sizeVRActiveActionSet_t == 0)
			{
				sizeVRActiveActionSet_t = (uint)Marshal.SizeOf(typeof(VRActiveActionSet_t));
			}
			OpenVR.Input.ShowBindingsForActionSet(sets, sizeVRActiveActionSet_t, highlightAction);
		}

		public static bool ShowBindingHints(ISteamVR_Action_In originToHighlight)
		{
			if (originToHighlight != null)
			{
				setCache[0].ulActionSet = originToHighlight.actionSet.handle;
				ShowBindingHintsForSets(setCache, originToHighlight.activeOrigin);
				return true;
			}
			return false;
		}

		public static bool ShowBindingHints(ISteamVR_ActionSet setToShow)
		{
			if (setToShow != null)
			{
				setCache[0].ulActionSet = setToShow.handle;
				ShowBindingHintsForSets(setCache, 0uL);
				return true;
			}
			return false;
		}

		public static void ShowBindingHintsForActiveActionSets(ulong highlightAction = 0uL)
		{
			if (sizeVRActiveActionSet_t == 0)
			{
				sizeVRActiveActionSet_t = (uint)Marshal.SizeOf(typeof(VRActiveActionSet_t));
			}
			OpenVR.Input.ShowBindingsForActionSet(SteamVR_ActionSet_Manager.rawActiveActionSetArray, sizeVRActiveActionSet_t, highlightAction);
		}

		public static T GetActionDataFromPath<T>(string path, bool caseSensitive = false) where T : SteamVR_Action_Source_Map
		{
			SteamVR_Action baseActionFromPath = GetBaseActionFromPath(path, caseSensitive);
			if (baseActionFromPath != null)
			{
				return (T)baseActionFromPath.GetSourceMap();
			}
			return null;
		}

		public static SteamVR_ActionSet_Data GetActionSetDataFromPath(string path, bool caseSensitive = false)
		{
			SteamVR_ActionSet actionSetFromPath = GetActionSetFromPath(path, caseSensitive);
			if (actionSetFromPath != null)
			{
				return actionSetFromPath.GetActionSetData();
			}
			return null;
		}

		public static T GetActionFromPath<T>(string path, bool caseSensitive = false, bool returnNulls = false) where T : SteamVR_Action, new()
		{
			SteamVR_Action baseActionFromPath = GetBaseActionFromPath(path, caseSensitive);
			if (baseActionFromPath != null)
			{
				return baseActionFromPath.GetCopy<T>();
			}
			if (returnNulls)
			{
				return null;
			}
			return CreateFakeAction<T>(path, caseSensitive);
		}

		public static SteamVR_Action GetBaseActionFromPath(string path, bool caseSensitive = false)
		{
			if (string.IsNullOrEmpty(path))
			{
				return null;
			}
			if (caseSensitive)
			{
				if (actionsByPath.ContainsKey(path))
				{
					return actionsByPath[path];
				}
			}
			else
			{
				if (actionsByPathCache.ContainsKey(path))
				{
					return actionsByPathCache[path];
				}
				if (actionsByPath.ContainsKey(path))
				{
					actionsByPathCache.Add(path, actionsByPath[path]);
					return actionsByPath[path];
				}
				string key = path.ToLower();
				if (actionsByPathLowered.ContainsKey(key))
				{
					actionsByPathCache.Add(path, actionsByPathLowered[key]);
					return actionsByPathLowered[key];
				}
				actionsByPathCache.Add(path, null);
			}
			return null;
		}

		public static bool HasActionPath(string path, bool caseSensitive = false)
		{
			return GetBaseActionFromPath(path, caseSensitive) != null;
		}

		public static bool HasAction(string actionName, bool caseSensitive = false)
		{
			return GetBaseAction(null, actionName, caseSensitive) != null;
		}

		public static bool HasAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetBaseAction(actionSetName, actionName, caseSensitive) != null;
		}

		public static SteamVR_Action_Boolean GetBooleanActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Boolean>(path, caseSensitive);
		}

		public static SteamVR_Action_Single GetSingleActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Single>(path, caseSensitive);
		}

		public static SteamVR_Action_Vector2 GetVector2ActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Vector2>(path, caseSensitive);
		}

		public static SteamVR_Action_Vector3 GetVector3ActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Vector3>(path, caseSensitive);
		}

		public static SteamVR_Action_Vibration GetVibrationActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Vibration>(path, caseSensitive);
		}

		public static SteamVR_Action_Pose GetPoseActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Pose>(path, caseSensitive);
		}

		public static SteamVR_Action_Skeleton GetSkeletonActionFromPath(string path, bool caseSensitive = false)
		{
			return GetActionFromPath<SteamVR_Action_Skeleton>(path, caseSensitive);
		}

		public static T GetAction<T>(string actionSetName, string actionName, bool caseSensitive = false, bool returnNulls = false) where T : SteamVR_Action, new()
		{
			SteamVR_Action baseAction = GetBaseAction(actionSetName, actionName, caseSensitive);
			if (baseAction != null)
			{
				return baseAction.GetCopy<T>();
			}
			if (returnNulls)
			{
				return null;
			}
			return CreateFakeAction<T>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action GetBaseAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			if (actions == null)
			{
				return null;
			}
			if (string.IsNullOrEmpty(actionSetName))
			{
				for (int i = 0; i < actions.Length; i++)
				{
					if (caseSensitive)
					{
						if (actions[i].GetShortName() == actionName)
						{
							return actions[i];
						}
					}
					else if (string.Equals(actions[i].GetShortName(), actionName, StringComparison.CurrentCultureIgnoreCase))
					{
						return actions[i];
					}
				}
			}
			else
			{
				SteamVR_ActionSet actionSet = GetActionSet(actionSetName, caseSensitive, true);
				if (actionSet != null)
				{
					for (int j = 0; j < actionSet.allActions.Length; j++)
					{
						if (caseSensitive)
						{
							if (actionSet.allActions[j].GetShortName() == actionName)
							{
								return actionSet.allActions[j];
							}
						}
						else if (string.Equals(actionSet.allActions[j].GetShortName(), actionName, StringComparison.CurrentCultureIgnoreCase))
						{
							return actionSet.allActions[j];
						}
					}
				}
			}
			return null;
		}

		private static T CreateFakeAction<T>(string actionSetName, string actionName, bool caseSensitive) where T : SteamVR_Action, new()
		{
			if (typeof(T) == typeof(SteamVR_Action_Vibration))
			{
				return SteamVR_Action.CreateUninitialized<T>(actionSetName, SteamVR_ActionDirections.Out, actionName, caseSensitive);
			}
			return SteamVR_Action.CreateUninitialized<T>(actionSetName, SteamVR_ActionDirections.In, actionName, caseSensitive);
		}

		private static T CreateFakeAction<T>(string actionPath, bool caseSensitive) where T : SteamVR_Action, new()
		{
			return SteamVR_Action.CreateUninitialized<T>(actionPath, caseSensitive);
		}

		public static T GetAction<T>(string actionName, bool caseSensitive = false) where T : SteamVR_Action, new()
		{
			return GetAction<T>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Boolean GetBooleanAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Boolean>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Boolean GetBooleanAction(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Boolean>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Single GetSingleAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Single>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Single GetSingleAction(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Single>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Vector2 GetVector2Action(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Vector2>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Vector2 GetVector2Action(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Vector2>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Vector3 GetVector3Action(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Vector3>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Vector3 GetVector3Action(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Vector3>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Pose GetPoseAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Pose>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Pose GetPoseAction(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Pose>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Skeleton GetSkeletonAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Skeleton>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Skeleton GetSkeletonAction(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Skeleton>(null, actionName, caseSensitive);
		}

		public static SteamVR_Action_Vibration GetVibrationAction(string actionSetName, string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Vibration>(actionSetName, actionName, caseSensitive);
		}

		public static SteamVR_Action_Vibration GetVibrationAction(string actionName, bool caseSensitive = false)
		{
			return GetAction<SteamVR_Action_Vibration>(null, actionName, caseSensitive);
		}

		public static T GetActionSet<T>(string actionSetName, bool caseSensitive = false, bool returnNulls = false) where T : SteamVR_ActionSet, new()
		{
			if (actionSets == null)
			{
				if (returnNulls)
				{
					return null;
				}
				return SteamVR_ActionSet.CreateFromName<T>(actionSetName);
			}
			for (int i = 0; i < actionSets.Length; i++)
			{
				if (caseSensitive)
				{
					if (actionSets[i].GetShortName() == actionSetName)
					{
						return actionSets[i].GetCopy<T>();
					}
				}
				else if (string.Equals(actionSets[i].GetShortName(), actionSetName, StringComparison.CurrentCultureIgnoreCase))
				{
					return actionSets[i].GetCopy<T>();
				}
			}
			if (returnNulls)
			{
				return null;
			}
			return SteamVR_ActionSet.CreateFromName<T>(actionSetName);
		}

		public static SteamVR_ActionSet GetActionSet(string actionSetName, bool caseSensitive = false, bool returnsNulls = false)
		{
			return GetActionSet<SteamVR_ActionSet>(actionSetName, caseSensitive, returnsNulls);
		}

		protected static bool HasActionSet(string name, bool caseSensitive = false)
		{
			return GetActionSet(name, caseSensitive, true) != null;
		}

		public static T GetActionSetFromPath<T>(string path, bool caseSensitive = false, bool returnsNulls = false) where T : SteamVR_ActionSet, new()
		{
			if (actionSets == null || actionSets[0] == null || string.IsNullOrEmpty(path))
			{
				if (returnsNulls)
				{
					return null;
				}
				return SteamVR_ActionSet.Create<T>(path);
			}
			if (caseSensitive)
			{
				if (actionSetsByPath.ContainsKey(path))
				{
					return actionSetsByPath[path].GetCopy<T>();
				}
			}
			else
			{
				if (actionSetsByPathCache.ContainsKey(path))
				{
					SteamVR_ActionSet steamVR_ActionSet = actionSetsByPathCache[path];
					if (steamVR_ActionSet == null)
					{
						return null;
					}
					return steamVR_ActionSet.GetCopy<T>();
				}
				if (actionSetsByPath.ContainsKey(path))
				{
					actionSetsByPathCache.Add(path, actionSetsByPath[path]);
					return actionSetsByPath[path].GetCopy<T>();
				}
				string key = path.ToLower();
				if (actionSetsByPathLowered.ContainsKey(key))
				{
					actionSetsByPathCache.Add(path, actionSetsByPathLowered[key]);
					return actionSetsByPath[key].GetCopy<T>();
				}
				actionSetsByPathCache.Add(path, null);
			}
			if (returnsNulls)
			{
				return null;
			}
			return SteamVR_ActionSet.Create<T>(path);
		}

		public static SteamVR_ActionSet GetActionSetFromPath(string path, bool caseSensitive = false)
		{
			return GetActionSetFromPath<SteamVR_ActionSet>(path, caseSensitive);
		}

		public static bool GetState(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Boolean action2 = GetAction<SteamVR_Action_Boolean>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetState(inputSource);
			}
			return false;
		}

		public static bool GetState(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetState(null, action, inputSource, caseSensitive);
		}

		public static bool GetStateDown(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Boolean action2 = GetAction<SteamVR_Action_Boolean>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetStateDown(inputSource);
			}
			return false;
		}

		public static bool GetStateDown(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetStateDown(null, action, inputSource, caseSensitive);
		}

		public static bool GetStateUp(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Boolean action2 = GetAction<SteamVR_Action_Boolean>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetStateUp(inputSource);
			}
			return false;
		}

		public static bool GetStateUp(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetStateUp(null, action, inputSource, caseSensitive);
		}

		public static float GetFloat(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Single action2 = GetAction<SteamVR_Action_Single>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetAxis(inputSource);
			}
			return 0f;
		}

		public static float GetFloat(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetFloat(null, action, inputSource, caseSensitive);
		}

		public static float GetSingle(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Single action2 = GetAction<SteamVR_Action_Single>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetAxis(inputSource);
			}
			return 0f;
		}

		public static float GetSingle(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetFloat(null, action, inputSource, caseSensitive);
		}

		public static Vector2 GetVector2(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Vector2 action2 = GetAction<SteamVR_Action_Vector2>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetAxis(inputSource);
			}
			return Vector2.zero;
		}

		public static Vector2 GetVector2(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetVector2(null, action, inputSource, caseSensitive);
		}

		public static Vector3 GetVector3(string actionSet, string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			SteamVR_Action_Vector3 action2 = GetAction<SteamVR_Action_Vector3>(actionSet, action, caseSensitive);
			if (action2 != null)
			{
				return action2.GetAxis(inputSource);
			}
			return Vector3.zero;
		}

		public static Vector3 GetVector3(string action, SteamVR_Input_Sources inputSource, bool caseSensitive = false)
		{
			return GetVector3(null, action, inputSource, caseSensitive);
		}

		public static SteamVR_ActionSet[] GetActionSets()
		{
			return actionSets;
		}

		public static T[] GetActions<T>() where T : SteamVR_Action
		{
			Type typeFromHandle = typeof(T);
			if (typeFromHandle == typeof(SteamVR_Action))
			{
				return actions as T[];
			}
			if (typeFromHandle == typeof(ISteamVR_Action_In))
			{
				return actionsIn as T[];
			}
			if (typeFromHandle == typeof(ISteamVR_Action_Out))
			{
				return actionsOut as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Boolean))
			{
				return actionsBoolean as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Single))
			{
				return actionsSingle as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Vector2))
			{
				return actionsVector2 as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Vector3))
			{
				return actionsVector3 as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Pose))
			{
				return actionsPose as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Skeleton))
			{
				return actionsSkeleton as T[];
			}
			if (typeFromHandle == typeof(SteamVR_Action_Vibration))
			{
				return actionsVibration as T[];
			}
			Debug.Log("<b>[SteamVR]</b> Wrong type.");
			return null;
		}

		internal static bool ShouldMakeCopy()
		{
			return !SteamVR_Behaviour.isPlaying;
		}

		public static string GetLocalizedName(ulong originHandle, params EVRInputStringBits[] localizedParts)
		{
			int num = 0;
			for (int i = 0; i < localizedParts.Length; i++)
			{
				num |= (int)localizedParts[i];
			}
			StringBuilder stringBuilder = new StringBuilder(500);
			OpenVR.Input.GetOriginLocalizedName(originHandle, stringBuilder, 500u, num);
			return stringBuilder.ToString();
		}

		public static bool CheckOldLocation()
		{
			return false;
		}

		public static void IdentifyActionsFile(bool showLogs = true)
		{
			string actionsFilePath = GetActionsFilePath(true);
			if (File.Exists(actionsFilePath))
			{
				if (OpenVR.Input == null)
				{
					Debug.LogError("<b>[SteamVR]</b> Could not instantiate OpenVR Input interface.");
					return;
				}
				EVRInputError eVRInputError = OpenVR.Input.SetActionManifestPath(actionsFilePath);
				if (eVRInputError != 0)
				{
					Debug.LogError("<b>[SteamVR]</b> Error loading action manifest into SteamVR: " + eVRInputError);
					return;
				}
				int num = 0;
				if (actions != null)
				{
					num = actions.Length;
					if (showLogs)
					{
						Debug.Log($"<b>[SteamVR]</b> Successfully loaded {num} actions from action manifest into SteamVR ({actionsFilePath})");
					}
				}
				else if (showLogs)
				{
					Debug.LogWarning("<b>[SteamVR]</b> No actions found, but the action manifest was loaded. This usually means you haven't generated actions. Window -> SteamVR Input -> Save and Generate.");
				}
			}
			else if (showLogs)
			{
				Debug.LogError("<b>[SteamVR]</b> Could not find actions file at: " + actionsFilePath);
			}
		}

		public static bool HasFileInMemoryBeenModified()
		{
			string actionsFilePath = GetActionsFilePath(true);
			string text = null;
			if (File.Exists(actionsFilePath))
			{
				text = File.ReadAllText(actionsFilePath);
				string badMD5Hash = SteamVR_Utils.GetBadMD5Hash(text);
				string badMD5Hash2 = SteamVR_Utils.GetBadMD5Hash(JsonConvert.SerializeObject(actionFile, Formatting.Indented, new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore
				}));
				return badMD5Hash != badMD5Hash2;
			}
			return true;
		}

		public static bool CreateEmptyActionsFile(bool completelyEmpty = false)
		{
			string actionsFilePath = GetActionsFilePath(true);
			if (File.Exists(actionsFilePath))
			{
				Debug.LogErrorFormat("<b>[SteamVR]</b> Actions file already exists in project root: {0}", actionsFilePath);
				return false;
			}
			actionFile = new SteamVR_Input_ActionFile();
			if (!completelyEmpty)
			{
				actionFile.action_sets.Add(SteamVR_Input_ActionFile_ActionSet.CreateNew());
				actionFile.actions.Add(SteamVR_Input_ActionFile_Action.CreateNew(actionFile.action_sets[0].shortName, SteamVR_ActionDirections.In, SteamVR_Input_ActionFile_ActionTypes.boolean));
			}
			string contents = JsonConvert.SerializeObject(actionFile, Formatting.Indented, new JsonSerializerSettings
			{
				NullValueHandling = NullValueHandling.Ignore
			});
			File.WriteAllText(actionsFilePath, contents);
			actionFile.InitializeHelperLists();
			fileInitialized = true;
			return true;
		}

		public static bool DoesActionsFileExist()
		{
			return File.Exists(GetActionsFilePath(true));
		}

		public static bool InitializeFile(bool force = false, bool showErrors = true)
		{
			bool num = DoesActionsFileExist();
			string actionsFilePath = GetActionsFilePath(true);
			string text = null;
			if (num)
			{
				text = File.ReadAllText(actionsFilePath);
				if (fileInitialized || (fileInitialized && !force))
				{
					string badMD5Hash = SteamVR_Utils.GetBadMD5Hash(text);
					if (badMD5Hash == actionFileHash)
					{
						return true;
					}
					actionFileHash = badMD5Hash;
				}
				actionFile = SteamVR_Input_ActionFile.Open(GetActionsFilePath(true));
				fileInitialized = true;
				return true;
			}
			if (showErrors)
			{
				Debug.LogErrorFormat("<b>[SteamVR]</b> Actions file does not exist in project root: {0}", actionsFilePath);
			}
			return false;
		}

		public static string GetActionsFileFolder(bool fullPath = true)
		{
			string streamingAssetsPath = Application.streamingAssetsPath;
			if (!Directory.Exists(streamingAssetsPath))
			{
				Directory.CreateDirectory(streamingAssetsPath);
			}
			string text = Path.Combine(streamingAssetsPath, "SteamVR");
			if (!Directory.Exists(text))
			{
				Directory.CreateDirectory(text);
			}
			return text;
		}

		public static string GetActionsFilePath(bool fullPath = true)
		{
			return SteamVR_Utils.SanitizePath(Path.Combine(GetActionsFileFolder(fullPath), SteamVR_Settings.instance.actionsFilePath));
		}

		public static string GetActionsFileName()
		{
			return SteamVR_Settings.instance.actionsFilePath;
		}

		public static bool DeleteManifestAndBindings()
		{
			if (!DoesActionsFileExist())
			{
				return false;
			}
			InitializeFile(false, true);
			string[] filesToCopy = actionFile.GetFilesToCopy(false);
			foreach (string obj in filesToCopy)
			{
				new FileInfo(obj).IsReadOnly = false;
				File.Delete(obj);
			}
			string actionsFilePath = GetActionsFilePath(true);
			if (File.Exists(actionsFilePath))
			{
				new FileInfo(actionsFilePath).IsReadOnly = false;
				File.Delete(actionsFilePath);
				actionFile = null;
				fileInitialized = false;
				return true;
			}
			return false;
		}

		public static void OpenBindingUI(SteamVR_ActionSet actionSetToEdit = null, SteamVR_Input_Sources deviceBindingToEdit = SteamVR_Input_Sources.Any)
		{
			ulong handle = SteamVR_Input_Source.GetHandle(deviceBindingToEdit);
			ulong ulActionSetHandle = 0uL;
			if (actionSetToEdit != null)
			{
				ulActionSetHandle = actionSetToEdit.handle;
			}
			OpenVR.Input.OpenBindingUI(null, ulActionSetHandle, handle, false);
		}
	}
}
