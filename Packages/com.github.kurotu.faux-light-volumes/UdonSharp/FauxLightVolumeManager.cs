using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FauxLightVolumes
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FauxLightVolumeManager : FauxLightVolumeUdonSharpComponent
    {
#if !COMPILER_UDONSHARP
        [LocalizedLabel]
#endif
        public bool UseOnPC = false;

#if !COMPILER_UDONSHARP
        [LocalizedLabel]
#endif
        public bool UseOnAndroid = true;

#if !COMPILER_UDONSHARP
        [LocalizedLabel]
#endif
        public bool UseOnIOS = true;

#if !COMPILER_UDONSHARP
        [LocalizedLabel]
        [Range(0.1f, 1.0f)]
#endif
        public float InitialGamma = 0.6f;

#if !COMPILER_UDONSHARP
        [LocalizedLabel]
#endif
        public FauxLightVolumeCurveMode InitialCurveMode = FauxLightVolumeCurveMode.SCurve;

#if !COMPILER_UDONSHARP
        [LocalizedLabel]
        [Range(0.0f, 5.0f)]
#endif
        public float InitialOutputScale = 1.0f;

        [SerializeField]
#if !COMPILER_UDONSHARP
        [LocalizedLabel]
#endif
        private FauxLightVolumeInstance[] LightVolumes;

        private float _gamma;
        private FauxLightVolumeCurveMode _curveMode;
        private float _outputScale;
        private int _cachedGammaPropertyID = -1;
        private int _cachedCurveModePropertyID = -1;
        private int _cachedOutputScalePropertyID = -1;

        public float Gamma
        {
            get => _gamma;
            set
            {
                _gamma = value;
                if (_cachedGammaPropertyID == -1)
                {
                    _cachedGammaPropertyID = VRCShader.PropertyToID("_Udon_FauxLV_Gamma");
                }
                VRCShader.SetGlobalFloat(_cachedGammaPropertyID, _gamma);
            }
        }

        public FauxLightVolumeCurveMode CurveMode
        {
            get => _curveMode;
            set
            {
                _curveMode = value;
                if (_cachedCurveModePropertyID == -1)
                {
                    _cachedCurveModePropertyID = VRCShader.PropertyToID("_Udon_FauxLV_CurveMode");
                }
                var intValue = (int)_curveMode;
                VRCShader.SetGlobalFloat(_cachedCurveModePropertyID, intValue);
            }
        }

        public float OutputScale
        {
            get => _outputScale;
            set
            {
                _outputScale = value;
                if (_cachedOutputScalePropertyID == -1)
                {
                    _cachedOutputScalePropertyID = VRCShader.PropertyToID("_Udon_FauxLV_OutputScale");
                }
                // Negative values are treated as 1 in shader; no need to clamp here unless you want to avoid confusion.
                VRCShader.SetGlobalFloat(_cachedOutputScalePropertyID, _outputScale);
            }
        }

        public bool IsAvailableOnCurrentPlatform
        {
            get
            {
#if UNITY_ANDROID
                return UseOnAndroid;
#elif UNITY_IOS
                return UseOnIOS;
#else
                return UseOnPC;
#endif
            }
        }

        void Start()
        {
            Gamma = InitialGamma;
            CurveMode = InitialCurveMode;
            OutputScale = InitialOutputScale;
            if (IsAvailableOnCurrentPlatform)
            {
                SetAllLightVolumesActive(true);
            }
            else
            {
                SetAllLightVolumesActive(false);
            }
        }

        void OnEnable()
        {
            if (IsAvailableOnCurrentPlatform)
            {
                SetAllLightVolumesActive(true);
            }
        }

        void OnDisable()
        {
            if (IsAvailableOnCurrentPlatform)
            {
                SetAllLightVolumesActive(false);
            }
        }

        private void SetAllLightVolumesActive(bool value)
        {
            foreach (var lightVolume in LightVolumes)
            {
                if (lightVolume.gameObject != null)
                {
                    lightVolume.gameObject.SetActive(value);
                }
            }
        }
    }

    public enum FauxLightVolumeCurveMode
    {
        SCurve = 0,
        StandardGamma = 1,
        InverseGamma = 2
    }
}
