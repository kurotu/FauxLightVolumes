
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

        [SerializeField]
        private FauxLightVolumeInstance[] LightVolumes;

        void Start()
        {
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
