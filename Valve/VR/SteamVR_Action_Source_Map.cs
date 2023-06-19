using System.Globalization;
using UnityEngine;

namespace Valve.VR
{
    public abstract class SteamVR_Action_Source_Map<SourceElement> : SteamVR_Action_Source_Map where SourceElement : SteamVR_Action_Source, new()
    {
        protected SourceElement[] sources = new SourceElement[SteamVR_Input_Source.numSources];

        public SourceElement this[SteamVR_Input_Sources inputSource] => GetSourceElementForIndexer(inputSource);

        protected virtual void OnAccessSource(SteamVR_Input_Sources inputSource) { }

        public override void Initialize()
        {
            base.Initialize();
            for (var i = 0; i < sources.Length; i++)
            {
                if (sources[i] != null) sources[i].Initialize();
            }
        }

        protected override void PreinitializeMap(SteamVR_Input_Sources inputSource, SteamVR_Action wrappingAction)
        {
            sources[(int)inputSource] = new SourceElement();
            sources[(int)inputSource].Preinitialize(wrappingAction, inputSource);
        }

        protected virtual SourceElement GetSourceElementForIndexer(SteamVR_Input_Sources inputSource)
        {
            OnAccessSource(inputSource);
            return sources[(int)inputSource];
        }
    }

    public abstract class SteamVR_Action_Source_Map
    {
        public SteamVR_Action action;

        private static string inLowered = "IN".ToLower(CultureInfo.CurrentCulture);

        private static string outLowered = "OUT".ToLower(CultureInfo.CurrentCulture);

        public string fullPath { get; protected set; }

        public ulong handle { get; protected set; }

        public SteamVR_ActionSet actionSet { get; protected set; }

        public SteamVR_ActionDirections direction { get; protected set; }

        public virtual void PreInitialize(SteamVR_Action wrappingAction, string actionPath, bool throwErrors = true)
        {
            fullPath = actionPath;
            action = wrappingAction;
            actionSet = SteamVR_Input.GetActionSetFromPath(GetActionSetPath());
            direction = GetActionDirection();
            var allSources = SteamVR_Input_Source.GetAllSources();
            for (var i = 0; i < allSources.Length; i++) PreinitializeMap(allSources[i], wrappingAction);
        }

        protected abstract void PreinitializeMap(SteamVR_Input_Sources inputSource, SteamVR_Action wrappingAction);

        public virtual void Initialize()
        {
            var pHandle = 0uL;
            var actionHandle = OpenVR.Input.GetActionHandle(fullPath.ToLowerInvariant(), ref pHandle);
            handle = pHandle;
            if (actionHandle != 0) Debug.LogError("<b>[SteamVR]</b> GetActionHandle (" + fullPath.ToLowerInvariant() + ") error: " + actionHandle);
        }

        private string GetActionSetPath()
        {
            var startIndex = fullPath.IndexOf('/', 1) + 1;
            var length = fullPath.IndexOf('/', startIndex);
            return fullPath.Substring(0, length);
        }

        private SteamVR_ActionDirections GetActionDirection()
        {
            var startIndex = fullPath.IndexOf('/', 1) + 1;
            var num = fullPath.IndexOf('/', startIndex);
            var length = fullPath.IndexOf('/', num + 1) - num - 1;
            var text = fullPath.Substring(num + 1, length);
            if (text == inLowered) return SteamVR_ActionDirections.In;
            if (text == outLowered) return SteamVR_ActionDirections.Out;
            Debug.LogError("Could not find match for direction: " + text);
            return SteamVR_ActionDirections.In;
        }
    }
}
