using UnityEngine;
using Valve.VR;

internal class Nop_Action_Vector2 : ISteamVR_Action_Vector2, ISteamVR_Action_In_Source, ISteamVR_Action_Source
{
    public Vector2 axis => Vector2.zero;

    public Vector2 lastAxis => Vector2.zero;

    public Vector2 delta => Vector2.zero;

    public Vector2 lastDelta => Vector2.zero;

    public bool changed => false;

    public bool lastChanged => false;

    public float changedTime => 0f;

    public float updateTime => 0f;

    public ulong activeOrigin => 0uL;

    public ulong lastActiveOrigin => 0uL;

    public SteamVR_Input_Sources activeDevice => SteamVR_Input_Sources.Any;

    public uint trackedDeviceIndex => 0u;

    public string renderModelComponentName => null;

    public string localizedOriginName => null;

    public bool active => false;

    public bool activeBinding => false;

    public bool lastActive => false;

    public bool lastActiveBinding => false;

    public string fullPath => null;

    public ulong handle => 0uL;

    public SteamVR_ActionSet actionSet => null;

    public SteamVR_ActionDirections direction => SteamVR_ActionDirections.In;
}
