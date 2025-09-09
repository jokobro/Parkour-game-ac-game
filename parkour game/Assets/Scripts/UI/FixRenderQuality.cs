using UnityEngine;
using UnityEngine.Rendering.Universal;

[System.Serializable]
public class FixRenderQuality : MonoBehaviour
{
    [Header("Fix Render Quality - Run in Editor")]
    [SerializeField] private bool autoFixOnStart = true;

    void Start()
    {
        if (autoFixOnStart)
        {
            FixAllQualitySettings();
        }
    }

    [ContextMenu("Fix All Quality Settings")]
    public void FixAllQualitySettings()
    {
        FixURPRenderScale();
        FixAntiAliasing();
        FixTextureQuality();
        FixCameraSettings();
    }

    void FixURPRenderScale()
    {   // Find URP Asset
        var urpAsset = UniversalRenderPipeline.asset;
        if (urpAsset != null)
        {
            // Use reflection to set render scale (Unity 6 compatible)
            var renderScaleProperty = typeof(UniversalRenderPipelineAsset).GetField("m_RenderScale",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (renderScaleProperty != null)
            {
                renderScaleProperty.SetValue(urpAsset, 1.0f);
            }
        }
    }

    void FixAntiAliasing()
    {   // Set Quality Settings
        QualitySettings.antiAliasing = 4; // 4x MSAA
    }

    void FixTextureQuality()
    {   // Set texture quality to full resolution
        QualitySettings.globalTextureMipmapLimit = 0;
        QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
    }

    void FixCameraSettings()
    {
        // Find Main Camera
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.allowMSAA = true;
            mainCam.allowHDR = true;
        }
    }
}