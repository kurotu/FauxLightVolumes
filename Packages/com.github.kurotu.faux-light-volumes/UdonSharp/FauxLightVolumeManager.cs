using UdonSharp;
using UnityEngine;
using VRC.SDK3.Rendering;
using VRC.SDKBase;
using VRC.Udon.Common.Interfaces;

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
#endif
        public FauxLightVolumeCurveMode InitialCurveMode = FauxLightVolumeCurveMode.SCurve;

#if !COMPILER_UDONSHARP
        [LocalizedLabel]
        [Range(0.1f, 1.0f)]
#endif
        public float InitialGamma = 0.6f;

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

        [SerializeField]
        private CustomRenderTexture ErrorDetectorTexture;

        private float _gamma;
        private FauxLightVolumeCurveMode _curveMode;
        private float _outputScale;
        private int _cachedGammaPropertyID = -1;
        private int _cachedCurveModePropertyID = -1;
        private int _cachedOutputScalePropertyID = -1;
        private bool _hasShaderError = false;

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
                return UseOnAndroid && !(_hasShaderError);
#elif UNITY_IOS
                return UseOnIOS && !(_hasShaderError);
#else
                return UseOnPC && !(_hasShaderError);
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

            if (ErrorDetectorTexture != null)
            {
                ErrorDetectorTexture.Update();
                SendCustomEventDelayedFrames(nameof(ReadbackErrorDetectorTexture), 1);
            }
            else
            {
                Debug.LogError("ErrorDetectorTexture is not assigned. FauxLightVolumeManager cannot detect shader compilation errors.");
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

        public void ReadbackErrorDetectorTexture()
        {
            Debug.Log("FauxLightVolumeManager: ReadbackErrorDetectorTexture");
            if (ErrorDetectorTexture != null)
            {
                VRCAsyncGPUReadback.Request(ErrorDetectorTexture, 0, (IUdonEventReceiver)this);
            }
        }

        public override void OnAsyncGpuReadbackComplete(VRCAsyncGPUReadbackRequest request)
        {
            Debug.Log("FauxLightVolumeManager: OnAsyncGpuReadbackComplete");
            if (request.hasError)
            {
                Debug.LogError("GPU readback error!");
                return;
            }
            else
            {
                var px = new Color32[ErrorDetectorTexture.width * ErrorDetectorTexture.height];
                Debug.Log($"FauxLightVolumeManager: Readback: {request.TryGetData(px)}");
                var pix = px[0];
                Debug.Log($"FauxLightVolumeManager: Readback pixel RGBA = {pix.r}, {pix.g}, {pix.b}, {pix.a}");
                var shaderIsWorking = px[0].g == 255 && px[0].r == 0 && px[0].b == 0;
                _hasShaderError = !shaderIsWorking;
                if (_hasShaderError)
                {
                    Debug.Log($"FauxLightVolumeManager: Shader compilation error detected: {_hasShaderError}");
                    SetAllLightVolumesActive(false);
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
