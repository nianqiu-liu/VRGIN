using System;
using System.Collections.Generic;
using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Skeleton_Poser : MonoBehaviour
    {
        public class SkeletonBlendablePose
        {
            public SteamVR_Skeleton_Pose pose;

            public SteamVR_Skeleton_PoseSnapshot snapshotR;

            public SteamVR_Skeleton_PoseSnapshot snapshotL;

            public SteamVR_Skeleton_PoseSnapshot GetHandSnapshot(SteamVR_Input_Sources inputSource)
            {
                if (inputSource == SteamVR_Input_Sources.LeftHand) return snapshotL;
                return snapshotR;
            }

            public void UpdateAdditiveAnimation(SteamVR_Action_Skeleton skeletonAction, SteamVR_Input_Sources inputSource)
            {
                if (skeletonAction.GetSkeletalTrackingLevel() == EVRSkeletalTrackingLevel.VRSkeletalTracking_Estimated) return;
                var handSnapshot = GetHandSnapshot(inputSource);
                var hand = pose.GetHand(inputSource);
                for (var i = 0; i < snapshotL.bonePositions.Length; i++)
                {
                    var fingerForBone = SteamVR_Skeleton_JointIndexes.GetFingerForBone(i);
                    var movementTypeForBone = hand.GetMovementTypeForBone(i);
                    if (movementTypeForBone == SteamVR_Skeleton_FingerExtensionTypes.Free)
                    {
                        handSnapshot.bonePositions[i] = skeletonAction.bonePositions[i];
                        handSnapshot.boneRotations[i] = skeletonAction.boneRotations[i];
                    }

                    if (movementTypeForBone == SteamVR_Skeleton_FingerExtensionTypes.Extend)
                    {
                        handSnapshot.bonePositions[i] = Vector3.Lerp(hand.bonePositions[i], skeletonAction.bonePositions[i], 1f - skeletonAction.fingerCurls[fingerForBone]);
                        handSnapshot.boneRotations[i] = Quaternion.Lerp(hand.boneRotations[i], skeletonAction.boneRotations[i], 1f - skeletonAction.fingerCurls[fingerForBone]);
                    }

                    if (movementTypeForBone == SteamVR_Skeleton_FingerExtensionTypes.Contract)
                    {
                        handSnapshot.bonePositions[i] = Vector3.Lerp(hand.bonePositions[i], skeletonAction.bonePositions[i], skeletonAction.fingerCurls[fingerForBone]);
                        handSnapshot.boneRotations[i] = Quaternion.Lerp(hand.boneRotations[i], skeletonAction.boneRotations[i], skeletonAction.fingerCurls[fingerForBone]);
                    }
                }
            }

            public SkeletonBlendablePose(SteamVR_Skeleton_Pose p)
            {
                pose = p;
                snapshotR = new SteamVR_Skeleton_PoseSnapshot(p.rightHand.bonePositions.Length, SteamVR_Input_Sources.RightHand);
                snapshotL = new SteamVR_Skeleton_PoseSnapshot(p.leftHand.bonePositions.Length, SteamVR_Input_Sources.LeftHand);
            }

            public void PoseToSnapshots()
            {
                snapshotR.position = pose.rightHand.position;
                snapshotR.rotation = pose.rightHand.rotation;
                pose.rightHand.bonePositions.CopyTo(snapshotR.bonePositions, 0);
                pose.rightHand.boneRotations.CopyTo(snapshotR.boneRotations, 0);
                snapshotL.position = pose.leftHand.position;
                snapshotL.rotation = pose.leftHand.rotation;
                pose.leftHand.bonePositions.CopyTo(snapshotL.bonePositions, 0);
                pose.leftHand.boneRotations.CopyTo(snapshotL.boneRotations, 0);
            }

            public SkeletonBlendablePose() { }
        }

        [Serializable]
        public class PoseBlendingBehaviour
        {
            public enum BlenderTypes
            {
                Manual = 0,
                AnalogAction = 1,
                BooleanAction = 2
            }

            public string name;

            public bool enabled = true;

            public float influence = 1f;

            public int pose = 1;

            public float value;

            public SteamVR_Action_Single action_single;

            public SteamVR_Action_Boolean action_bool;

            public float smoothingSpeed;

            public BlenderTypes type;

            public bool useMask;

            public SteamVR_Skeleton_HandMask mask = new SteamVR_Skeleton_HandMask();

            public bool previewEnabled;

            public void Update(float deltaTime, SteamVR_Input_Sources inputSource)
            {
                if (type == BlenderTypes.AnalogAction)
                {
                    if (smoothingSpeed == 0f)
                        value = action_single.GetAxis(inputSource);
                    else
                        value = Mathf.Lerp(value, action_single.GetAxis(inputSource), deltaTime * smoothingSpeed);
                }

                if (type == BlenderTypes.BooleanAction)
                {
                    if (smoothingSpeed == 0f)
                        value = action_bool.GetState(inputSource) ? 1 : 0;
                    else
                        value = Mathf.Lerp(value, action_bool.GetState(inputSource) ? 1 : 0, deltaTime * smoothingSpeed);
                }
            }

            public void ApplyBlending(SteamVR_Skeleton_PoseSnapshot snapshot, SkeletonBlendablePose[] blendPoses, SteamVR_Input_Sources inputSource)
            {
                var handSnapshot = blendPoses[pose].GetHandSnapshot(inputSource);
                if (mask.GetFinger(0) || !useMask)
                {
                    snapshot.position = Vector3.Lerp(snapshot.position, handSnapshot.position, influence * value);
                    snapshot.rotation = Quaternion.Slerp(snapshot.rotation, handSnapshot.rotation, influence * value);
                }

                for (var i = 0; i < snapshot.bonePositions.Length; i++)
                {
                    if (mask.GetFinger(SteamVR_Skeleton_JointIndexes.GetFingerForBone(i) + 1) || !useMask)
                    {
                        snapshot.bonePositions[i] = Vector3.Lerp(snapshot.bonePositions[i], handSnapshot.bonePositions[i], influence * value);
                        snapshot.boneRotations[i] = Quaternion.Slerp(snapshot.boneRotations[i], handSnapshot.boneRotations[i], influence * value);
                    }
                }
            }

            public PoseBlendingBehaviour()
            {
                enabled = true;
                influence = 1f;
            }
        }

        public bool poseEditorExpanded = true;

        public bool blendEditorExpanded = true;

        public string[] poseNames;

        public GameObject overridePreviewLeftHandPrefab;

        public GameObject overridePreviewRightHandPrefab;

        public SteamVR_Skeleton_Pose skeletonMainPose;

        public List<SteamVR_Skeleton_Pose> skeletonAdditionalPoses = new List<SteamVR_Skeleton_Pose>();

        [SerializeField] protected bool showLeftPreview;

        [SerializeField] protected bool showRightPreview = true;

        [SerializeField] protected GameObject previewLeftInstance;

        [SerializeField] protected GameObject previewRightInstance;

        [SerializeField] protected int previewPoseSelection;

        public List<PoseBlendingBehaviour> blendingBehaviours = new List<PoseBlendingBehaviour>();

        public SteamVR_Skeleton_PoseSnapshot blendedSnapshotL;

        public SteamVR_Skeleton_PoseSnapshot blendedSnapshotR;

        private SkeletonBlendablePose[] blendPoses;

        private int boneCount;

        private bool poseUpdatedThisFrame;

        public float scale;

        public int blendPoseCount => blendPoses.Length;

        protected void Awake()
        {
            if (previewLeftInstance != null) DestroyImmediate(previewLeftInstance);
            if (previewRightInstance != null) DestroyImmediate(previewRightInstance);
            blendPoses = new SkeletonBlendablePose[skeletonAdditionalPoses.Count + 1];
            for (var i = 0; i < blendPoseCount; i++)
            {
                blendPoses[i] = new SkeletonBlendablePose(GetPoseByIndex(i));
                blendPoses[i].PoseToSnapshots();
            }

            boneCount = skeletonMainPose.leftHand.bonePositions.Length;
            blendedSnapshotL = new SteamVR_Skeleton_PoseSnapshot(boneCount, SteamVR_Input_Sources.LeftHand);
            blendedSnapshotR = new SteamVR_Skeleton_PoseSnapshot(boneCount, SteamVR_Input_Sources.RightHand);
        }

        public void SetBlendingBehaviourValue(string behaviourName, float value)
        {
            var poseBlendingBehaviour = FindBlendingBehaviour(behaviourName);
            if (poseBlendingBehaviour != null)
            {
                poseBlendingBehaviour.value = value;
                if (poseBlendingBehaviour.type != 0) Debug.LogWarning("[SteamVR] Blending Behaviour: " + behaviourName + " is not a manual behaviour. Its value will likely be overriden.", this);
            }
        }

        public float GetBlendingBehaviourValue(string behaviourName)
        {
            return FindBlendingBehaviour(behaviourName)?.value ?? 0f;
        }

        public void SetBlendingBehaviourEnabled(string behaviourName, bool value)
        {
            var poseBlendingBehaviour = FindBlendingBehaviour(behaviourName);
            if (poseBlendingBehaviour != null) poseBlendingBehaviour.enabled = value;
        }

        public bool GetBlendingBehaviourEnabled(string behaviourName)
        {
            return FindBlendingBehaviour(behaviourName)?.enabled ?? false;
        }

        public PoseBlendingBehaviour GetBlendingBehaviour(string behaviourName)
        {
            return FindBlendingBehaviour(behaviourName);
        }

        protected PoseBlendingBehaviour FindBlendingBehaviour(string behaviourName, bool throwErrors = true)
        {
            var poseBlendingBehaviour = blendingBehaviours.Find((PoseBlendingBehaviour b) => b.name == behaviourName);
            if (poseBlendingBehaviour == null)
            {
                if (throwErrors) Debug.LogError("[SteamVR] Blending Behaviour: " + behaviourName + " not found on Skeleton Poser: " + gameObject.name, this);
                return null;
            }

            return poseBlendingBehaviour;
        }

        public SteamVR_Skeleton_Pose GetPoseByIndex(int index)
        {
            if (index == 0) return skeletonMainPose;
            return skeletonAdditionalPoses[index - 1];
        }

        private SteamVR_Skeleton_PoseSnapshot GetHandSnapshot(SteamVR_Input_Sources inputSource)
        {
            if (inputSource == SteamVR_Input_Sources.LeftHand) return blendedSnapshotL;
            return blendedSnapshotR;
        }

        public SteamVR_Skeleton_PoseSnapshot GetBlendedPose(SteamVR_Action_Skeleton skeletonAction, SteamVR_Input_Sources handType)
        {
            UpdatePose(skeletonAction, handType);
            return GetHandSnapshot(handType);
        }

        public SteamVR_Skeleton_PoseSnapshot GetBlendedPose(SteamVR_Behaviour_Skeleton skeletonBehaviour)
        {
            return GetBlendedPose(skeletonBehaviour.skeletonAction, skeletonBehaviour.inputSource);
        }

        public void UpdatePose(SteamVR_Action_Skeleton skeletonAction, SteamVR_Input_Sources inputSource)
        {
            if (!poseUpdatedThisFrame)
            {
                poseUpdatedThisFrame = true;
                if (skeletonAction.activeBinding) blendPoses[0].UpdateAdditiveAnimation(skeletonAction, inputSource);
                var handSnapshot = GetHandSnapshot(inputSource);
                handSnapshot.CopyFrom(blendPoses[0].GetHandSnapshot(inputSource));
                ApplyBlenderBehaviours(skeletonAction, inputSource, handSnapshot);
                if (inputSource == SteamVR_Input_Sources.RightHand) blendedSnapshotR = handSnapshot;
                if (inputSource == SteamVR_Input_Sources.LeftHand) blendedSnapshotL = handSnapshot;
            }
        }

        protected void ApplyBlenderBehaviours(SteamVR_Action_Skeleton skeletonAction, SteamVR_Input_Sources inputSource, SteamVR_Skeleton_PoseSnapshot snapshot)
        {
            for (var i = 0; i < blendingBehaviours.Count; i++)
            {
                blendingBehaviours[i].Update(Time.deltaTime, inputSource);
                if (blendingBehaviours[i].enabled && blendingBehaviours[i].influence * blendingBehaviours[i].value > 0.01f)
                {
                    if (blendingBehaviours[i].pose != 0 && skeletonAction.activeBinding) blendPoses[blendingBehaviours[i].pose].UpdateAdditiveAnimation(skeletonAction, inputSource);
                    blendingBehaviours[i].ApplyBlending(snapshot, blendPoses, inputSource);
                }
            }
        }

        protected void LateUpdate()
        {
            poseUpdatedThisFrame = false;
        }

        protected Vector3 BlendVectors(Vector3[] vectors, float[] weights)
        {
            var zero = Vector3.zero;
            for (var i = 0; i < vectors.Length; i++) zero += vectors[i] * weights[i];
            return zero;
        }

        protected Quaternion BlendQuaternions(Quaternion[] quaternions, float[] weights)
        {
            var identity = Quaternion.identity;
            for (var i = 0; i < quaternions.Length; i++) identity *= Quaternion.Slerp(Quaternion.identity, quaternions[i], weights[i]);
            return identity;
        }

        public Vector3 GetTargetHandPosition(SteamVR_Behaviour_Skeleton hand, Transform origin)
        {
            var position = origin.position;
            var rotation = hand.transform.rotation;
            hand.transform.rotation = GetBlendedPose(hand).rotation;
            origin.position = hand.transform.TransformPoint(GetBlendedPose(hand).position);
            var position2 = origin.InverseTransformPoint(hand.transform.position);
            origin.position = position;
            hand.transform.rotation = rotation;
            return origin.TransformPoint(position2);
        }

        public Quaternion GetTargetHandRotation(SteamVR_Behaviour_Skeleton hand, Transform origin)
        {
            var rotation = origin.rotation;
            origin.rotation = hand.transform.rotation * GetBlendedPose(hand).rotation;
            var quaternion = Quaternion.Inverse(origin.rotation) * hand.transform.rotation;
            origin.rotation = rotation;
            return origin.rotation * quaternion;
        }
    }
}
