using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace OpenLife
{
    /// <summary>
    /// Runtime graphics baseline for OpenLife. Scales quality for desktop and Android
    /// without changing simulation, save data, navigation, or build-mode systems.
    /// Real character and furniture prefabs remain content-swappable.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GraphicsQualityProfile : MonoBehaviour
    {
        [SerializeField] bool allowAutomaticQuality = true;
        static GraphicsQualityProfile instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            if (instance != null) return;
            var go = new GameObject("OpenLife_Graphics");
            DontDestroyOnLoad(go);
            instance = go.AddComponent<GraphicsQualityProfile>();
        }

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;
            DontDestroyOnLoad(gameObject);
            ApplyQuality();
            ApplyEnvironment();
        }

        void ApplyQuality()
        {
            bool mobile = Application.isMobilePlatform || SystemInfo.deviceType == DeviceType.Handheld;
            int memory = SystemInfo.systemMemorySize;
            int gpuMemory = SystemInfo.graphicsMemorySize;
            bool lowEnd = mobile && (memory < 4000 || (gpuMemory > 0 && gpuMemory < 1024));

            if (allowAutomaticQuality)
            {
                QualitySettings.vSyncCount = mobile ? 0 : 1;
                Application.targetFrameRate = mobile ? (lowEnd ? 30 : 45) : 60;
                QualitySettings.antiAliasing = mobile ? (lowEnd ? 0 : 2) : 4;
                QualitySettings.shadows = lowEnd ? ShadowQuality.HardOnly : ShadowQuality.All;
                QualitySettings.shadowResolution = mobile ? ShadowResolution.Medium : ShadowResolution.High;
                QualitySettings.shadowDistance = mobile ? (lowEnd ? 22f : 38f) : 75f;
                QualitySettings.shadowCascades = mobile ? 1 : 2;
                QualitySettings.pixelLightCount = mobile ? 2 : 4;
                QualitySettings.lodBias = lowEnd ? 0.8f : (mobile ? 1.2f : 2f);
                QualitySettings.maximumLODLevel = lowEnd ? 1 : 0;
                QualitySettings.streamingMipmapsActive = true;
                QualitySettings.streamingMipmapsMemoryBudget = mobile ? 192f : 512f;
            }

            var camera = Camera.main;
            if (camera != null)
            {
                camera.allowHDR = !lowEnd;
                camera.allowMSAA = !lowEnd;
                camera.farClipPlane = mobile ? 180f : 350f;
                camera.fieldOfView = 48f;
                var data = camera.GetUniversalAdditionalCameraData();
                data.renderPostProcessing = !lowEnd;
                data.antialiasing = mobile
                    ? (lowEnd ? AntialiasingMode.None : AntialiasingMode.FastApproximateAntialiasing)
                    : AntialiasingMode.SubpixelMorphologicalAntialiasing;
                data.antialiasingQuality = AntialiasingQuality.High;
            }
        }

        void ApplyEnvironment()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.79f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.48f, 0.48f, 0.46f);
            RenderSettings.ambientGroundColor = new Color(0.22f, 0.2f, 0.18f);
            RenderSettings.reflectionIntensity = 0.8f;
            RenderSettings.defaultReflectionResolution = 128;

            var camera = Camera.main;
            if (camera == null) return;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;

            var volumeObject = new GameObject("OpenLife_GlobalPostProcessing");
            volumeObject.transform.SetParent(transform, false);
            var volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = -10;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();

            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(1.15f);
            bloom.intensity.Override(0.18f);
            bloom.scatter.Override(0.55f);

            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(0.05f);
            color.contrast.Override(12f);
            color.saturation.Override(8f);

            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.18f);
            vignette.smoothness.Override(0.35f);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);

            volume.sharedProfile = profile;
        }
    }
}
