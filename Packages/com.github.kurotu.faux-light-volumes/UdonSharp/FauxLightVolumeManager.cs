
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

        [SerializeField]
        private FauxLightVolumeInstance[] LightVolumes;

        private float _gamma;
        private int _cachedGammaPropertyID = -1;

        public float Gamma
        {
            get => _gamma;
            set {
                _gamma = value;
                if (_cachedGammaPropertyID == -1)
                {
                    _cachedGammaPropertyID = VRCShader.PropertyToID("_Udon_LVGamma");
                }
                VRCShader.SetGlobalFloat(_cachedGammaPropertyID, _gamma);
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
}
