using Unity.XR.OpenVR;
using UnityEngine;
using UnityEngine.Serialization;

namespace Valve.VR
{
    public class SteamVR_Settings : ScriptableObject
    {
        private static SteamVR_Settings _instance;

        public bool pauseGameWhenDashboardVisible = true;

        public bool lockPhysicsUpdateRateToRenderFrequency = true;

        [SerializeField] [FormerlySerializedAs("trackingSpace")]
        private ETrackingUniverseOrigin trackingSpaceOrigin = ETrackingUniverseOrigin.TrackingUniverseStanding;

        [Tooltip("Filename local to StreamingAssets/SteamVR/ folder")]
        public string actionsFilePath = "actions.json";

        [Tooltip("Path local to the directory the SteamVR folder as in")]
        public string steamVRInputPath = "SteamVR_Input";

        public SteamVR_UpdateModes inputUpdateMode = SteamVR_UpdateModes.OnUpdate;

        public SteamVR_UpdateModes poseUpdateMode = SteamVR_UpdateModes.OnPreCull;

        public bool activateFirstActionSetOnStart = true;

        [Tooltip("This is the app key the unity editor will use to identify your application. (can be \"steam.app.[appid]\" to persist bindings between editor steam)")]
        public string editorAppKey;

        [Tooltip("The SteamVR Plugin can automatically make sure VR is enabled in your player settings and if not, enable it.")]
        public bool autoEnableVR = true;

        [Space] [Tooltip("This determines if we use legacy mixed reality mode (3rd controller/tracker device connected) or the new input system mode (pose / input source)")]
        public bool legacyMixedRealityCamera = true;

        [Tooltip("[NON-LEGACY] This is the pose action that will be used for positioning a mixed reality camera if connected")]
        public SteamVR_Action_Pose mixedRealityCameraPose = SteamVR_Input.GetPoseAction("ExternalCamera");

        [Tooltip("[NON-LEGACY] This is the input source to check on the pose for the mixed reality camera")]
        public SteamVR_Input_Sources mixedRealityCameraInputSource = SteamVR_Input_Sources.Camera;

        [Tooltip("[NON-LEGACY] Auto enable mixed reality action set if file exists")]
        public bool mixedRealityActionSetAutoEnable = true;

        [Tooltip("[EDITOR ONLY] The (left) prefab to be used for showing previews while posing hands")]
        public GameObject previewHandLeft;

        [Tooltip("[EDITOR ONLY] The (right) prefab to be used for showing previews while posing hands")]
        public GameObject previewHandRight;

        private const string previewLeftDefaultAssetName = "vr_glove_left_model_slim";

        private const string previewRightDefaultAssetName = "vr_glove_right_model_slim";

        private const string defaultSettingsAssetName = "SteamVR_Settings";

        public static SteamVR_Settings instance
        {
            get
            {
                LoadInstance();
                return _instance;
            }
        }

        public ETrackingUniverseOrigin trackingSpace
        {
            get => trackingSpaceOrigin;
            set
            {
                trackingSpaceOrigin = value;
                if (SteamVR_Behaviour.isPlaying) SteamVR_Action_Pose.SetTrackingUniverseOrigin(trackingSpaceOrigin);
            }
        }

        public bool IsInputUpdateMode(SteamVR_UpdateModes tocheck)
        {
            return (inputUpdateMode & tocheck) == tocheck;
        }

        public bool IsPoseUpdateMode(SteamVR_UpdateModes tocheck)
        {
            return (poseUpdateMode & tocheck) == tocheck;
        }

        public static void VerifyScriptableObject()
        {
            LoadInstance();
        }

        private static void LoadInstance()
        {
            if (_instance == null)
            {
                _instance = Resources.Load<SteamVR_Settings>("SteamVR_Settings");
                if (_instance == null) _instance = CreateInstance<SteamVR_Settings>();
                SetDefaultsIfNeeded();
            }
        }

        public static void Save() { }

        private static void SetDefaultsIfNeeded()
        {
            if (string.IsNullOrEmpty(_instance.editorAppKey))
            {
                _instance.editorAppKey = SteamVR.GenerateAppKey();
                Debug.Log("<b>[SteamVR Setup]</b> Generated you an editor app key of: " + _instance.editorAppKey +
                          ". This lets the editor tell SteamVR what project this is. Has no effect on builds. This can be changed in Assets/SteamVR/Resources/SteamVR_Settings");
            }

            OpenVRSettings.GetSettings(true).ActionManifestFileRelativeFilePath = SteamVR_Input.GetActionsFilePath(true);
        }

        private static GameObject FindDefaultPreviewHand(string assetName)
        {
            return null;
        }
    }
}
