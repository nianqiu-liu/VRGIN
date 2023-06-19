using System.Collections.Generic;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_ActionSet_Data : ISteamVR_ActionSet
    {
        protected bool[] rawSetActive = new bool[SteamVR_Input_Source.numSources];

        protected float[] rawSetLastChanged = new float[SteamVR_Input_Source.numSources];

        protected int[] rawSetPriority = new int[SteamVR_Input_Source.numSources];

        protected bool initialized;

        private string cachedShortName;

        public SteamVR_Action[] allActions { get; set; }

        public ISteamVR_Action_In[] nonVisualInActions { get; set; }

        public ISteamVR_Action_In[] visualActions { get; set; }

        public SteamVR_Action_Pose[] poseActions { get; set; }

        public SteamVR_Action_Skeleton[] skeletonActions { get; set; }

        public ISteamVR_Action_Out[] outActionArray { get; set; }

        public string fullPath { get; set; }

        public string usage { get; set; }

        public ulong handle { get; set; }

        public void PreInitialize() { }

        public void FinishPreInitialize()
        {
            var list = new List<SteamVR_Action>();
            var list2 = new List<ISteamVR_Action_In>();
            var list3 = new List<ISteamVR_Action_In>();
            var list4 = new List<SteamVR_Action_Pose>();
            var list5 = new List<SteamVR_Action_Skeleton>();
            var list6 = new List<ISteamVR_Action_Out>();
            if (SteamVR_Input.actions == null)
            {
                Debug.LogError("<b>[SteamVR Input]</b> Actions not initialized!");
                return;
            }

            for (var i = 0; i < SteamVR_Input.actions.Length; i++)
            {
                var steamVR_Action = SteamVR_Input.actions[i];
                if (steamVR_Action.actionSet.GetActionSetData() == this)
                {
                    list.Add(steamVR_Action);
                    if (steamVR_Action is ISteamVR_Action_Boolean || steamVR_Action is ISteamVR_Action_Single || steamVR_Action is ISteamVR_Action_Vector2 || steamVR_Action is ISteamVR_Action_Vector3)
                        list2.Add((ISteamVR_Action_In)steamVR_Action);
                    else if (steamVR_Action is SteamVR_Action_Pose)
                    {
                        list3.Add((ISteamVR_Action_In)steamVR_Action);
                        list4.Add((SteamVR_Action_Pose)steamVR_Action);
                    }
                    else if (steamVR_Action is SteamVR_Action_Skeleton)
                    {
                        list3.Add((ISteamVR_Action_In)steamVR_Action);
                        list5.Add((SteamVR_Action_Skeleton)steamVR_Action);
                    }
                    else if (steamVR_Action is ISteamVR_Action_Out)
                        list6.Add((ISteamVR_Action_Out)steamVR_Action);
                    else
                        Debug.LogError("<b>[SteamVR Input]</b> Action doesn't implement known interface: " + steamVR_Action.fullPath);
                }
            }

            allActions = list.ToArray();
            nonVisualInActions = list2.ToArray();
            visualActions = list3.ToArray();
            poseActions = list4.ToArray();
            skeletonActions = list5.ToArray();
            outActionArray = list6.ToArray();
        }

        public void Initialize()
        {
            var pHandle = 0uL;
            var actionSetHandle = OpenVR.Input.GetActionSetHandle(fullPath.ToLower(), ref pHandle);
            handle = pHandle;
            if (actionSetHandle != 0) Debug.LogError("<b>[SteamVR]</b> GetActionSetHandle (" + fullPath + ") error: " + actionSetHandle);
            initialized = true;
        }

        public bool IsActive(SteamVR_Input_Sources source = SteamVR_Input_Sources.Any)
        {
            if (initialized)
            {
                if (!rawSetActive[(int)source]) return rawSetActive[0];
                return true;
            }

            return false;
        }

        public float GetTimeLastChanged(SteamVR_Input_Sources source = SteamVR_Input_Sources.Any)
        {
            if (initialized) return rawSetLastChanged[(int)source];
            return 0f;
        }

        public void Activate(SteamVR_Input_Sources activateForSource = SteamVR_Input_Sources.Any, int priority = 0, bool disableAllOtherActionSets = false)
        {
            if (disableAllOtherActionSets) SteamVR_ActionSet_Manager.DisableAllActionSets();
            if (!rawSetActive[(int)activateForSource])
            {
                rawSetActive[(int)activateForSource] = true;
                SteamVR_ActionSet_Manager.SetChanged();
                rawSetLastChanged[(int)activateForSource] = Time.realtimeSinceStartup;
            }

            if (rawSetPriority[(int)activateForSource] != priority)
            {
                rawSetPriority[(int)activateForSource] = priority;
                SteamVR_ActionSet_Manager.SetChanged();
                rawSetLastChanged[(int)activateForSource] = Time.realtimeSinceStartup;
            }
        }

        public void Deactivate(SteamVR_Input_Sources forSource = SteamVR_Input_Sources.Any)
        {
            if (rawSetActive[(int)forSource])
            {
                rawSetLastChanged[(int)forSource] = Time.realtimeSinceStartup;
                SteamVR_ActionSet_Manager.SetChanged();
            }

            rawSetActive[(int)forSource] = false;
            rawSetPriority[(int)forSource] = 0;
        }

        public string GetShortName()
        {
            if (cachedShortName == null) cachedShortName = SteamVR_Input_ActionFile.GetShortName(fullPath);
            return cachedShortName;
        }

        public bool ReadRawSetActive(SteamVR_Input_Sources inputSource)
        {
            return rawSetActive[(int)inputSource];
        }

        public float ReadRawSetLastChanged(SteamVR_Input_Sources inputSource)
        {
            return rawSetLastChanged[(int)inputSource];
        }

        public int ReadRawSetPriority(SteamVR_Input_Sources inputSource)
        {
            return rawSetPriority[(int)inputSource];
        }
    }
}
