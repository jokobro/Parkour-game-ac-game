using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance;

    [SerializeField] private AudioMixer audioMixer;
    private UIDocument UIDocument;

    private Slider musicSlider;
    private Slider sfxSlider;
    private Button musicDecreaseBtn, musicIncreaseBtn;
    private Button sfxDecreaseBtn, sfxIncreaseBtn;

    private Label resolutionLabel;
    private Button resolutionPrevBtn, resolutionNextBtn;
    private DropdownField qualityField;

    private List<Vector2Int> supportedResolutions = new List<Vector2Int>();
    private int currrentResolutionIndex = 0;


    private void Awake()
    {
        Instance = this;

        UIDocument = GetComponent<UIDocument>();
        var root = UIDocument.rootVisualElement;

        musicSlider = root.Q<Slider>("MusicSlider");
        sfxSlider = root.Q<Slider>("SFXSlider");

        musicDecreaseBtn = root.Q<Button>("MusicDecreaseBtn");
        musicIncreaseBtn = root.Q<Button>("MusicIncreaseBtn");
        sfxDecreaseBtn = root.Q<Button>("SFXDecreaseBtn");
        sfxIncreaseBtn = root.Q<Button>("SFXIncreaseBtn");

        resolutionLabel = root.Q<Label>("ResolutionLabel");
        resolutionPrevBtn = root.Q<Button>("ResolutionPrevBtn");
        resolutionNextBtn = root.Q<Button>("ResolutionNextBtn");

        qualityField = root.Q<DropdownField>("QualityDropDown");
    }

    private void Start()
    {
        SetupVolumeControls();
        SetupResolutionControls();
        SetupQualityControls();

        loadVolume();
    }

    private void SetupVolumeControls()
    {
        float volumeStep = 0.1f;

        musicSlider.RegisterValueChangedCallback(evt => UpdateMusicVolume(evt.newValue));
        sfxSlider.RegisterValueChangedCallback(evt => UpdateSFXVolume(evt.newValue));

        musicDecreaseBtn.clicked += () =>
        {
            musicSlider.value = Mathf.Clamp(musicSlider.value - volumeStep, 0f, 1f);
        };

        musicIncreaseBtn.clicked += () =>
        {
            musicSlider.value = Mathf.Clamp(musicSlider.value + volumeStep, 0f, 1f);
        };


        sfxDecreaseBtn.clicked += () =>
        {
            sfxSlider.value = Mathf.Clamp(sfxSlider.value - volumeStep, 0f, 1f);
        };

        sfxIncreaseBtn.clicked += () =>
        {
            sfxSlider.value = Mathf.Clamp(sfxSlider.value + volumeStep, 0f, 1f);
        };
    }

    private void SetupResolutionControls()
    {
        var availableResolutions = Screen.resolutions
         .Select(r => new Vector2Int(r.width, r.height))
            .Distinct()
            .OrderByDescending(r => r.x * r.y)
            .ToList();

        var standardResolutions = new List<Vector2Int>()
        {
            new Vector2Int(3840, 2160),
            new Vector2Int(2560, 1440),
            new Vector2Int(1920, 1080),
            new Vector2Int(1600, 900),
            new Vector2Int(1280, 720)
        };

        supportedResolutions = standardResolutions
            .Where(r => availableResolutions.Contains(r))
            .ToList();

        if (supportedResolutions.Count == 0)
        {
            supportedResolutions = availableResolutions.Take(5).ToList();
        }

        Vector2Int currentRes = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        currrentResolutionIndex = supportedResolutions.FindIndex(r => r == currentRes);
        if (currrentResolutionIndex == -1) currrentResolutionIndex = 0;

        UpdateResolutionDisplay();

        resolutionPrevBtn.clicked += () =>
        {
            currrentResolutionIndex = (currrentResolutionIndex - 1 + supportedResolutions.Count) % supportedResolutions.Count;
            ChangeResolution();
        };

        resolutionNextBtn.clicked += () =>
        {
            currrentResolutionIndex = (currrentResolutionIndex + 1) % supportedResolutions.Count;
            ChangeResolution();
        };

    }

    private void SetupQualityControls()
    {
        qualityField.choices = QualitySettings.names.ToList();
        qualityField.value = QualitySettings.names[QualitySettings.GetQualityLevel()];

        qualityField.RegisterValueChangedCallback(evt =>
        {
            int index = qualityField.choices.IndexOf(evt.newValue);
            SetQuality(index);
        });
    }

    private void ChangeResolution()
    {
        var targetRes = supportedResolutions[currrentResolutionIndex];

        var urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;

        if (urpAsset != null)
        {
            float baseWidth = 1920f;
            float baseHeight = 1080f;
            float scaleX = targetRes.x / baseWidth;
            float scaleY = targetRes.y / baseHeight;
            float uniformScale = Mathf.Min(scaleX, scaleY);

            urpAsset.renderScale = Mathf.Clamp(uniformScale, 0.25f, 2.0f);
        }
        Screen.SetResolution(targetRes.x, targetRes.y, FullScreenMode.FullScreenWindow);
        UpdateResolutionDisplay();

        Debug.Log($"Resolutie veranderd naar {targetRes.x}x{targetRes.y} met render scale {urpAsset?.renderScale}");
    }

    private void UpdateResolutionDisplay()
    {
        if (currrentResolutionIndex >= 0 && currrentResolutionIndex < supportedResolutions.Count)
        {
            var res = supportedResolutions[currrentResolutionIndex];
            resolutionLabel.text = $"{res.x} x {res.y}";
        }
    }

    private void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
    }

    public void UpdateMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
    }

    public void UpdateSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
    }

    public void SaveVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        PlayerPrefs.SetInt("ResolutionIndex", currrentResolutionIndex);
        PlayerPrefs.Save();
    }

    private void loadVolume()
    {
        float musicValue = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicSlider.value = musicValue;
        sfxSlider.value = sfxValue;

        UpdateMusicVolume(musicValue);
        UpdateSFXVolume(sfxValue);
        UpdateResolutionDisplay();
    }

    float LinearToDecibel(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
    }
}