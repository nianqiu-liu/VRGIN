using System;
using UnityEngine;

namespace Valve.VR
{
	[Serializable]
	public class SteamVR_ActionSet : IEquatable<SteamVR_ActionSet>, ISteamVR_ActionSet, ISerializationCallbackReceiver
	{
		[SerializeField]
		private string actionSetPath;

		[NonSerialized]
		protected SteamVR_ActionSet_Data setData;

		[NonSerialized]
		protected bool initialized;

		public SteamVR_Action[] allActions
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.allActions;
			}
		}

		public ISteamVR_Action_In[] nonVisualInActions
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.nonVisualInActions;
			}
		}

		public ISteamVR_Action_In[] visualActions
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.visualActions;
			}
		}

		public SteamVR_Action_Pose[] poseActions
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.poseActions;
			}
		}

		public SteamVR_Action_Skeleton[] skeletonActions
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.skeletonActions;
			}
		}

		public ISteamVR_Action_Out[] outActionArray
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.outActionArray;
			}
		}

		public string fullPath
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.fullPath;
			}
		}

		public string usage
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.usage;
			}
		}

		public ulong handle
		{
			get
			{
				if (!initialized)
				{
					Initialize(false, true);
				}
				return setData.handle;
			}
		}

		public static CreateType Create<CreateType>(string newSetPath) where CreateType : SteamVR_ActionSet, new()
		{
			CreateType val = new CreateType();
			val.PreInitialize(newSetPath);
			return val;
		}

		public static CreateType CreateFromName<CreateType>(string newSetName) where CreateType : SteamVR_ActionSet, new()
		{
			CreateType val = new CreateType();
			val.PreInitialize(SteamVR_Input_ActionFile_ActionSet.GetPathFromName(newSetName));
			return val;
		}

		public void PreInitialize(string newActionPath)
		{
			actionSetPath = newActionPath;
			setData = new SteamVR_ActionSet_Data();
			setData.fullPath = actionSetPath;
			setData.PreInitialize();
			initialized = true;
		}

		public virtual void FinishPreInitialize()
		{
			setData.FinishPreInitialize();
		}

		public virtual void Initialize(bool createNew = false, bool throwErrors = true)
		{
			if (createNew)
			{
				setData.Initialize();
			}
			else
			{
				setData = SteamVR_Input.GetActionSetDataFromPath(actionSetPath);
				_ = setData;
			}
			initialized = true;
		}

		public string GetPath()
		{
			return actionSetPath;
		}

		public bool IsActive(SteamVR_Input_Sources source = SteamVR_Input_Sources.Any)
		{
			return setData.IsActive(source);
		}

		public float GetTimeLastChanged(SteamVR_Input_Sources source = SteamVR_Input_Sources.Any)
		{
			return setData.GetTimeLastChanged(source);
		}

		public void Activate(SteamVR_Input_Sources activateForSource = SteamVR_Input_Sources.Any, int priority = 0, bool disableAllOtherActionSets = false)
		{
			setData.Activate(activateForSource, priority, disableAllOtherActionSets);
		}

		public void Deactivate(SteamVR_Input_Sources forSource = SteamVR_Input_Sources.Any)
		{
			setData.Deactivate(forSource);
		}

		public string GetShortName()
		{
			return setData.GetShortName();
		}

		public bool ShowBindingHints(ISteamVR_Action_In originToHighlight = null)
		{
			if (originToHighlight == null)
			{
				return SteamVR_Input.ShowBindingHints(this);
			}
			return SteamVR_Input.ShowBindingHints(originToHighlight);
		}

		public bool ReadRawSetActive(SteamVR_Input_Sources inputSource)
		{
			return setData.ReadRawSetActive(inputSource);
		}

		public float ReadRawSetLastChanged(SteamVR_Input_Sources inputSource)
		{
			return setData.ReadRawSetLastChanged(inputSource);
		}

		public int ReadRawSetPriority(SteamVR_Input_Sources inputSource)
		{
			return setData.ReadRawSetPriority(inputSource);
		}

		public SteamVR_ActionSet_Data GetActionSetData()
		{
			return setData;
		}

		public CreateType GetCopy<CreateType>() where CreateType : SteamVR_ActionSet, new()
		{
			if (SteamVR_Input.ShouldMakeCopy())
			{
				return new CreateType
				{
					actionSetPath = actionSetPath,
					setData = setData,
					initialized = true
				};
			}
			return (CreateType)this;
		}

		public bool Equals(SteamVR_ActionSet other)
		{
			if ((object)other == null)
			{
				return false;
			}
			return actionSetPath == other.actionSetPath;
		}

		public override bool Equals(object other)
		{
			if (other == null)
			{
				if (string.IsNullOrEmpty(actionSetPath))
				{
					return true;
				}
				return false;
			}
			if (this == other)
			{
				return true;
			}
			if (other is SteamVR_ActionSet)
			{
				return Equals((SteamVR_ActionSet)other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			if (actionSetPath == null)
			{
				return 0;
			}
			return actionSetPath.GetHashCode();
		}

		public static bool operator !=(SteamVR_ActionSet set1, SteamVR_ActionSet set2)
		{
			return !(set1 == set2);
		}

		public static bool operator ==(SteamVR_ActionSet set1, SteamVR_ActionSet set2)
		{
			bool flag = (object)set1 == null || string.IsNullOrEmpty(set1.actionSetPath) || set1.GetActionSetData() == null;
			bool flag2 = (object)set2 == null || string.IsNullOrEmpty(set2.actionSetPath) || set2.GetActionSetData() == null;
			if (flag && flag2)
			{
				return true;
			}
			if (flag != flag2)
			{
				return false;
			}
			return set1.Equals(set2);
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
		}

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			if (setData != null && setData.fullPath != actionSetPath)
			{
				setData = SteamVR_Input.GetActionSetDataFromPath(actionSetPath);
			}
			if (!initialized)
			{
				Initialize(false, false);
			}
		}
	}
}
