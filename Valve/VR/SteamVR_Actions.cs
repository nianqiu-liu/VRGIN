namespace Valve.VR
{
	public class SteamVR_Actions
	{
		private static SteamVR_Action_Boolean p_legacy_emulate_Grip_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_Grip_Touch;

		private static SteamVR_Action_Vector2 p_legacy_emulate_Grip_2D;

		private static SteamVR_Action_Boolean p_legacy_emulate_ApplicationMenu_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_ApplicationMenu_Touch;

		private static SteamVR_Action_Boolean p_legacy_emulate_A_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_A_Touch;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis0_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis0_Touch;

		private static SteamVR_Action_Vector2 p_legacy_emulate_Axis0_2D;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis1_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis1_Touch;

		private static SteamVR_Action_Vector2 p_legacy_emulate_Axis1_2D;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis2_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis2_Touch;

		private static SteamVR_Action_Vector2 p_legacy_emulate_Axis2_2D;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis3_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis3_Touch;

		private static SteamVR_Action_Vector2 p_legacy_emulate_Axis3_2D;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis4_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_Axis4_Touch;

		private static SteamVR_Action_Vector2 p_legacy_emulate_Axis4_2D;

		private static SteamVR_Action_Boolean p_legacy_emulate_System_Press;

		private static SteamVR_Action_Boolean p_legacy_emulate_System_Touch;

		private static SteamVR_Action_Single p_legacy_emulate_Axis1_1D;

		private static SteamVR_Action_Single p_legacy_emulate_Grip_1D;

		private static SteamVR_Action_Pose p_legacy_emulate_Pose;

		private static SteamVR_Action_Vibration p_legacy_emulate_Huptic;

		private static SteamVR_Input_ActionSet_legacy_emulate p_legacy_emulate;

		public static SteamVR_Action_Boolean legacy_emulate_Grip_Press => p_legacy_emulate_Grip_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Grip_Touch => p_legacy_emulate_Grip_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Vector2 legacy_emulate_Grip_2D => p_legacy_emulate_Grip_2D.GetCopy<SteamVR_Action_Vector2>();

		public static SteamVR_Action_Boolean legacy_emulate_ApplicationMenu_Press => p_legacy_emulate_ApplicationMenu_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_ApplicationMenu_Touch => p_legacy_emulate_ApplicationMenu_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_A_Press => p_legacy_emulate_A_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_A_Touch => p_legacy_emulate_A_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis0_Press => p_legacy_emulate_Axis0_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis0_Touch => p_legacy_emulate_Axis0_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Vector2 legacy_emulate_Axis0_2D => p_legacy_emulate_Axis0_2D.GetCopy<SteamVR_Action_Vector2>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis1_Press => p_legacy_emulate_Axis1_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis1_Touch => p_legacy_emulate_Axis1_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Vector2 legacy_emulate_Axis1_2D => p_legacy_emulate_Axis1_2D.GetCopy<SteamVR_Action_Vector2>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis2_Press => p_legacy_emulate_Axis2_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis2_Touch => p_legacy_emulate_Axis2_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Vector2 legacy_emulate_Axis2_2D => p_legacy_emulate_Axis2_2D.GetCopy<SteamVR_Action_Vector2>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis3_Press => p_legacy_emulate_Axis3_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis3_Touch => p_legacy_emulate_Axis3_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Vector2 legacy_emulate_Axis3_2D => p_legacy_emulate_Axis3_2D.GetCopy<SteamVR_Action_Vector2>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis4_Press => p_legacy_emulate_Axis4_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_Axis4_Touch => p_legacy_emulate_Axis4_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Vector2 legacy_emulate_Axis4_2D => p_legacy_emulate_Axis4_2D.GetCopy<SteamVR_Action_Vector2>();

		public static SteamVR_Action_Boolean legacy_emulate_System_Press => p_legacy_emulate_System_Press.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Boolean legacy_emulate_System_Touch => p_legacy_emulate_System_Touch.GetCopy<SteamVR_Action_Boolean>();

		public static SteamVR_Action_Single legacy_emulate_Axis1_1D => p_legacy_emulate_Axis1_1D.GetCopy<SteamVR_Action_Single>();

		public static SteamVR_Action_Single legacy_emulate_Grip_1D => p_legacy_emulate_Grip_1D.GetCopy<SteamVR_Action_Single>();

		public static SteamVR_Action_Pose legacy_emulate_Pose => p_legacy_emulate_Pose.GetCopy<SteamVR_Action_Pose>();

		public static SteamVR_Action_Vibration legacy_emulate_Huptic => p_legacy_emulate_Huptic.GetCopy<SteamVR_Action_Vibration>();

		public static SteamVR_Input_ActionSet_legacy_emulate legacy_emulate => p_legacy_emulate.GetCopy<SteamVR_Input_ActionSet_legacy_emulate>();

		private static void InitializeActionArrays()
		{
			SteamVR_Input.actions = new SteamVR_Action[28]
			{
				legacy_emulate_Grip_Press, legacy_emulate_Grip_Touch, legacy_emulate_Grip_2D, legacy_emulate_ApplicationMenu_Press, legacy_emulate_ApplicationMenu_Touch, legacy_emulate_A_Press, legacy_emulate_A_Touch, legacy_emulate_Axis0_Press, legacy_emulate_Axis0_Touch, legacy_emulate_Axis0_2D,
				legacy_emulate_Axis1_Press, legacy_emulate_Axis1_Touch, legacy_emulate_Axis1_2D, legacy_emulate_Axis2_Press, legacy_emulate_Axis2_Touch, legacy_emulate_Axis2_2D, legacy_emulate_Axis3_Press, legacy_emulate_Axis3_Touch, legacy_emulate_Axis3_2D, legacy_emulate_Axis4_Press,
				legacy_emulate_Axis4_Touch, legacy_emulate_Axis4_2D, legacy_emulate_System_Press, legacy_emulate_System_Touch, legacy_emulate_Axis1_1D, legacy_emulate_Grip_1D, legacy_emulate_Pose, legacy_emulate_Huptic
			};
			SteamVR_Input.actionsIn = new ISteamVR_Action_In[27]
			{
				legacy_emulate_Grip_Press, legacy_emulate_Grip_Touch, legacy_emulate_Grip_2D, legacy_emulate_ApplicationMenu_Press, legacy_emulate_ApplicationMenu_Touch, legacy_emulate_A_Press, legacy_emulate_A_Touch, legacy_emulate_Axis0_Press, legacy_emulate_Axis0_Touch, legacy_emulate_Axis0_2D,
				legacy_emulate_Axis1_Press, legacy_emulate_Axis1_Touch, legacy_emulate_Axis1_2D, legacy_emulate_Axis2_Press, legacy_emulate_Axis2_Touch, legacy_emulate_Axis2_2D, legacy_emulate_Axis3_Press, legacy_emulate_Axis3_Touch, legacy_emulate_Axis3_2D, legacy_emulate_Axis4_Press,
				legacy_emulate_Axis4_Touch, legacy_emulate_Axis4_2D, legacy_emulate_System_Press, legacy_emulate_System_Touch, legacy_emulate_Axis1_1D, legacy_emulate_Grip_1D, legacy_emulate_Pose
			};
			SteamVR_Input.actionsOut = new ISteamVR_Action_Out[1] { legacy_emulate_Huptic };
			SteamVR_Input.actionsVibration = new SteamVR_Action_Vibration[1] { legacy_emulate_Huptic };
			SteamVR_Input.actionsPose = new SteamVR_Action_Pose[1] { legacy_emulate_Pose };
			SteamVR_Input.actionsBoolean = new SteamVR_Action_Boolean[18]
			{
				legacy_emulate_Grip_Press, legacy_emulate_Grip_Touch, legacy_emulate_ApplicationMenu_Press, legacy_emulate_ApplicationMenu_Touch, legacy_emulate_A_Press, legacy_emulate_A_Touch, legacy_emulate_Axis0_Press, legacy_emulate_Axis0_Touch, legacy_emulate_Axis1_Press, legacy_emulate_Axis1_Touch,
				legacy_emulate_Axis2_Press, legacy_emulate_Axis2_Touch, legacy_emulate_Axis3_Press, legacy_emulate_Axis3_Touch, legacy_emulate_Axis4_Press, legacy_emulate_Axis4_Touch, legacy_emulate_System_Press, legacy_emulate_System_Touch
			};
			SteamVR_Input.actionsSingle = new SteamVR_Action_Single[2] { legacy_emulate_Axis1_1D, legacy_emulate_Grip_1D };
			SteamVR_Input.actionsVector2 = new SteamVR_Action_Vector2[6] { legacy_emulate_Grip_2D, legacy_emulate_Axis0_2D, legacy_emulate_Axis1_2D, legacy_emulate_Axis2_2D, legacy_emulate_Axis3_2D, legacy_emulate_Axis4_2D };
			SteamVR_Input.actionsVector3 = new SteamVR_Action_Vector3[0];
			SteamVR_Input.actionsSkeleton = new SteamVR_Action_Skeleton[0];
			SteamVR_Input.actionsNonPoseNonSkeletonIn = new ISteamVR_Action_In[26]
			{
				legacy_emulate_Grip_Press, legacy_emulate_Grip_Touch, legacy_emulate_Grip_2D, legacy_emulate_ApplicationMenu_Press, legacy_emulate_ApplicationMenu_Touch, legacy_emulate_A_Press, legacy_emulate_A_Touch, legacy_emulate_Axis0_Press, legacy_emulate_Axis0_Touch, legacy_emulate_Axis0_2D,
				legacy_emulate_Axis1_Press, legacy_emulate_Axis1_Touch, legacy_emulate_Axis1_2D, legacy_emulate_Axis2_Press, legacy_emulate_Axis2_Touch, legacy_emulate_Axis2_2D, legacy_emulate_Axis3_Press, legacy_emulate_Axis3_Touch, legacy_emulate_Axis3_2D, legacy_emulate_Axis4_Press,
				legacy_emulate_Axis4_Touch, legacy_emulate_Axis4_2D, legacy_emulate_System_Press, legacy_emulate_System_Touch, legacy_emulate_Axis1_1D, legacy_emulate_Grip_1D
			};
		}

		private static void PreInitActions()
		{
			p_legacy_emulate_Grip_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Grip_Press");
			p_legacy_emulate_Grip_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Grip_Touch");
			p_legacy_emulate_Grip_2D = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/legacy_emulate/in/Grip_2D");
			p_legacy_emulate_ApplicationMenu_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/ApplicationMenu_Press");
			p_legacy_emulate_ApplicationMenu_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/ApplicationMenu_Touch");
			p_legacy_emulate_A_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/A_Press");
			p_legacy_emulate_A_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/A_Touch");
			p_legacy_emulate_Axis0_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis0_Press");
			p_legacy_emulate_Axis0_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis0_Touch");
			p_legacy_emulate_Axis0_2D = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/legacy_emulate/in/Axis0_2D");
			p_legacy_emulate_Axis1_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis1_Press");
			p_legacy_emulate_Axis1_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis1_Touch");
			p_legacy_emulate_Axis1_2D = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/legacy_emulate/in/Axis1_2D");
			p_legacy_emulate_Axis2_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis2_Press");
			p_legacy_emulate_Axis2_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis2_Touch");
			p_legacy_emulate_Axis2_2D = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/legacy_emulate/in/Axis2_2D");
			p_legacy_emulate_Axis3_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis3_Press");
			p_legacy_emulate_Axis3_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis3_Touch");
			p_legacy_emulate_Axis3_2D = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/legacy_emulate/in/Axis3_2D");
			p_legacy_emulate_Axis4_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis4_Press");
			p_legacy_emulate_Axis4_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/Axis4_Touch");
			p_legacy_emulate_Axis4_2D = SteamVR_Action.Create<SteamVR_Action_Vector2>("/actions/legacy_emulate/in/Axis4_2D");
			p_legacy_emulate_System_Press = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/System_Press");
			p_legacy_emulate_System_Touch = SteamVR_Action.Create<SteamVR_Action_Boolean>("/actions/legacy_emulate/in/System_Touch");
			p_legacy_emulate_Axis1_1D = SteamVR_Action.Create<SteamVR_Action_Single>("/actions/legacy_emulate/in/Axis1_1D");
			p_legacy_emulate_Grip_1D = SteamVR_Action.Create<SteamVR_Action_Single>("/actions/legacy_emulate/in/Grip_1D");
			p_legacy_emulate_Pose = SteamVR_Action.Create<SteamVR_Action_Pose>("/actions/legacy_emulate/in/Pose");
			p_legacy_emulate_Huptic = SteamVR_Action.Create<SteamVR_Action_Vibration>("/actions/legacy_emulate/out/Huptic");
		}

		private static void StartPreInitActionSets()
		{
			p_legacy_emulate = SteamVR_ActionSet.Create<SteamVR_Input_ActionSet_legacy_emulate>("/actions/legacy_emulate");
			SteamVR_Input.actionSets = new SteamVR_ActionSet[1] { legacy_emulate };
		}

		public static void PreInitialize()
		{
			StartPreInitActionSets();
			SteamVR_Input.PreinitializeActionSetDictionaries();
			PreInitActions();
			InitializeActionArrays();
			SteamVR_Input.PreinitializeActionDictionaries();
			SteamVR_Input.PreinitializeFinishActionSets();
		}
	}
}
