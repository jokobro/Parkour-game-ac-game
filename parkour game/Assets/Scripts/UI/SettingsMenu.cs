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
    private int currentResolutionIndex = 0;

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
        SetupResponsiveUI();

        LoadSettings();
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
        // Haal beschikbare resoluties op
        var availableResolutions = Screen.resolutions
            .Select(r => new Vector2Int(r.width, r.height))
            .Distinct()
            .OrderByDescending(r => r.x * r.y)
            .ToList();

        // Standaard resoluties die we ondersteunen
        var standardResolutions = new List<Vector2Int>()
        {
            new Vector2Int(3840, 2160), // 4K
            new Vector2Int(2560, 1440), // 1440p
            new Vector2Int(1920, 1080), // 1080p
            new Vector2Int(1600, 900),  // 900p
            new Vector2Int(1280, 720)   // 720p
        };

        // Filter op beschikbare resoluties
        supportedResolutions = standardResolutions
            .Where(r => availableResolutions.Contains(r))
            .ToList();

        if (supportedResolutions.Count == 0)
        {
            supportedResolutions = availableResolutions.Take(5).ToList();
        }

        // Vind huidige resolutie
        Vector2Int currentRes = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        currentResolutionIndex = supportedResolutions.FindIndex(r => r == currentRes);
        if (currentResolutionIndex == -1) currentResolutionIndex = 0;

        UpdateResolutionDisplay();

        // Button events
        resolutionPrevBtn.clicked += () =>
        {
            currentResolutionIndex = (currentResolutionIndex - 1 + supportedResolutions.Count) % supportedResolutions.Count;
            ChangeResolution();
        };

        resolutionNextBtn.clicked += () =>
        {
            currentResolutionIndex = (currentResolutionIndex + 1) % supportedResolutions.Count;
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

    private void SetupResponsiveUI()
    {
        // Automatische UI scaling voor AC Syndicate-achtig gedrag
        var root = UIDocument.rootVisualElement;

        // Definieer button layouts als percentages
        var buttonLayouts = new Dictionary<string, ButtonLayout>()
        {
            {"ContinueButton", new ButtonLayout(1.04f, 29.54f, 17.81f, 5.37f)},
            {"OptionsButton", new ButtonLayout(1.20f, 43.61f, 17.81f, 5.37f)},
            {"QuitButton", new ButtonLayout(0.52f, 92.13f, 17.81f, 5.37f)},
            // Voeg hier meer buttons toe als je ze hebt
        };

        // Pas responsive layout toe op alle gedefinieerde buttons
        foreach (var layout in buttonLayouts)
        {
            var button = root.Q<Button>(layout.Key);
            if (button != null)
            {
                ApplyResponsiveLayout(button, layout.Value);
            }
        }
    }

    private void ApplyResponsiveLayout(VisualElement element, ButtonLayout layout)
    {
        element.style.position = Position.Absolute;
        element.style.left = Length.Percent(layout.leftPercent);
        element.style.top = Length.Percent(layout.topPercent);
        element.style.width = Length.Percent(layout.widthPercent);
        element.style.height = Length.Percent(layout.heightPercent);
        element.style.fontSize = Length.Percent(4.35f); // Responsive font size
    }

    private void ChangeResolution()
    {
        var targetRes = supportedResolutions[currentResolutionIndex];

        // AC Syndicate-achtige resolutie scaling: render scale op 1.0 houden
        var urpAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        if (urpAsset != null)
        {
            urpAsset.renderScale = 1.0f;
        }

        // Verander alleen de screen resolutie (geen camera aanpassingen)
        Screen.SetResolution(targetRes.x, targetRes.y, FullScreenMode.FullScreenWindow);

        UpdateResolutionDisplay();

        Debug.Log($"Resolutie veranderd naar {targetRes.x}x{targetRes.y} - AC Syndicate style scaling");
    }

    private void UpdateResolutionDisplay()
    {
        if (currentResolutionIndex >= 0 && currentResolutionIndex < supportedResolutions.Count)
        {
            var res = supportedResolutions[currentResolutionIndex];
            resolutionLabel.text = $"{res.x} x {res.y}";
        }
    }

    private void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        Debug.Log($"Quality level ingesteld op: {QualitySettings.names[qualityIndex]}");
    }

    public void UpdateMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", LinearToDecibel(value));
    }

    public void UpdateSFXVolume(float value)
    {
        audioMixer.SetFloat("SFXVolume", LinearToDecibel(value));
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);
        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);
        PlayerPrefs.SetInt("QualityLevel", QualitySettings.GetQualityLevel());
        PlayerPrefs.Save();

        Debug.Log("Settings opgeslagen");
    }

    private void LoadSettings()
    {
        // Laad audio settings
        float musicValue = PlayerPrefs.GetFloat("MusicVolume", 1f);
        float sfxValue = PlayerPrefs.GetFloat("SFXVolume", 1f);

        musicSlider.value = musicValue;
        sfxSlider.value = sfxValue;

        UpdateMusicVolume(musicValue);
        UpdateSFXVolume(sfxValue);

        // Laad resolutie en quality settings
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        int savedQuality = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());

        // Pas instellingen toe
        if (currentResolutionIndex < supportedResolutions.Count)
        {
            ChangeResolution();
        }

        SetQuality(savedQuality);
        qualityField.value = QualitySettings.names[savedQuality];

        UpdateResolutionDisplay();

        Debug.Log("Settings geladen");
    }

    private float LinearToDecibel(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
    }

    // Helper struct voor button layouts
    [System.Serializable]
    public struct ButtonLayout
    {
        public float leftPercent;
        public float topPercent;
        public float widthPercent;
        public float heightPercent;

        public ButtonLayout(float left, float top, float width, float height)
        {
            leftPercent = left;
            topPercent = top;
            widthPercent = width;
            heightPercent = height;
        }
    }

    // Public methods voor andere scripts
    public void OnBackButton()
    {
        SaveSettings();
        // Hier kun je terug naar main menu gaan
    }

    public void ResetToDefaults()
    {
        musicSlider.value = 1f;
        sfxSlider.value = 1f;
        currentResolutionIndex = 0;
        SetQuality(QualitySettings.names.Length - 1); // Hoogste quality

        UpdateMusicVolume(1f);
        UpdateSFXVolume(1f);
        ChangeResolution();
        qualityField.value = QualitySettings.names[QualitySettings.GetQualityLevel()];

        Debug.Log("Settings gereset naar standaardwaarden");
    }
}
