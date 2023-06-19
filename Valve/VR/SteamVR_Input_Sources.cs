using System.ComponentModel;

namespace Valve.VR
{
	public enum SteamVR_Input_Sources
	{
		[Description("/unrestricted")]
		Any = 0,
		[Description("/user/hand/left")]
		LeftHand = 1,
		[Description("/user/hand/right")]
		RightHand = 2,
		[Description("/user/foot/left")]
		LeftFoot = 3,
		[Description("/user/foot/right")]
		RightFoot = 4,
		[Description("/user/shoulder/left")]
		LeftShoulder = 5,
		[Description("/user/shoulder/right")]
		RightShoulder = 6,
		[Description("/user/waist")]
		Waist = 7,
		[Description("/user/chest")]
		Chest = 8,
		[Description("/user/head")]
		Head = 9,
		[Description("/user/gamepad")]
		Gamepad = 10,
		[Description("/user/camera")]
		Camera = 11,
		[Description("/user/keyboard")]
		Keyboard = 12,
		[Description("/user/treadmill")]
		Treadmill = 13
	}
}
