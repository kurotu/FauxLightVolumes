
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

        public bool EnforceMinBrightness = false;

        [Range(0.0f, 1.0f)]
        public float MinBrightness = 0.0f;

        [SerializeField]
        private FauxLightVolumeInstance[] LightVolumes;

        private int MinBrightnessId = -1;

        void Start()
        {
            MinBrightnessId = VRCShader.PropertyToID("_MinBrightness");

            if (IsAvailableOnCurrentPlatform())
            {
                EnableAllInstances();
                if (EnforceMinBrightness)
                {
                    SetMinBrightness(MinBrightness);
                }
            }
            else
            {
                DisableAllInstances();
            }
        }

        public void Enable()
        {
            if (IsAvailableOnCurrentPlatform())
            {
                EnableAllInstances();
            }
        }

        public void Disable()
        {
            if (IsAvailableOnCurrentPlatform())
            {
                DisableAllInstances();
            }
        }

        public void SetMinBrightness(float minBrightness)
        {
            if (IsAvailableOnCurrentPlatform())
            {
                VRCShader.SetGlobalFloat(MinBrightnessId, minBrightness);
                EnforceMinBrightness = true;
                MinBrightness = minBrightness;
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
                lightVolume.Enable();
            }
        }

        private void DisableAllInstances()
        {
            foreach (var lightVolume in LightVolumes)
            {
                lightVolume.Disable();
            }
        }
    }
}
