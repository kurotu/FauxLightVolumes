
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

namespace FauxLightVolumes
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class FauxLightVolumeManager : UdonSharpBehaviour
    {
        public bool UseOnPC = false;
        public bool UseOnAndroid = true;
        public bool UseOnIOS = true;
        public float InitialGamma = 0.6f;
        public FauxLightVolumeCurveMode InitialCurveMode = FauxLightVolumeCurveMode.SCurve;

        [SerializeField]
        private FauxLightVolumeInstance[] LightVolumes;

        private float _gamma;
        private FauxLightVolumeCurveMode _curveMode;
        private int _cachedGammaPropertyID = -1;
        private int _cachedCurveModePropertyID = -1;

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
