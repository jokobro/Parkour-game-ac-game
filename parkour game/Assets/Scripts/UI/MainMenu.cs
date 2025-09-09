using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioMixer audioMixer;

    // Basis variabelen
    private UIDocument uiDocument;
    private VisualElement[] allPanels;

    // INPUT MODE MANAGEMENT - Alleen 1 actief tegelijk!
    private enum InputMode { Controller, Mouse }
    private InputMode currentInputMode = InputMode.Mouse;
    private float lastInputTime = 0f;
    private const float INPUT_DELAY = 0.12f;

    // Controller input
    private bool wasUp, wasDown, wasLeft, wasRight;

    // Welke menu elementen kunnen we selecteren?
    private List<MenuElement> selectableElements = new List<MenuElement>();
    private int currentSelection = 0;

    // PURE MONITOR RESOLUTION SYSTEM - ALLEEN ECHTE RESOLUTIES
    [Header("Resolution Settings")]
    private List<Resolution> realMonitorResolutions = new List<Resolution>();
    private int currentResolutionIndex = 0;
    private Resolution nativeResolution;
    private float nativeAspectRatio;

    // Quality instellingen
    private int currentQualityIndex = 0;

    // Enhanced visual feedback - AC Syndicate style
    private bool isAdjustingSlider = false;

    // Simpele class voor menu elementen
    private class MenuElement
    {
        public string name;
        public Button button;
        public Slider slider;
        public VisualElement container;

        public MenuElement(string elementName, Button btn)
        {
            name = elementName;
            button = btn;
            container = btn;
        }

        public MenuElement(string elementName, Slider sldr, VisualElement cont = null)
        {
            name = elementName;
            slider = sldr;
            container = cont ?? sldr;
        }

        public MenuElement(string elementName, Button btn, VisualElement cont)
        {
            name = elementName;
            button = btn;
            container = cont;
        }
    }

    void Start()
    {
        Setup();
    }

    #region PURE MONITOR RESOLUTION SYSTEM - ALLEEN ECHTE RESOLUTIES

    void SetupResolutionSystem()
    {
        // STAP 1: Bewaar native monitor resolutie en aspect ratio
        nativeResolution = Screen.currentResolution;
        nativeAspectRatio = (float)nativeResolution.width / (float)nativeResolution.height;
        
        // STAP 2: Setup ALLEEN échte monitor resoluties
        SetupRealMonitorResolutions();
        
        // STAP 3: Setup resolution controls
        RegisterButtonClick("ResolutionPrevBtn", () => ChangeResolution(-1));
        RegisterButtonClick("ResolutionNextBtn", () => ChangeResolution(1));
        
        // STAP 4: Zorg dat main menu altijd native resolutie heeft
        EnsureMainMenuNativeResolution();
        
        UpdateResolutionText();

        Debug.Log($"🖥️ Native monitor: {nativeResolution.width}x{nativeResolution.height}@{nativeResolution.refreshRate}Hz");
        Debug.Log($"📐 Native aspect ratio: {nativeAspectRatio:F3} ({GetAspectRatioName(nativeAspectRatio)})");
        Debug.Log($"🎮 Main menu stays at native resolution");
        Debug.Log($"📺 Test scene will use ONLY REAL monitor resolutions (100% guaranteed no black bars)");
    }

    void SetupRealMonitorResolutions()
    {
        realMonitorResolutions.Clear();
        
        Debug.Log($"🔍 Scanning ALL monitor resolutions from Screen.resolutions...");
        
        // STAP 1: Verzamel alle monitor resoluties (zonder filtering)
        var allMonitorRes = Screen.resolutions.ToList();
        Debug.Log($"📺 Total resolutions reported by monitor: {allMonitorRes.Count}");
        
        // STAP 2: Filter op aspect ratio matching (kleine tolerantie)
        const float aspectTolerance = 0.02f; // Iets ruimer voor edge cases
        var aspectMatchingRes = allMonitorRes.Where(res => {
            float resAspect = (float)res.width / (float)res.height;
            bool matches = Mathf.Abs(resAspect - nativeAspectRatio) < aspectTolerance;
            
            if (matches)
            {
                Debug.Log($"✅ KEEP: {res.width}x{res.height}@{res.refreshRate}Hz (aspect: {resAspect:F3}) - matches native");
            }
            else
            {
                Debug.Log($"❌ SKIP: {res.width}x{res.height}@{res.refreshRate}Hz (aspect: {resAspect:F3}) - different aspect ratio");
            }
            
            return matches;
        }).ToList();
        
        Debug.Log($"📐 Aspect ratio matching resolutions: {aspectMatchingRes.Count}");
        
        // STAP 3: Groepeer per resolutie (width x height) en neem hoogste refresh rate
        var uniqueResolutions = new Dictionary<string, Resolution>();
        foreach (var res in aspectMatchingRes)
        {
            string key = $"{res.width}x{res.height}";
            if (!uniqueResolutions.ContainsKey(key) || res.refreshRate > uniqueResolutions[key].refreshRate)
            {
                if (uniqueResolutions.ContainsKey(key))
                {
                    Debug.Log($"🔄 UPGRADE: {key} from {uniqueResolutions[key].refreshRate}Hz to {res.refreshRate}Hz");
                }
                else
                {
                    Debug.Log($"➕ ADD: {key}@{res.refreshRate}Hz");
                }
                uniqueResolutions[key] = res;
            }
        }
        
        // STAP 4: Converteer naar lijst en sorteer (GEEN EXTRA RESOLUTIES TOEVOEGEN!)
        realMonitorResolutions = uniqueResolutions.Values.OrderBy(r => r.width * r.height).ToList();
        
        // STAP 5: Vind current resolutie index
        currentResolutionIndex = realMonitorResolutions.FindIndex(r => 
            r.width == nativeResolution.width && r.height == nativeResolution.height);
        
        if (currentResolutionIndex == -1)
        {
            Debug.LogWarning("⚠️ Native resolution not found in filtered list! Adding it manually...");
            realMonitorResolutions.Add(nativeResolution);
            realMonitorResolutions = realMonitorResolutions.OrderBy(r => r.width * r.height).ToList();
            currentResolutionIndex = realMonitorResolutions.FindIndex(r => 
                r.width == nativeResolution.width && r.height == nativeResolution.height);
        }

        Debug.Log($"📺 FINAL REAL monitor resolutions: {realMonitorResolutions.Count} (100% supported by your monitor)");
        
        // Log alle beschikbare resoluties
        for (int i = 0; i < realMonitorResolutions.Count; i++)
        {
            var res = realMonitorResolutions[i];
            string marker = (i == currentResolutionIndex) ? " <-- CURRENT" : "";
            string quality = GetResolutionQuality(res.width, res.height);
            float aspect = (float)res.width / (float)res.height;
            string aspectName = GetAspectRatioName(aspect);
            Debug.Log($"  📺 [{i}] {res.width}x{res.height}@{res.refreshRate}Hz ({quality}) [{aspectName}]{marker}");
        }
        
        Debug.Log($"🎯 GUARANTEE: All {realMonitorResolutions.Count} resolutions are 100% real and supported by your monitor!");
    }

    string GetAspectRatioName(float aspectRatio)
    {
        if (Mathf.Abs(aspectRatio - 16f/9f) < 0.01f) return "16:9";
        if (Mathf.Abs(aspectRatio - 21f/9f) < 0.01f) return "21:9";
        if (Mathf.Abs(aspectRatio - 32f/9f) < 0.01f) return "32:9";
        if (Mathf.Abs(aspectRatio - 4f/3f) < 0.01f) return "4:3";
        if (Mathf.Abs(aspectRatio - 16f/10f) < 0.01f) return "16:10";
        if (Mathf.Abs(aspectRatio - 5f/4f) < 0.01f) return "5:4";
        return $"{aspectRatio:F2}:1";
    }

    void EnsureMainMenuNativeResolution()
    {
        // Zorg dat main menu scene altijd native resolutie heeft
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (IsMainMenuScene(currentScene))
        {
            // FORCE native resolutie voor main menu
            ApplyResolutionSafely(nativeResolution, "Main Menu");
        }
    }

    bool IsMainMenuScene(string sceneName)
    {
        // Check voor main menu scene namen (case insensitive)
        string[] mainMenuScenes = { "mainmenu", "main menu", "menu", "start", "title" };
        string lowerSceneName = sceneName.ToLower();
        
        foreach (string menuScene in mainMenuScenes)
        {
            if (lowerSceneName.Contains(menuScene))
            {
                return true;
            }
        }
        return false;
    }

    bool IsGameplayScene(string sceneName)
    {
        // Check voor gameplay scenes - specifiek "Test scene"
        return sceneName.ToLower().Contains("test");
    }

    string GetResolutionQuality(int width, int height)
    {
        if (width >= 3840) return "4K";
        if (width >= 2560) return "1440p";
        if (width >= 1920) return "1080p";
        if (width >= 1600) return "900p";
        if (width >= 1366) return "Laptop";
        if (width >= 1280) return "720p";
        return "Low";
    }

    void ChangeResolution(int direction)
    {
        if (realMonitorResolutions.Count == 0) 
        {
            Debug.LogWarning("⚠️ No real monitor resolutions available!");
            return;
        }

        // Update index
        currentResolutionIndex = (currentResolutionIndex + direction + realMonitorResolutions.Count) % realMonitorResolutions.Count;
        var newRes = realMonitorResolutions[currentResolutionIndex];
        
        // BELANGRIJKE LOGICA: Alleen toepassen als we NIET in main menu zijn
        string currentScene = SceneManager.GetActiveScene().name;
        
        if (IsMainMenuScene(currentScene))
        {
            // Main menu: NIET de resolutie wijzigen, alleen de setting opslaan
            Debug.Log($"📺 Resolution setting changed to: {newRes.width}x{newRes.height}@{newRes.refreshRate}Hz (will apply in Test scene)");
            Debug.Log($"🎮 Main menu stays at native: {nativeResolution.width}x{nativeResolution.height}@{nativeResolution.refreshRate}Hz");
            Debug.Log($"🎯 GUARANTEED: This resolution is 100% supported by your monitor!");
        }
        else if (IsGameplayScene(currentScene))
        {
            // Test scene: WEL de resolutie toepassen met REAL MONITOR GUARANTEE
            ApplyResolutionSafely(newRes, "Test Scene");
        }
        
        UpdateResolutionText();
    }

    void ApplyResolutionSafely(Resolution resolution, string context)
    {
        Debug.Log($"🔧 APPLYING {context} resolution: {resolution.width}x{resolution.height}@{resolution.refreshRate}Hz");
        Debug.Log($"🎯 GUARANTEE: This is a REAL monitor resolution - no black bars expected!");
        
        // STAP 1: Probeer Fullscreen Window mode (beste voor most monitors)
        Screen.SetResolution(resolution.width, resolution.height, FullScreenMode.FullScreenWindow, resolution.refreshRate);
        
        // STAP 2: Wacht en verifieer
        StartCoroutine(VerifyRealResolution(resolution, context));
    }

    System.Collections.IEnumerator VerifyRealResolution(Resolution targetResolution, string context)
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Extra frames voor GPU sync
        yield return new WaitForEndOfFrame();
        
        var currentRes = Screen.currentResolution;
        bool exactMatch = (currentRes.width == targetResolution.width && 
                          currentRes.height == targetResolution.height);
        
        float currentAspect = (float)currentRes.width / (float)currentRes.height;
        float targetAspect = (float)targetResolution.width / (float)targetResolution.height;
        bool aspectMatch = Mathf.Abs(currentAspect - targetAspect) < 0.01f;
        
        if (exactMatch)
        {
            string quality = GetResolutionQuality(currentRes.width, currentRes.height);
            string aspectName = GetAspectRatioName(currentAspect);
            Debug.Log($"✅✅✅ {context} resolution PERFECT: {currentRes.width}x{currentRes.height}@{currentRes.refreshRate}Hz ({quality}) [{aspectName}]");
            
        }
        else if (aspectMatch)
        {
            Debug.LogWarning($"⚠️ {context} resolution CLOSE: Got {currentRes.width}x{currentRes.height}, wanted {targetResolution.width}x{targetResolution.height}");
            Debug.Log($"✅ Aspect ratio OK - no black bars expected (real monitor resolution)");
        }
        else
        {
            Debug.LogError($"❌ {context} resolution UNEXPECTED! Got: {currentRes.width}x{currentRes.height}, Target: {targetResolution.width}x{targetResolution.height}");
            Debug.LogError($"😬 This should NOT happen with real monitor resolutions!");
            
            // FALLBACK: probeer native resolutie
            if (!exactMatch && targetResolution.width != nativeResolution.width)
            {
                Debug.Log($"🔄 EMERGENCY FALLBACK: Reverting to native resolution...");
                Screen.SetResolution(nativeResolution.width, nativeResolution.height, FullScreenMode.FullScreenWindow, nativeResolution.refreshRate);
                
                yield return new WaitForEndOfFrame();
                var fallbackRes = Screen.currentResolution;
                Debug.Log($"🏠 Emergency fallback result: {fallbackRes.width}x{fallbackRes.height}@{fallbackRes.refreshRate}Hz");
            }
        }
    }

    void UpdateResolutionText()
    {
        var resolutionLabel = uiDocument.rootVisualElement.Q<Label>("ResolutionLabel");
        if (resolutionLabel != null && currentResolutionIndex >= 0 && currentResolutionIndex < realMonitorResolutions.Count)
        {
            var res = realMonitorResolutions[currentResolutionIndex];
            resolutionLabel.text = $"{res.width} x {res.height}";
        }
    }

    // NIEUWE METHODE: Apply resolutie settings when loading a scene
    void ApplyResolutionForScene(string sceneName)
    {
        if (IsMainMenuScene(sceneName))
        {
            // Main menu: altijd native
            ApplyResolutionSafely(nativeResolution, "Main Menu Scene Load");
        }
        else if (IsGameplayScene(sceneName))
        {
            // Test scene: gebruik opgeslagen setting met REAL MONITOR GUARANTEE
            if (currentResolutionIndex >= 0 && currentResolutionIndex < realMonitorResolutions.Count)
            {
                var res = realMonitorResolutions[currentResolutionIndex];
                ApplyResolutionSafely(res, "Test Scene Load");
            }
        }
    }

    #endregion

    #region SETUP - Initialisatie van het menu

    void Setup()
    {
        // Basis setup
        uiDocument = GetComponent<UIDocument>();
        FindAllPanels();
        SetupButtonClicks();
        SetupAudioSettings();
        SetupResolutionSystem(); // NIEUWE: Pure real monitor resolution systeem
        SetupQualitySettings();
        SetupMouseHoverEvents();
        SetupACButtonStyling();

        // Start met main menu
        ShowMainMenu();
        LoadSavedSettings();

        // Start in mouse mode
        SwitchToInputMode(InputMode.Mouse);

        // DEBUG: Log alle UI elementen
        LogAllUIElements();

        Debug.Log("🎮 AC Syndicate Main Menu Setup Complete - PURE REAL monitor resolutions only!");
    }

    void FindAllPanels()
    {
        var root = uiDocument.rootVisualElement;

        allPanels = new VisualElement[]
        {
            root.Q<VisualElement>("Options"),
            root.Q<VisualElement>("Credits"),
            root.Q<VisualElement>("Sound"),
            root.Q<VisualElement>("Video"),
            root.Q<VisualElement>("Controls")
        };

        HideAllPanels();
    }

    void SetupACButtonStyling()
    {
        var root = uiDocument.rootVisualElement;
        var allButtons = root.Query<Button>().ToList();

        foreach (var button in allButtons)
        {
            button.focusable = true;
            button.AddToClassList("ac-button");

            if (button.name == "ContinueButton" || button.name == "OptionsButton" || button.name == "QuitButton")
            {
                button.AddToClassList("main-menu-button");
            }

            if (button.name.Contains("Prev") || button.name.Contains("Next"))
            {
                button.AddToClassList("nav-arrow-button");
            }
        }

        Debug.Log($"✅ AC Syndicate styling applied to {allButtons.Count} buttons");
    }

    void SetupButtonClicks()
    {
        // UPDATED StartGame method met scene-specific resolution
        RegisterButtonClick("ContinueButton", () => StartGameWithResolution());
        RegisterButtonClick("OptionsButton", () => {
            Debug.Log("🎯 Options clicked - checking if panel exists...");
            ShowPanel("Options");
        });
        RegisterButtonClick("QuitButton", QuitGame);

        // Sub menu buttons
        RegisterButtonClick("VideoButton", () => ShowPanel("Video"));
        RegisterButtonClick("SoundButton", () => ShowPanel("Sound"));
        RegisterButtonClick("ControlsButton", () => ShowPanel("Controls"));
        RegisterButtonClick("creditsButton", () => ShowPanel("Credits"));

        // Back buttons
        RegisterButtonClick("BackButton", ShowMainMenu);
        RegisterButtonClick("CloseCredits", () => ShowPanel("Options"));
        RegisterButtonClick("CloseSound", () => ShowPanel("Options"));
        RegisterButtonClick("CloseVideo", () => ShowPanel("Options"));
        RegisterButtonClick("CloseControls", () => ShowPanel("Options"));
    }

    void RegisterButtonClick(string buttonName, System.Action onClick)
    {
        var button = uiDocument.rootVisualElement.Q<Button>(buttonName);
        if (button != null)
        {
            button.clicked += onClick;
        }
        
    }

    void SetupMouseHoverEvents()
    {
        var root = uiDocument.rootVisualElement;

        var allButtons = root.Query<Button>().ToList();
        foreach (var button in allButtons)
        {
            button.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            button.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
        }

        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");
        var qualityContainer = root.Q<VisualElement>("QaulityContainer");

        if (musicContainer != null)
        {
            musicContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            musicContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
        }

        if (sfxContainer != null)
        {
            sfxContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            sfxContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
        }

        if (resolutionContainer != null)
        {
            resolutionContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            resolutionContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
        }

        if (qualityContainer != null)
        {
            qualityContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            qualityContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
        }
    }
    #endregion

    #region INPUT MODE MANAGEMENT
    void SwitchToInputMode(InputMode newMode)
    {
        if (currentInputMode == newMode) return;

        currentInputMode = newMode;

        ClearAllHighlights();
        ClearAllMouseHighlights();

        var root = uiDocument.rootVisualElement;
        var controllerHint = root.Q<Label>("ControllerHint");

        if (currentInputMode == InputMode.Controller)
        {
            if (selectableElements.Count > 1)
            {
                HighlightCurrentElement();
            }
            else if (selectableElements.Count == 1)
            {
                currentSelection = 0;
            }

            if (controllerHint != null)
            {
                controllerHint.style.display = DisplayStyle.Flex;
            }
        }
        else
        {
            if (controllerHint != null)
            {
                controllerHint.style.display = DisplayStyle.None;
            }
        }
    }

    void OnMouseEnterElement(MouseEnterEvent evt)
    {
        SwitchToInputMode(InputMode.Mouse);

        var element = evt.target as VisualElement;
        if (element != null)
        {
            element.AddToClassList("mouse-hover");
        }
    }

    void OnMouseLeaveElement(MouseLeaveEvent evt)
    {
        var element = evt.target as VisualElement;
        if (element != null)
        {
            element.RemoveFromClassList("mouse-hover");
        }
    }

    void ClearAllMouseHighlights()
    {
        var root = uiDocument.rootVisualElement;
        var allElements = root.Query<VisualElement>().ToList();
        foreach (var element in allElements)
        {
            element.RemoveFromClassList("mouse-hover");
        }
    }

    #endregion

    #region AUDIO INSTELLINGEN
    void SetupAudioSettings()
    {
        var root = uiDocument.rootVisualElement;

        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");

        if (musicSlider != null)
        {
            musicSlider.RegisterValueChangedCallback(evt =>
            {
                SetMusicVolume(evt.newValue);
            });
        }

        if (sfxSlider != null)
        {
            sfxSlider.RegisterValueChangedCallback(evt =>
            {
                SetSFXVolume(evt.newValue);
            });
        }

        SetupVolumeButtons();
    }

    void SetupVolumeButtons()
    {
        var root = uiDocument.rootVisualElement;

        var musicSlider = root.Q<Slider>("MusicSlider");
        RegisterButtonClick("MusicDecreaseBtn", () => ChangeSliderValue(musicSlider, -0.1f));
        RegisterButtonClick("MusicIncreaseBtn", () => ChangeSliderValue(musicSlider, 0.1f));

        var sfxSlider = root.Q<Slider>("SFXSlider");
        RegisterButtonClick("SFXDecreaseBtn", () => ChangeSliderValue(sfxSlider, -0.1f));
        RegisterButtonClick("SFXIncreaseBtn", () => ChangeSliderValue(sfxSlider, 0.1f));
    }

    void ChangeSliderValue(Slider slider, float change)
    {
        if (slider != null)
        {
            slider.value = Mathf.Clamp(slider.value + change, 0f, 1f);
        }
    }

    void SetMusicVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            bool success = audioMixer.SetFloat("MusicVolume", dbValue);
        }
    }

    void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            bool success = audioMixer.SetFloat("SFXVolume", dbValue);
        }
    }

    #endregion

    #region QUALITY INSTELLINGEN

    void SetupQualitySettings()
    {
        currentQualityIndex = QualitySettings.GetQualityLevel();
        UpdateQualityText();

        RegisterButtonClick("QualityPrevBtn", () => ChangeQuality(-1));
        RegisterButtonClick("QaulityPrevBtn", () => ChangeQuality(-1));
        RegisterButtonClick("QualityNextBtn", () => ChangeQuality(1));
        RegisterButtonClick("QaulityNextBtn", () => ChangeQuality(1));
    }

    void ChangeQuality(int direction)
    {
        int totalQualityLevels = QualitySettings.names.Length;
        currentQualityIndex = (currentQualityIndex + direction + totalQualityLevels) % totalQualityLevels;

        QualitySettings.SetQualityLevel(currentQualityIndex);
        UpdateQualityText();
    }

    void UpdateQualityText()
    {
        var qualityLabel = uiDocument.rootVisualElement.Q<Label>("QaulityLabel");
        if (qualityLabel == null)
        {
            return;
        }

        if (currentQualityIndex < QualitySettings.names.Length)
        {
            qualityLabel.text = QualitySettings.names[currentQualityIndex];
        }
    }

    #endregion

    #region PANEL MANAGEMENT

    void ShowMainMenu()
    {
        HideAllPanels();
        SetupMainMenuElements();
    }

    void ShowPanel(string panelName)
    {
        HideAllPanels();

        var panel = uiDocument.rootVisualElement.Q<VisualElement>(panelName);
        if (panel != null)
        {
            panel.style.display = DisplayStyle.Flex;
            panel.style.visibility = Visibility.Visible;

            SetupPanelElements(panelName);
        }
        else
        {
            ShowMainMenu();
        }
    }

    void HideAllPanels()
    {
        foreach (var panel in allPanels)
        {
            if (panel != null)
            {
                panel.style.display = DisplayStyle.None;
                panel.style.visibility = Visibility.Hidden;
            }
        }
    }

    #endregion

    #region CONTROLLER NAVIGATION
    void SetupMainMenuElements()
    {
        selectableElements.Clear();

        AddButtonElement("ContinueButton", "Continue");
        AddButtonElement("OptionsButton", "Options");
        AddButtonElement("QuitButton", "Quit");

        currentSelection = 0;
        if (currentInputMode == InputMode.Controller)
        {
            HighlightCurrentElement();
        }
    }

    void SetupPanelElements(string panelName)
    {
        selectableElements.Clear();

        switch (panelName)
        {
            case "Options":
                SetupOptionsElements();
                break;
            case "Sound":
                SetupSoundElements();
                break;
            case "Video":
                SetupVideoElements();
                break;
            case "Credits":
            case "Controls":
                SetupSimplePanelElements(panelName);
                break;
        }

        if (selectableElements.Count > 1)
        {
            currentSelection = 0;
            if (currentInputMode == InputMode.Controller)
            {
                HighlightCurrentElement();
            }
        }
        else if (selectableElements.Count == 1)
        {
            currentSelection = 0;
        }
    }

    void SetupOptionsElements()
    {
        AddButtonElement("VideoButton", "Video");
        AddButtonElement("SoundButton", "Sound");
        AddButtonElement("ControlsButton", "Controls");
        AddButtonElement("creditsButton", "Credits");
        AddButtonElement("BackButton", "Back");
    }

    void SetupSoundElements()
    {
        var root = uiDocument.rootVisualElement;

        var musicSlider = root.Q<Slider>("MusicSlider");
        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        if (musicSlider != null)
        {
            selectableElements.Add(new MenuElement("Music Volume", musicSlider, musicContainer));
        }

        var sfxSlider = root.Q<Slider>("SFXSlider");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");
        if (sfxSlider != null)
        {
            selectableElements.Add(new MenuElement("SFX Volume", sfxSlider, sfxContainer));
        }

        AddButtonElement("CloseSound", "Back");
    }

    void SetupVideoElements()
    {
        var root = uiDocument.rootVisualElement;

        var resolutionPrevBtn = root.Q<Button>("ResolutionPrevBtn");
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");
        
        if (resolutionPrevBtn != null && resolutionContainer != null)
        {
            selectableElements.Add(new MenuElement("Resolution", resolutionPrevBtn, resolutionContainer));
        }

        var qualityPrevBtn = root.Q<Button>("QaulityPrevBtn");
        var qualityContainer = root.Q<VisualElement>("QaulityContainer");
        
        if (qualityPrevBtn != null && qualityContainer != null)
        {
            selectableElements.Add(new MenuElement("Quality", qualityPrevBtn, qualityContainer));
        }
        else if (qualityPrevBtn != null)
        {
            selectableElements.Add(new MenuElement("Quality", qualityPrevBtn));
        }

        AddButtonElement("CloseVideo", "Back");
    }

    void SetupSimplePanelElements(string panelName)
    {
        string backButtonName = "Close" + panelName;
        AddButtonElement(backButtonName, "Back");
    }

    void AddButtonElement(string buttonName, string displayName)
    {
        var button = uiDocument.rootVisualElement.Q<Button>(buttonName);
        if (button != null)
        {
            selectableElements.Add(new MenuElement(displayName, button));
        }
    }

    void StartControllerNavigation()
    {
        if (currentInputMode == InputMode.Controller && selectableElements.Count > 0)
        {
            currentSelection = 0;
            HighlightCurrentElement();
        }
    }

    void HighlightCurrentElement()
    {
        if (currentInputMode != InputMode.Controller) return;

        ClearAllHighlights();

        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];

            if (element.button != null)
            {
                if (element.container == element.button || element.container == null)
                {
                    element.button.AddToClassList("ac-button-selected");
                    element.button.AddToClassList("ac-pulse");
                    element.button.Focus();
                }
                else
                {
                    element.container.AddToClassList("controller-selected");
                    element.container.AddToClassList("ac-pulse");
                }
            }
            else if (element.slider != null && element.container != null)
            {
                element.container.AddToClassList("controller-selected");
                element.container.AddToClassList("ac-pulse");
            }
        }
    }

    void ClearAllHighlights()
    {
        foreach (var element in selectableElements)
        {
            if (element.container != null)
            {
                element.container.RemoveFromClassList("controller-selected");
                element.container.RemoveFromClassList("ac-pulse");
            }

            if (element.button != null)
            {
                element.button.RemoveFromClassList("ac-button-selected");
                element.button.RemoveFromClassList("ac-pulse");
            }
        }
    }
    #endregion

    #region INPUT HANDLING
    void Update()
    {
        DetectInputModeChanges();

        if (currentInputMode == InputMode.Controller)
        {
            HandleControllerInput();
        }
    }

    void DetectInputModeChanges()
    {
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            Vector2 dpad = Gamepad.current.dpad.ReadValue();

            bool anyButtonPressed = Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                  Gamepad.current.buttonEast.wasPressedThisFrame ||
                                  Gamepad.current.buttonWest.wasPressedThisFrame ||
                                  Gamepad.current.buttonNorth.wasPressedThisFrame ||
                                  Gamepad.current.leftShoulder.wasPressedThisFrame ||
                                  Gamepad.current.rightShoulder.wasPressedThisFrame;

            if (stick.magnitude > 0.2f || rightStick.magnitude > 0.2f ||
                dpad.magnitude > 0.2f || anyButtonPressed)
            {
                SwitchToInputMode(InputMode.Controller);
            }
        }

        if (Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 1f)
        {
            SwitchToInputMode(InputMode.Mouse);
        }
    }

    void HandleControllerInput()
    {
        if (Gamepad.current == null) return;
        if (Time.time - lastInputTime < INPUT_DELAY) return;

        Vector2 stick = Gamepad.current.leftStick.ReadValue();
        Vector2 dpad = Gamepad.current.dpad.ReadValue();
        Vector2 combinedInput = stick + dpad;

        float threshold = 0.5f;

        bool hasActiveSelection = selectableElements.Count > 0 &&
                                currentSelection < selectableElements.Count &&
                                (selectableElements[currentSelection].button?.ClassListContains("ac-button-selected") == true ||
                                 selectableElements[currentSelection].container?.ClassListContains("controller-selected") == true);

        // Up/Down navigatie
        if (combinedInput.y > threshold && !wasUp)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                StartControllerNavigation();
            }
            else
            {
                NavigateUp();
            }
            wasUp = true;
            lastInputTime = Time.time;
            /*PlayNavigationSound();*/
        }
        else if (combinedInput.y <= threshold) wasUp = false;

        if (combinedInput.y < -threshold && !wasDown)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                StartControllerNavigation();
            }
            else
            {
                NavigateDown();
            }
            wasDown = true;
            lastInputTime = Time.time;
            /*PlayNavigationSound();*/
        }
        else if (combinedInput.y >= -threshold) wasDown = false;

        // Left/Right voor sliders en instellingen
        if (combinedInput.x < -threshold && !wasLeft)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                StartControllerNavigation();
            }
            else
            {
                AdjustCurrentElement(-1);
            }
            wasLeft = true;
            lastInputTime = Time.time;
        }
        else if (combinedInput.x >= -threshold) wasLeft = false;

        if (combinedInput.x > threshold && !wasRight)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                StartControllerNavigation();
            }
            else
            {
                AdjustCurrentElement(1);
            }
            wasRight = true;
            lastInputTime = Time.time;
        }
        else if (combinedInput.x <= threshold) wasRight = false;

        // A button (confirm)
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                StartControllerNavigation();
            }
            else
            {
                ActivateCurrentElement();
                /*PlayConfirmSound();*/
            }
        }

        /*// B button (back)
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            GoBack();
            *//*PlayBackSound();*//*
        }*/
    }

    void NavigateUp()
    {
        if (selectableElements.Count > 0)
        {
            currentSelection = (currentSelection - 1 + selectableElements.Count) % selectableElements.Count;
            HighlightCurrentElement();
        }
    }

    void NavigateDown()
    {
        if (selectableElements.Count > 0)
        {
            currentSelection = (currentSelection + 1) % selectableElements.Count;
            HighlightCurrentElement();
        }
    }

    void AdjustCurrentElement(int direction)
    {
        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];

            if (element.slider != null)
            {
                float change = direction * 0.1f;
                element.slider.value = Mathf.Clamp(element.slider.value + change, 0f, 1f);
            }
            else if (element.name == "Resolution")
            {
                // PURE: Alleen ECHTE monitor resoluties
                ChangeResolution(direction);
            }
            else if (element.name == "Quality")
            {
                ChangeQuality(direction);
            }
        }
    }

    void ActivateCurrentElement()
    {
        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];
            if (element.button != null)
            {
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = element.button;
                    element.button.SendEvent(clickEvent);
                }
            }
        }
    }

    void GoBack()
    {
        var backElement = selectableElements.Find(e => e.name == "Back");
        if (backElement?.button != null)
        {
            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = backElement.button;
                backElement.button.SendEvent(clickEvent);
            }
        }
        else
        {
            ShowMainMenu();
        }
    }

   /* void PlayNavigationSound()
    {
        Debug.Log("🔊 AC Navigation");
    }

    void PlayConfirmSound()
    {
        Debug.Log("🔊 AC Confirm");
    }

    void PlayBackSound()
    {
        Debug.Log("🔊 AC Back");
    }*/

    #endregion

    #region GAME ACTIONS - UPDATED MET PURE REAL RESOLUTION

    // NIEUWE METHODE: Start game met PURE real monitor resolutie toepassing
    void StartGameWithResolution()
    {
        SaveAllSettings();
        
        // BELANGRIJK: Apply resolutie setting voor Test scene
        StartCoroutine(LoadSceneWithPureRealResolution("Test sceme"));
    }

    System.Collections.IEnumerator LoadSceneWithPureRealResolution(string sceneName)
    {
        // Load de scene
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Wacht tot scene geladen is
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
        
        // Apply de correcte resolutie voor deze scene MET PURE REAL GUARANTEE
        yield return new WaitForEndOfFrame(); // Wacht een frame
        yield return new WaitForEndOfFrame(); // Extra frame voor zekerheid
        yield return new WaitForEndOfFrame(); // Nog een frame voor GPU sync
        ApplyResolutionForScene(sceneName);
    }

    void QuitGame()
    {
        SaveAllSettings();
        Application.Quit();
    }

    void SaveAllSettings()
    {
        var root = uiDocument.rootVisualElement;

        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");

        if (musicSlider != null) PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);

        // SAVE resolutie setting (voor Test scene)
        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);
        PlayerPrefs.SetInt("QualityLevel", currentQualityIndex);

        PlayerPrefs.Save();
    }

    void LoadSavedSettings()
    {
        var root = uiDocument.rootVisualElement;

        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");

        if (musicSlider != null)
        {
            float savedMusic = PlayerPrefs.GetFloat("MusicVolume", 1f);
            musicSlider.value = savedMusic;
            SetMusicVolume(savedMusic);
        }

        if (sfxSlider != null)
        {
            float savedSFX = PlayerPrefs.GetFloat("SFXVolume", 1f);
            sfxSlider.value = savedSFX;
            SetSFXVolume(savedSFX);
        }

        // LOAD resolutie setting (maar niet toepassen in main menu)
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        UpdateResolutionText();

        currentQualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(currentQualityIndex);
        UpdateQualityText();
    }

    #endregion

    #region DEBUG METHODS

    void LogAllUIElements()
    {
        var root = uiDocument.rootVisualElement;

        Debug.Log("=== 🔍 UI ELEMENTS DEBUG ===");

        var panels = new string[] { "Options", "Video", "Sound", "Credits", "Controls" };
        foreach (string panelName in panels)
        {
            var panel = root.Q<VisualElement>(panelName);
        }

        /*var allButtons = root.Query<Button>().ToList();
        foreach (var btn in allButtons)
        {
            Debug.Log($"  🔘 Button: {btn.name} (text: '{btn.text}')");
        }

        Debug.Log("=== 🔍 END UI ELEMENTS DEBUG ===");*/
    }
    #endregion
}
