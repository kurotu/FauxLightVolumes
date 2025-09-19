
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
        public float Gamma = 0.6f;

        [SerializeField]
        private FauxLightVolumeInstance[] LightVolumes;

        private int _cachedGammaPropertyID = -1;

        void Start()
        {
            _cachedGammaPropertyID = VRCShader.PropertyToID("_Udon_LVGamma");
            SetGamma(Gamma);
            if (IsAvailableOnCurrentPlatform())
            {
                EnableAllInstances();
            }
            else
            {
                DisableAllInstances();
            }
        }

        void OnEnable()
        {
            if (IsAvailableOnCurrentPlatform())
            {
                EnableAllInstances();
            }
        }

        void OnDisable()
        {
            if (IsAvailableOnCurrentPlatform())
            {
                DisableAllInstances();
            }
        }

        public bool IsAvailableOnCurrentPlatform()
        {
#if UNITY_ANDROID
            return UseOnAndroid;
#elif UNITY_IOS
            return UseOnIOS;
#else
            return UseOnPC;
#endif
        }

        public void SetGamma(float gamma)
        {
            Gamma = gamma;
            VRCShader.SetGlobalFloat(_cachedGammaPropertyID, Gamma);
        }

        private void EnableAllInstances()
        {
            foreach (var lightVolume in LightVolumes)
            {
                if (lightVolume.gameObject != null)
                {
                    lightVolume.gameObject.SetActive(true);
                }
            }
        }

        private void DisableAllInstances()
        {
            foreach (var lightVolume in LightVolumes)
            {
                if (lightVolume.gameObject != null)
                {
                    lightVolume.gameObject.SetActive(false);
                }
            }
        }
    }
}
