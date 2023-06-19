using System.Collections;
using UnityEngine;

namespace Valve.VR
{
	public class SteamVR_Behaviour_Skeleton : MonoBehaviour
	{
		public enum MirrorType
		{
			None = 0,
			LeftToRight = 1,
			RightToLeft = 2
		}

		public delegate void ActiveChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, bool active);

		public delegate void ChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource);

		public delegate void UpdateHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource);

		public delegate void TrackingChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, ETrackingResult trackingState);

		public delegate void ValidPoseChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, bool validPose);

		public delegate void DeviceConnectedChangeHandler(SteamVR_Behaviour_Skeleton fromAction, SteamVR_Input_Sources inputSource, bool deviceConnected);

		[Tooltip("If not set, will try to auto assign this based on 'Skeleton' + inputSource")]
		public SteamVR_Action_Skeleton skeletonAction;

		[Tooltip("The device this action should apply to. Any if the action is not device specific.")]
		public SteamVR_Input_Sources inputSource;

		[Tooltip("The range of motion you'd like the hand to move in. With controller is the best estimate of the fingers wrapped around a controller. Without is from a flat hand to a fist.")]
		public EVRSkeletalMotionRange rangeOfMotion = EVRSkeletalMotionRange.WithoutController;

		[Tooltip("This needs to be in the order of: root -> wrist -> thumb, index, middle, ring, pinky")]
		public Transform skeletonRoot;

		[Tooltip("If not set, relative to parent")]
		public Transform origin;

		[Tooltip("Set to true if you want this script to update its position and rotation. False if this will be handled elsewhere")]
		public bool updatePose = true;

		[Tooltip("Check this to not set the positions of the bones. This is helpful for differently scaled skeletons.")]
		public bool onlySetRotations;

		[Range(0f, 1f)]
		[Tooltip("Modify this to blend between animations setup on the hand")]
		public float skeletonBlend = 1f;

		public SteamVR_Behaviour_SkeletonEvent onBoneTransformsUpdated;

		public SteamVR_Behaviour_SkeletonEvent onTransformUpdated;

		public SteamVR_Behaviour_SkeletonEvent onTransformChanged;

		public SteamVR_Behaviour_Skeleton_ConnectedChangedEvent onConnectedChanged;

		public SteamVR_Behaviour_Skeleton_TrackingChangedEvent onTrackingChanged;

		public UpdateHandler onBoneTransformsUpdatedEvent;

		public UpdateHandler onTransformUpdatedEvent;

		public ChangeHandler onTransformChangedEvent;

		public DeviceConnectedChangeHandler onConnectedChangedEvent;

		public TrackingChangeHandler onTrackingChangedEvent;

		[Tooltip("Is this rendermodel a mirror of another one?")]
		public MirrorType mirroring;

		[Header("No Skeleton - Fallback")]
		[Tooltip("The fallback SkeletonPoser to drive hand animation when no skeleton data is available")]
		public SteamVR_Skeleton_Poser fallbackPoser;

		[Tooltip("The fallback action to drive finger curl values when no skeleton data is available")]
		public SteamVR_Action_Single fallbackCurlAction;

		protected SteamVR_Skeleton_Poser blendPoser;

		protected SteamVR_Skeleton_PoseSnapshot blendSnapshot;

		protected Coroutine blendRoutine;

		protected Coroutine rangeOfMotionBlendRoutine;

		protected Coroutine attachRoutine;

		protected Transform[] bones;

		protected EVRSkeletalMotionRange? temporaryRangeOfMotion;

		protected static readonly Quaternion rightFlipAngle = Quaternion.AngleAxis(180f, Vector3.right);

		public bool skeletonAvailable => skeletonAction.activeBinding;

		public bool isActive => skeletonAction.GetActive();

		public float[] fingerCurls
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.GetFingerCurls(false);
				}
				float[] array = new float[5];
				for (int i = 0; i < 5; i++)
				{
					array[i] = fallbackCurlAction.GetAxis(inputSource);
				}
				return array;
			}
		}

		public float thumbCurl
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.thumb);
				}
				return fallbackCurlAction.GetAxis(inputSource);
			}
		}

		public float indexCurl
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.index);
				}
				return fallbackCurlAction.GetAxis(inputSource);
			}
		}

		public float middleCurl
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.middle);
				}
				return fallbackCurlAction.GetAxis(inputSource);
			}
		}

		public float ringCurl
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.ring);
				}
				return fallbackCurlAction.GetAxis(inputSource);
			}
		}

		public float pinkyCurl
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.GetFingerCurl(SteamVR_Skeleton_FingerIndexEnum.pinky);
				}
				return fallbackCurlAction.GetAxis(inputSource);
			}
		}

		public Transform root => bones[0];

		public Transform wrist => bones[1];

		public Transform indexMetacarpal => bones[6];

		public Transform indexProximal => bones[7];

		public Transform indexMiddle => bones[8];

		public Transform indexDistal => bones[9];

		public Transform indexTip => bones[10];

		public Transform middleMetacarpal => bones[11];

		public Transform middleProximal => bones[12];

		public Transform middleMiddle => bones[13];

		public Transform middleDistal => bones[14];

		public Transform middleTip => bones[15];

		public Transform pinkyMetacarpal => bones[21];

		public Transform pinkyProximal => bones[22];

		public Transform pinkyMiddle => bones[23];

		public Transform pinkyDistal => bones[24];

		public Transform pinkyTip => bones[25];

		public Transform ringMetacarpal => bones[16];

		public Transform ringProximal => bones[17];

		public Transform ringMiddle => bones[18];

		public Transform ringDistal => bones[19];

		public Transform ringTip => bones[20];

		public Transform thumbMetacarpal => bones[2];

		public Transform thumbProximal => bones[2];

		public Transform thumbMiddle => bones[3];

		public Transform thumbDistal => bones[4];

		public Transform thumbTip => bones[5];

		public Transform thumbAux => bones[26];

		public Transform indexAux => bones[27];

		public Transform middleAux => bones[28];

		public Transform ringAux => bones[29];

		public Transform pinkyAux => bones[30];

		public Transform[] proximals { get; protected set; }

		public Transform[] middles { get; protected set; }

		public Transform[] distals { get; protected set; }

		public Transform[] tips { get; protected set; }

		public Transform[] auxs { get; protected set; }

		public EVRSkeletalTrackingLevel skeletalTrackingLevel
		{
			get
			{
				if (skeletonAvailable)
				{
					return skeletonAction.skeletalTrackingLevel;
				}
				return EVRSkeletalTrackingLevel.VRSkeletalTracking_Estimated;
			}
		}

		public bool isBlending => blendRoutine != null;

		public SteamVR_ActionSet actionSet => skeletonAction.actionSet;

		public SteamVR_ActionDirections direction => skeletonAction.direction;

		protected virtual void Awake()
		{
			SteamVR.Initialize(false);
			AssignBonesArray();
			proximals = new Transform[5] { thumbProximal, indexProximal, middleProximal, ringProximal, pinkyProximal };
			middles = new Transform[5] { thumbMiddle, indexMiddle, middleMiddle, ringMiddle, pinkyMiddle };
			distals = new Transform[5] { thumbDistal, indexDistal, middleDistal, ringDistal, pinkyDistal };
			tips = new Transform[5] { thumbTip, indexTip, middleTip, ringTip, pinkyTip };
			auxs = new Transform[5] { thumbAux, indexAux, middleAux, ringAux, pinkyAux };
			CheckSkeletonAction();
		}

		protected virtual void CheckSkeletonAction()
		{
			if (skeletonAction == null)
			{
				skeletonAction = SteamVR_Input.GetAction<SteamVR_Action_Skeleton>("Skeleton" + inputSource);
			}
		}

		protected virtual void AssignBonesArray()
		{
			bones = skeletonRoot.GetComponentsInChildren<Transform>();
		}

		protected virtual void OnEnable()
		{
			CheckSkeletonAction();
			SteamVR_Input.onSkeletonsUpdated += SteamVR_Input_OnSkeletonsUpdated;
			if (skeletonAction != null)
			{
				skeletonAction.onDeviceConnectedChanged += OnDeviceConnectedChanged;
				skeletonAction.onTrackingChanged += OnTrackingChanged;
			}
		}

		protected virtual void OnDisable()
		{
			SteamVR_Input.onSkeletonsUpdated -= SteamVR_Input_OnSkeletonsUpdated;
			if (skeletonAction != null)
			{
				skeletonAction.onDeviceConnectedChanged -= OnDeviceConnectedChanged;
				skeletonAction.onTrackingChanged -= OnTrackingChanged;
			}
		}

		private void OnDeviceConnectedChanged(SteamVR_Action_Skeleton fromAction, bool deviceConnected)
		{
			if (onConnectedChanged != null)
			{
				onConnectedChanged.Invoke(this, inputSource, deviceConnected);
			}
			if (onConnectedChangedEvent != null)
			{
				onConnectedChangedEvent(this, inputSource, deviceConnected);
			}
		}

		private void OnTrackingChanged(SteamVR_Action_Skeleton fromAction, ETrackingResult trackingState)
		{
			if (onTrackingChanged != null)
			{
				onTrackingChanged.Invoke(this, inputSource, trackingState);
			}
			if (onTrackingChangedEvent != null)
			{
				onTrackingChangedEvent(this, inputSource, trackingState);
			}
		}

		protected virtual void SteamVR_Input_OnSkeletonsUpdated(bool skipSendingEvents)
		{
			UpdateSkeleton();
		}

		protected virtual void UpdateSkeleton()
		{
			if (skeletonAction == null)
			{
				return;
			}
			if (updatePose)
			{
				UpdatePose();
			}
			if (blendPoser != null && skeletonBlend < 1f)
			{
				if (blendSnapshot == null)
				{
					blendSnapshot = blendPoser.GetBlendedPose(this);
				}
				blendSnapshot = blendPoser.GetBlendedPose(this);
			}
			if (rangeOfMotionBlendRoutine == null)
			{
				if (temporaryRangeOfMotion.HasValue)
				{
					skeletonAction.SetRangeOfMotion(temporaryRangeOfMotion.Value);
				}
				else
				{
					skeletonAction.SetRangeOfMotion(rangeOfMotion);
				}
				UpdateSkeletonTransforms();
			}
		}

		public void SetTemporaryRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
		{
			if (rangeOfMotion != newRangeOfMotion || temporaryRangeOfMotion != newRangeOfMotion)
			{
				TemporaryRangeOfMotionBlend(newRangeOfMotion, blendOverSeconds);
			}
		}

		public void ResetTemporaryRangeOfMotion(float blendOverSeconds = 0.1f)
		{
			ResetTemporaryRangeOfMotionBlend(blendOverSeconds);
		}

		public void SetRangeOfMotion(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds = 0.1f)
		{
			if (rangeOfMotion != newRangeOfMotion)
			{
				RangeOfMotionBlend(newRangeOfMotion, blendOverSeconds);
			}
		}

		public void BlendToSkeleton(float overTime = 0.1f)
		{
			if (blendPoser != null)
			{
				blendSnapshot = blendPoser.GetBlendedPose(this);
			}
			blendPoser = null;
			BlendTo(1f, overTime);
		}

		public void BlendToPoser(SteamVR_Skeleton_Poser poser, float overTime = 0.1f)
		{
			if (!(poser == null))
			{
				blendPoser = poser;
				BlendTo(0f, overTime);
			}
		}

		public void BlendToAnimation(float overTime = 0.1f)
		{
			BlendTo(0f, overTime);
		}

		public void BlendTo(float blendToAmount, float overTime)
		{
			if (blendRoutine != null)
			{
				StopCoroutine(blendRoutine);
			}
			if (base.gameObject.activeInHierarchy)
			{
				blendRoutine = StartCoroutine(DoBlendRoutine(blendToAmount, overTime));
			}
		}

		protected IEnumerator DoBlendRoutine(float blendToAmount, float overTime)
		{
			float startTime = Time.time;
			float endTime = startTime + overTime;
			float startAmount = skeletonBlend;
			while (Time.time < endTime)
			{
				yield return null;
				skeletonBlend = Mathf.Lerp(startAmount, blendToAmount, (Time.time - startTime) / overTime);
			}
			skeletonBlend = blendToAmount;
			blendRoutine = null;
		}

		protected void RangeOfMotionBlend(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds)
		{
			if (rangeOfMotionBlendRoutine != null)
			{
				StopCoroutine(rangeOfMotionBlendRoutine);
			}
			EVRSkeletalMotionRange oldRangeOfMotion = rangeOfMotion;
			rangeOfMotion = newRangeOfMotion;
			if (base.gameObject.activeInHierarchy)
			{
				rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(oldRangeOfMotion, newRangeOfMotion, blendOverSeconds));
			}
		}

		protected void TemporaryRangeOfMotionBlend(EVRSkeletalMotionRange newRangeOfMotion, float blendOverSeconds)
		{
			if (rangeOfMotionBlendRoutine != null)
			{
				StopCoroutine(rangeOfMotionBlendRoutine);
			}
			EVRSkeletalMotionRange value = rangeOfMotion;
			if (temporaryRangeOfMotion.HasValue)
			{
				value = temporaryRangeOfMotion.Value;
			}
			temporaryRangeOfMotion = newRangeOfMotion;
			if (base.gameObject.activeInHierarchy)
			{
				rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(value, newRangeOfMotion, blendOverSeconds));
			}
		}

		protected void ResetTemporaryRangeOfMotionBlend(float blendOverSeconds)
		{
			if (temporaryRangeOfMotion.HasValue)
			{
				if (rangeOfMotionBlendRoutine != null)
				{
					StopCoroutine(rangeOfMotionBlendRoutine);
				}
				EVRSkeletalMotionRange value = temporaryRangeOfMotion.Value;
				EVRSkeletalMotionRange newRangeOfMotion = rangeOfMotion;
				temporaryRangeOfMotion = null;
				if (base.gameObject.activeInHierarchy)
				{
					rangeOfMotionBlendRoutine = StartCoroutine(DoRangeOfMotionBlend(value, newRangeOfMotion, blendOverSeconds));
				}
			}
		}

		protected IEnumerator DoRangeOfMotionBlend(EVRSkeletalMotionRange oldRangeOfMotion, EVRSkeletalMotionRange newRangeOfMotion, float overTime)
		{
			float startTime = Time.time;
			float endTime = startTime + overTime;
			while (Time.time < endTime)
			{
				yield return null;
				float t = (Time.time - startTime) / overTime;
				if (skeletonBlend > 0f)
				{
					skeletonAction.SetRangeOfMotion(oldRangeOfMotion);
					skeletonAction.UpdateValueWithoutEvents();
					Vector3[] array = (Vector3[])GetBonePositions().Clone();
					Quaternion[] array2 = (Quaternion[])GetBoneRotations().Clone();
					skeletonAction.SetRangeOfMotion(newRangeOfMotion);
					skeletonAction.UpdateValueWithoutEvents();
					Vector3[] bonePositions = GetBonePositions();
					Quaternion[] boneRotations = GetBoneRotations();
					for (int i = 0; i < bones.Length; i++)
					{
						if (bones[i] == null || !SteamVR_Utils.IsValid(boneRotations[i]) || !SteamVR_Utils.IsValid(array2[i]))
						{
							continue;
						}
						Vector3 vector = Vector3.Lerp(array[i], bonePositions[i], t);
						Quaternion quaternion = Quaternion.Lerp(array2[i], boneRotations[i], t);
						if (skeletonBlend < 1f)
						{
							if (blendPoser != null)
							{
								SetBonePosition(i, Vector3.Lerp(blendSnapshot.bonePositions[i], vector, skeletonBlend));
								SetBoneRotation(i, Quaternion.Lerp(GetBlendPoseForBone(i, quaternion), quaternion, skeletonBlend));
							}
							else
							{
								SetBonePosition(i, Vector3.Lerp(bones[i].localPosition, vector, skeletonBlend));
								SetBoneRotation(i, Quaternion.Lerp(bones[i].localRotation, quaternion, skeletonBlend));
							}
						}
						else
						{
							SetBonePosition(i, vector);
							SetBoneRotation(i, quaternion);
						}
					}
				}
				if (onBoneTransformsUpdated != null)
				{
					onBoneTransformsUpdated.Invoke(this, inputSource);
				}
				if (onBoneTransformsUpdatedEvent != null)
				{
					onBoneTransformsUpdatedEvent(this, inputSource);
				}
			}
			rangeOfMotionBlendRoutine = null;
		}

		protected virtual Quaternion GetBlendPoseForBone(int boneIndex, Quaternion skeletonRotation)
		{
			return blendSnapshot.boneRotations[boneIndex];
		}

		public virtual void UpdateSkeletonTransforms()
		{
			Vector3[] bonePositions = GetBonePositions();
			Quaternion[] boneRotations = GetBoneRotations();
			if (skeletonBlend <= 0f)
			{
				if (blendPoser != null)
				{
					SteamVR_Skeleton_Pose_Hand hand = blendPoser.skeletonMainPose.GetHand(inputSource);
					for (int i = 0; i < bones.Length; i++)
					{
						if (!(bones[i] == null))
						{
							if ((i == 1 && hand.ignoreWristPoseData) || (i == 0 && hand.ignoreRootPoseData))
							{
								SetBonePosition(i, bonePositions[i]);
								SetBoneRotation(i, boneRotations[i]);
							}
							else
							{
								Quaternion blendPoseForBone = GetBlendPoseForBone(i, boneRotations[i]);
								SetBonePosition(i, blendSnapshot.bonePositions[i]);
								SetBoneRotation(i, blendPoseForBone);
							}
						}
					}
				}
				else
				{
					for (int j = 0; j < bones.Length; j++)
					{
						Quaternion blendPoseForBone2 = GetBlendPoseForBone(j, boneRotations[j]);
						SetBonePosition(j, blendSnapshot.bonePositions[j]);
						SetBoneRotation(j, blendPoseForBone2);
					}
				}
			}
			else if (skeletonBlend >= 1f)
			{
				for (int k = 0; k < bones.Length; k++)
				{
					if (!(bones[k] == null))
					{
						SetBonePosition(k, bonePositions[k]);
						SetBoneRotation(k, boneRotations[k]);
					}
				}
			}
			else
			{
				for (int l = 0; l < bones.Length; l++)
				{
					if (bones[l] == null)
					{
						continue;
					}
					if (blendPoser != null)
					{
						SteamVR_Skeleton_Pose_Hand hand2 = blendPoser.skeletonMainPose.GetHand(inputSource);
						if ((l == 1 && hand2.ignoreWristPoseData) || (l == 0 && hand2.ignoreRootPoseData))
						{
							SetBonePosition(l, bonePositions[l]);
							SetBoneRotation(l, boneRotations[l]);
						}
						else
						{
							SetBonePosition(l, Vector3.Lerp(blendSnapshot.bonePositions[l], bonePositions[l], skeletonBlend));
							SetBoneRotation(l, Quaternion.Lerp(blendSnapshot.boneRotations[l], boneRotations[l], skeletonBlend));
						}
					}
					else if (blendSnapshot == null)
					{
						SetBonePosition(l, Vector3.Lerp(bones[l].localPosition, bonePositions[l], skeletonBlend));
						SetBoneRotation(l, Quaternion.Lerp(bones[l].localRotation, boneRotations[l], skeletonBlend));
					}
					else
					{
						SetBonePosition(l, Vector3.Lerp(blendSnapshot.bonePositions[l], bonePositions[l], skeletonBlend));
						SetBoneRotation(l, Quaternion.Lerp(blendSnapshot.boneRotations[l], boneRotations[l], skeletonBlend));
					}
				}
			}
			if (onBoneTransformsUpdated != null)
			{
				onBoneTransformsUpdated.Invoke(this, inputSource);
			}
			if (onBoneTransformsUpdatedEvent != null)
			{
				onBoneTransformsUpdatedEvent(this, inputSource);
			}
		}

		public virtual void SetBonePosition(int boneIndex, Vector3 localPosition)
		{
			if (!onlySetRotations)
			{
				bones[boneIndex].localPosition = localPosition;
			}
		}

		public virtual void SetBoneRotation(int boneIndex, Quaternion localRotation)
		{
			bones[boneIndex].localRotation = localRotation;
		}

		public virtual Transform GetBone(int joint)
		{
			if (bones == null || bones.Length == 0)
			{
				Awake();
			}
			return bones[joint];
		}

		public Vector3 GetBonePosition(int joint, bool local = false)
		{
			if (local)
			{
				return bones[joint].localPosition;
			}
			return bones[joint].position;
		}

		public Quaternion GetBoneRotation(int joint, bool local = false)
		{
			if (local)
			{
				return bones[joint].localRotation;
			}
			return bones[joint].rotation;
		}

		protected Vector3[] GetBonePositions()
		{
			if (skeletonAvailable)
			{
				Vector3[] bonePositions = skeletonAction.GetBonePositions(false);
				if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
				{
					for (int i = 0; i < bonePositions.Length; i++)
					{
						bonePositions[i] = MirrorPosition(i, bonePositions[i]);
					}
				}
				return bonePositions;
			}
			if (fallbackPoser != null)
			{
				return fallbackPoser.GetBlendedPose(skeletonAction, inputSource).bonePositions;
			}
			Debug.LogError("Skeleton Action is not bound, and you have not provided a fallback SkeletonPoser. Please create one to drive hand animation when no skeleton data is available.", this);
			return null;
		}

		protected Quaternion[] GetBoneRotations()
		{
			if (skeletonAvailable)
			{
				Quaternion[] boneRotations = skeletonAction.GetBoneRotations(false);
				if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
				{
					for (int i = 0; i < boneRotations.Length; i++)
					{
						boneRotations[i] = MirrorRotation(i, boneRotations[i]);
					}
				}
				return boneRotations;
			}
			if (fallbackPoser != null)
			{
				return fallbackPoser.GetBlendedPose(skeletonAction, inputSource).boneRotations;
			}
			Debug.LogError("Skeleton Action is not bound, and you have not provided a fallback SkeletonPoser. Please create one to drive hand animation when no skeleton data is available.", this);
			return null;
		}

		public static Vector3 MirrorPosition(int boneIndex, Vector3 rawPosition)
		{
			if (boneIndex == 1 || IsMetacarpal(boneIndex))
			{
				rawPosition.Scale(new Vector3(-1f, 1f, 1f));
			}
			else if (boneIndex != 0)
			{
				rawPosition *= -1f;
			}
			return rawPosition;
		}

		public static Quaternion MirrorRotation(int boneIndex, Quaternion rawRotation)
		{
			if (boneIndex == 1)
			{
				rawRotation.y *= -1f;
				rawRotation.z *= -1f;
			}
			if (IsMetacarpal(boneIndex))
			{
				rawRotation = rightFlipAngle * rawRotation;
			}
			return rawRotation;
		}

		protected virtual void UpdatePose()
		{
			if (skeletonAction == null)
			{
				return;
			}
			Vector3 position = skeletonAction.GetLocalPosition();
			Quaternion quaternion = skeletonAction.GetLocalRotation();
			if (origin == null)
			{
				if (base.transform.parent != null)
				{
					position = base.transform.parent.TransformPoint(position);
					quaternion = base.transform.parent.rotation * quaternion;
				}
			}
			else
			{
				position = origin.TransformPoint(position);
				quaternion = origin.rotation * quaternion;
			}
			if (skeletonAction.poseChanged)
			{
				if (onTransformChanged != null)
				{
					onTransformChanged.Invoke(this, inputSource);
				}
				if (onTransformChangedEvent != null)
				{
					onTransformChangedEvent(this, inputSource);
				}
			}
			base.transform.position = position;
			base.transform.rotation = quaternion;
			if (onTransformUpdated != null)
			{
				onTransformUpdated.Invoke(this, inputSource);
			}
		}

		public void ForceToReferencePose(EVRSkeletalReferencePose referencePose)
		{
			bool flag = false;
			if (Application.isEditor && !Application.isPlaying)
			{
				flag = SteamVR.InitializeTemporarySession(true);
				Awake();
				skeletonAction.actionSet.Activate(SteamVR_Input_Sources.Any, 0, false);
				SteamVR_ActionSet_Manager.UpdateActionStates(true);
				skeletonAction.UpdateValueWithoutEvents();
			}
			if (!skeletonAction.active)
			{
				Debug.LogError("<b>[SteamVR Input]</b> Please turn on your " + inputSource.ToString() + " controller and ensure SteamVR is open.", this);
				return;
			}
			SteamVR_Utils.RigidTransform[] referenceTransforms = skeletonAction.GetReferenceTransforms(EVRSkeletalTransformSpace.Parent, referencePose);
			if (referenceTransforms == null || referenceTransforms.Length == 0)
			{
				Debug.LogError("<b>[SteamVR Input]</b> Unable to get the reference transform for " + inputSource.ToString() + ". Please make sure SteamVR is open and both controllers are connected.", this);
			}
			if (mirroring == MirrorType.LeftToRight || mirroring == MirrorType.RightToLeft)
			{
				for (int i = 0; i < referenceTransforms.Length; i++)
				{
					bones[i].localPosition = MirrorPosition(i, referenceTransforms[i].pos);
					bones[i].localRotation = MirrorRotation(i, referenceTransforms[i].rot);
				}
			}
			else
			{
				for (int j = 0; j < referenceTransforms.Length; j++)
				{
					bones[j].localPosition = referenceTransforms[j].pos;
					bones[j].localRotation = referenceTransforms[j].rot;
				}
			}
			if (flag)
			{
				SteamVR.ExitTemporarySession();
			}
		}

		protected static bool IsMetacarpal(int boneIndex)
		{
			if (boneIndex != 6 && boneIndex != 11 && boneIndex != 16 && boneIndex != 21)
			{
				return boneIndex == 2;
			}
			return true;
		}
	}
}
