using System;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Action_Skeleton_Source : SteamVR_Action_Pose_Source, ISteamVR_Action_Skeleton_Source
    {
        protected static uint skeletonActionData_size;

        protected VRSkeletalSummaryData_t skeletalSummaryData;

        protected VRSkeletalSummaryData_t lastSkeletalSummaryData;

        protected SteamVR_Action_Skeleton skeletonAction;

        protected VRBoneTransform_t[] tempBoneTransforms = new VRBoneTransform_t[31];

        protected InputSkeletalActionData_t skeletonActionData;

        protected InputSkeletalActionData_t lastSkeletonActionData;

        protected InputSkeletalActionData_t tempSkeletonActionData;

        public override bool activeBinding => skeletonActionData.bActive;

        public override bool lastActiveBinding => lastSkeletonActionData.bActive;

        public Vector3[] bonePositions { get; protected set; }

        public Quaternion[] boneRotations { get; protected set; }

        public Vector3[] lastBonePositions { get; protected set; }

        public Quaternion[] lastBoneRotations { get; protected set; }

        public EVRSkeletalMotionRange rangeOfMotion { get; set; }

        public EVRSkeletalTransformSpace skeletalTransformSpace { get; set; }

        public EVRSummaryType summaryDataType { get; set; }

        public float thumbCurl => fingerCurls[0];

        public float indexCurl => fingerCurls[1];

        public float middleCurl => fingerCurls[2];

        public float ringCurl => fingerCurls[3];

        public float pinkyCurl => fingerCurls[4];

        public float thumbIndexSplay => fingerSplays[0];

        public float indexMiddleSplay => fingerSplays[1];

        public float middleRingSplay => fingerSplays[2];

        public float ringPinkySplay => fingerSplays[3];

        public float lastThumbCurl => lastFingerCurls[0];

        public float lastIndexCurl => lastFingerCurls[1];

        public float lastMiddleCurl => lastFingerCurls[2];

        public float lastRingCurl => lastFingerCurls[3];

        public float lastPinkyCurl => lastFingerCurls[4];

        public float lastThumbIndexSplay => lastFingerSplays[0];

        public float lastIndexMiddleSplay => lastFingerSplays[1];

        public float lastMiddleRingSplay => lastFingerSplays[2];

        public float lastRingPinkySplay => lastFingerSplays[3];

        public float[] fingerCurls { get; protected set; }

        public float[] fingerSplays { get; protected set; }

        public float[] lastFingerCurls { get; protected set; }

        public float[] lastFingerSplays { get; protected set; }

        public bool poseChanged { get; protected set; }

        public bool onlyUpdateSummaryData { get; set; }

        public int boneCount => (int)GetBoneCount();

        public int[] boneHierarchy => GetBoneHierarchy();

        public EVRSkeletalTrackingLevel skeletalTrackingLevel => GetSkeletalTrackingLevel();

        public new event SteamVR_Action_Skeleton.ActiveChangeHandler onActiveChange;

        public new event SteamVR_Action_Skeleton.ActiveChangeHandler onActiveBindingChange;

        public new event SteamVR_Action_Skeleton.ChangeHandler onChange;

        public new event SteamVR_Action_Skeleton.UpdateHandler onUpdate;

        public new event SteamVR_Action_Skeleton.TrackingChangeHandler onTrackingChanged;

        public new event SteamVR_Action_Skeleton.ValidPoseChangeHandler onValidPoseChanged;

        public new event SteamVR_Action_Skeleton.DeviceConnectedChangeHandler onDeviceConnectedChanged;

        public override void Preinitialize(SteamVR_Action wrappingAction, SteamVR_Input_Sources forInputSource)
        {
            base.Preinitialize(wrappingAction, forInputSource);
            skeletonAction = (SteamVR_Action_Skeleton)wrappingAction;
            bonePositions = new Vector3[31];
            lastBonePositions = new Vector3[31];
            boneRotations = new Quaternion[31];
            lastBoneRotations = new Quaternion[31];
            rangeOfMotion = EVRSkeletalMotionRange.WithController;
            skeletalTransformSpace = EVRSkeletalTransformSpace.Parent;
            fingerCurls = new float[SteamVR_Skeleton_FingerIndexes.enumArray.Length];
            fingerSplays = new float[SteamVR_Skeleton_FingerSplayIndexes.enumArray.Length];
            lastFingerCurls = new float[SteamVR_Skeleton_FingerIndexes.enumArray.Length];
            lastFingerSplays = new float[SteamVR_Skeleton_FingerSplayIndexes.enumArray.Length];
        }

        public override void Initialize()
        {
            base.Initialize();
            if (skeletonActionData_size == 0) skeletonActionData_size = (uint)Marshal.SizeOf(typeof(InputSkeletalActionData_t));
        }

        public override void RemoveAllListeners()
        {
            base.RemoveAllListeners();
            Delegate[] invocationList;
            if (onActiveChange != null)
            {
                invocationList = onActiveChange.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var @delegate in array) onActiveChange -= (SteamVR_Action_Skeleton.ActiveChangeHandler)@delegate;
                }
            }

            if (onChange != null)
            {
                invocationList = onChange.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate2 in array) onChange -= (SteamVR_Action_Skeleton.ChangeHandler)delegate2;
                }
            }

            if (onUpdate != null)
            {
                invocationList = onUpdate.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate3 in array) onUpdate -= (SteamVR_Action_Skeleton.UpdateHandler)delegate3;
                }
            }

            if (onTrackingChanged != null)
            {
                invocationList = onTrackingChanged.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate4 in array) onTrackingChanged -= (SteamVR_Action_Skeleton.TrackingChangeHandler)delegate4;
                }
            }

            if (onValidPoseChanged != null)
            {
                invocationList = onValidPoseChanged.GetInvocationList();
                if (invocationList != null)
                {
                    var array = invocationList;
                    foreach (var delegate5 in array) onValidPoseChanged -= (SteamVR_Action_Skeleton.ValidPoseChangeHandler)delegate5;
                }
            }

            if (onDeviceConnectedChanged == null) return;
            invocationList = onDeviceConnectedChanged.GetInvocationList();
            if (invocationList != null)
            {
                var array = invocationList;
                foreach (var delegate6 in array) onDeviceConnectedChanged -= (SteamVR_Action_Skeleton.DeviceConnectedChangeHandler)delegate6;
            }
        }

        public override void UpdateValue()
        {
            UpdateValue(false);
        }

        public override void UpdateValue(bool skipStateAndEventUpdates)
        {
            lastActive = active;
            lastSkeletonActionData = skeletonActionData;
            lastSkeletalSummaryData = skeletalSummaryData;
            if (!onlyUpdateSummaryData)
            {
                for (var i = 0; i < 31; i++)
                {
                    lastBonePositions[i] = bonePositions[i];
                    lastBoneRotations[i] = boneRotations[i];
                }
            }

            for (var j = 0; j < SteamVR_Skeleton_FingerIndexes.enumArray.Length; j++) lastFingerCurls[j] = fingerCurls[j];
            for (var k = 0; k < SteamVR_Skeleton_FingerSplayIndexes.enumArray.Length; k++) lastFingerSplays[k] = fingerSplays[k];
            base.UpdateValue(true);
            poseChanged = changed;
            var skeletalActionData = OpenVR.Input.GetSkeletalActionData(handle, ref skeletonActionData, skeletonActionData_size);
            if (skeletalActionData != 0)
            {
                Debug.LogError("<b>[SteamVR]</b> GetSkeletalActionData error (" + fullPath + "): " + skeletalActionData.ToString() + " handle: " + handle);
                return;
            }

            if (active)
            {
                if (!onlyUpdateSummaryData)
                {
                    skeletalActionData = OpenVR.Input.GetSkeletalBoneData(handle, skeletalTransformSpace, rangeOfMotion, tempBoneTransforms);
                    if (skeletalActionData != 0) Debug.LogError("<b>[SteamVR]</b> GetSkeletalBoneData error (" + fullPath + "): " + skeletalActionData.ToString() + " handle: " + handle);
                    for (var l = 0; l < tempBoneTransforms.Length; l++)
                    {
                        bonePositions[l].x = 0f - tempBoneTransforms[l].position.v0;
                        bonePositions[l].y = tempBoneTransforms[l].position.v1;
                        bonePositions[l].z = tempBoneTransforms[l].position.v2;
                        boneRotations[l].x = tempBoneTransforms[l].orientation.x;
                        boneRotations[l].y = 0f - tempBoneTransforms[l].orientation.y;
                        boneRotations[l].z = 0f - tempBoneTransforms[l].orientation.z;
                        boneRotations[l].w = tempBoneTransforms[l].orientation.w;
                    }

                    boneRotations[0] = SteamVR_Action_Skeleton.steamVRFixUpRotation * boneRotations[0];
                }

                UpdateSkeletalSummaryData(summaryDataType, true);
            }

            if (!changed)
            {
                for (var m = 0; m < tempBoneTransforms.Length; m++)
                {
                    if (Vector3.Distance(lastBonePositions[m], bonePositions[m]) > changeTolerance)
                    {
                        changed = true;
                        break;
                    }

                    if (Mathf.Abs(Quaternion.Angle(lastBoneRotations[m], boneRotations[m])) > changeTolerance)
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed) changedTime = Time.realtimeSinceStartup;
            if (!skipStateAndEventUpdates) CheckAndSendEvents();
        }

        public uint GetBoneCount()
        {
            var pBoneCount = 0u;
            var eVRInputError = OpenVR.Input.GetBoneCount(handle, ref pBoneCount);
            if (eVRInputError != 0) Debug.LogError("<b>[SteamVR]</b> GetBoneCount error (" + fullPath + "): " + eVRInputError.ToString() + " handle: " + handle);
            return pBoneCount;
        }

        public int[] GetBoneHierarchy()
        {
            var array = new int[GetBoneCount()];
            var eVRInputError = OpenVR.Input.GetBoneHierarchy(handle, array);
            if (eVRInputError != 0) Debug.LogError("<b>[SteamVR]</b> GetBoneHierarchy error (" + fullPath + "): " + eVRInputError.ToString() + " handle: " + handle);
            return array;
        }

        public string GetBoneName(int boneIndex)
        {
            var stringBuilder = new StringBuilder(255);
            var boneName = OpenVR.Input.GetBoneName(handle, boneIndex, stringBuilder, 255u);
            if (boneName != 0) Debug.LogError("<b>[SteamVR]</b> GetBoneName error (" + fullPath + "): " + boneName.ToString() + " handle: " + handle);
            return stringBuilder.ToString();
        }

        public SteamVR_Utils.RigidTransform[] GetReferenceTransforms(EVRSkeletalTransformSpace transformSpace, EVRSkeletalReferencePose referencePose)
        {
            var array = new SteamVR_Utils.RigidTransform[GetBoneCount()];
            var array2 = new VRBoneTransform_t[array.Length];
            var skeletalReferenceTransforms = OpenVR.Input.GetSkeletalReferenceTransforms(handle, transformSpace, referencePose, array2);
            if (skeletalReferenceTransforms != 0) Debug.LogError("<b>[SteamVR]</b> GetSkeletalReferenceTransforms error (" + fullPath + "): " + skeletalReferenceTransforms.ToString() + " handle: " + handle);
            for (var i = 0; i < array2.Length; i++)
            {
                var pos = new Vector3(0f - array2[i].position.v0, array2[i].position.v1, array2[i].position.v2);
                var rot = new Quaternion(array2[i].orientation.x, 0f - array2[i].orientation.y, 0f - array2[i].orientation.z, array2[i].orientation.w);
                array[i] = new SteamVR_Utils.RigidTransform(pos, rot);
            }

            if (array.Length != 0)
            {
                var quaternion = Quaternion.AngleAxis(180f, Vector3.up);
                array[0].rot = quaternion * array[0].rot;
            }

            return array;
        }

        public EVRSkeletalTrackingLevel GetSkeletalTrackingLevel()
        {
            var pSkeletalTrackingLevel = EVRSkeletalTrackingLevel.VRSkeletalTracking_Estimated;
            var eVRInputError = OpenVR.Input.GetSkeletalTrackingLevel(handle, ref pSkeletalTrackingLevel);
            if (eVRInputError != 0) Debug.LogError("<b>[SteamVR]</b> GetSkeletalTrackingLevel error (" + fullPath + "): " + eVRInputError.ToString() + " handle: " + handle);
            return pSkeletalTrackingLevel;
        }

        protected VRSkeletalSummaryData_t GetSkeletalSummaryData(EVRSummaryType summaryType = EVRSummaryType.FromAnimation, bool force = false)
        {
            UpdateSkeletalSummaryData(summaryType, force);
            return skeletalSummaryData;
        }

        protected void UpdateSkeletalSummaryData(EVRSummaryType summaryType = EVRSummaryType.FromAnimation, bool force = false)
        {
            if (force || (summaryDataType != summaryDataType && active))
            {
                var eVRInputError = OpenVR.Input.GetSkeletalSummaryData(handle, summaryType, ref skeletalSummaryData);
                if (eVRInputError != 0) Debug.LogError("<b>[SteamVR]</b> GetSkeletalSummaryData error (" + fullPath + "): " + eVRInputError.ToString() + " handle: " + handle);
                fingerCurls[0] = skeletalSummaryData.flFingerCurl0;
                fingerCurls[1] = skeletalSummaryData.flFingerCurl1;
                fingerCurls[2] = skeletalSummaryData.flFingerCurl2;
                fingerCurls[3] = skeletalSummaryData.flFingerCurl3;
                fingerCurls[4] = skeletalSummaryData.flFingerCurl4;
                fingerSplays[0] = skeletalSummaryData.flFingerSplay0;
                fingerSplays[1] = skeletalSummaryData.flFingerSplay1;
                fingerSplays[2] = skeletalSummaryData.flFingerSplay2;
                fingerSplays[3] = skeletalSummaryData.flFingerSplay3;
            }
        }

        protected override void CheckAndSendEvents()
        {
            if (trackingState != lastTrackingState && onTrackingChanged != null) onTrackingChanged(skeletonAction, trackingState);
            if (poseIsValid != lastPoseIsValid && onValidPoseChanged != null) onValidPoseChanged(skeletonAction, poseIsValid);
            if (deviceIsConnected != lastDeviceIsConnected && onDeviceConnectedChanged != null) onDeviceConnectedChanged(skeletonAction, deviceIsConnected);
            if (changed && onChange != null) onChange(skeletonAction);
            if (active != lastActive && onActiveChange != null) onActiveChange(skeletonAction, active);
            if (activeBinding != lastActiveBinding && onActiveBindingChange != null) onActiveBindingChange(skeletonAction, activeBinding);
            if (onUpdate != null) onUpdate(skeletonAction);
        }
    }
}
