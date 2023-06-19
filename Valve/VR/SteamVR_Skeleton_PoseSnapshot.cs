using UnityEngine;

namespace Valve.VR
{
    public class SteamVR_Skeleton_PoseSnapshot
    {
        public SteamVR_Input_Sources inputSource;

        public Vector3 position;

        public Quaternion rotation;

        public Vector3[] bonePositions;

        public Quaternion[] boneRotations;

        public SteamVR_Skeleton_PoseSnapshot(int boneCount, SteamVR_Input_Sources source)
        {
            inputSource = source;
            bonePositions = new Vector3[boneCount];
            boneRotations = new Quaternion[boneCount];
            position = Vector3.zero;
            rotation = Quaternion.identity;
        }

        public void CopyFrom(SteamVR_Skeleton_PoseSnapshot source)
        {
            inputSource = source.inputSource;
            position = source.position;
            rotation = source.rotation;
            for (var i = 0; i < bonePositions.Length; i++)
            {
                bonePositions[i] = source.bonePositions[i];
                boneRotations[i] = source.boneRotations[i];
            }
        }
    }
}
