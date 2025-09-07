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
    private const float INPUT_DELAY = 0.12f; // Snellere response voor AC Syndicate feel

    // Controller input
    private bool wasUp, wasDown, wasLeft, wasRight;

    // Welke menu elementen kunnen we selecteren?
    private List<MenuElement> selectableElements = new List<MenuElement>();
    private int currentSelection = 0;

    // SIMPELE RESOLUTIE LIJST - GEEN EXTREME FILTERING
    [Header("Resolution Settings")]
    private List<Vector2Int> resolutions = new List<Vector2Int>();
    private int currentResolutionIndex = 0;

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
        public VisualElement container; // Voor rode highlighting

        // Maak een menu element van een button
        public MenuElement(string elementName, Button btn)
        {
            name = elementName;
            button = btn;
            container = btn;
        }

        // Maak een menu element van een slider met container
        public MenuElement(string elementName, Slider sldr, VisualElement cont = null)
        {
            name = elementName;
            slider = sldr;
            container = cont ?? sldr;
        }

        // Maak een menu element van button met container
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

    #region SIMPELE RESOLUTIE SETUP - LAAT USERS KIEZEN

    void SetupResolutionSystem()
    {
        // Simpele aanpak: gewoon goede resoluties aanbieden
        SetupSimpleResolutionList();

        // Resolution controls
        RegisterButtonClick("ResolutionPrevBtn", () => ChangeResolution(-1));
        RegisterButtonClick("ResolutionNextBtn", () => ChangeResolution(1));

        UpdateResolutionText();

        Debug.Log($"🖥️ Current screen resolution: {Screen.currentResolution.width}x{Screen.currentResolution.height}");
        Debug.Log($"🎮 UI designed for: 1920x1080 (from PanelSettings)");
        Debug.Log($"📺 User can choose any good resolution they want");
    }

    void SetupSimpleResolutionList()
    {
        resolutions.Clear();

        // SIMPELE LIJST MET GOEDE RESOLUTIES - Geen extreme filtering
        Vector2Int[] goodResolutions = {
            new Vector2Int(1280, 720),   // 720p - minimum
            new Vector2Int(1366, 768),   // Laptop standaard
            new Vector2Int(1600, 900),   // 900p
            new Vector2Int(1920, 1080),  // 1080p - UI design target
            new Vector2Int(2560, 1440),  // 1440p
            new Vector2Int(3840, 2160)   // 4K
        };

        // Voeg alle goede resoluties toe
        foreach (var res in goodResolutions)
        {
            resolutions.Add(res);
        }

        // Voeg ook monitor ondersteunde resoluties toe (als ze groot genoeg zijn)
        foreach (var res in Screen.resolutions)
        {
            Vector2Int resolution = new Vector2Int(res.width, res.height);

            // Simpele check: alleen toevoegen als groot genoeg en nog niet in lijst
            if (resolution.x >= 1280 && resolution.y >= 720 && !resolutions.Contains(resolution))
            {
                resolutions.Add(resolution);
            }
        }

        // Sorteer van klein naar groot (makkelijker om te navigeren)
        resolutions = resolutions.OrderBy(r => r.x * r.y).ToList();

        // Vind een goede starting resolutie
        Vector2Int currentRes = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        currentResolutionIndex = resolutions.FindIndex(r => r == currentRes);

        // Als huidige resolutie niet gevonden, start met 1920x1080 (UI design target)
        if (currentResolutionIndex == -1)
        {
            currentResolutionIndex = resolutions.FindIndex(r => r.x == 1920 && r.y == 1080);
            if (currentResolutionIndex == -1) currentResolutionIndex = resolutions.Count - 1; // fallback: hoogste
        }

        Debug.Log($"📺 Resolution list setup: {resolutions.Count} options available");

        // Log alle beschikbare resoluties
        for (int i = 0; i < resolutions.Count; i++)
        {
            var res = resolutions[i];
            string marker = (i == currentResolutionIndex) ? " <-- CURRENT" : "";
            string quality = GetResolutionQuality(res);
            Debug.Log($"  📺 [{i}] {res.x}x{res.y} ({quality}){marker}");
        }

        // BELANGRIJK: Als we niet op de design resolutie starten, fix dat direct
        if (currentResolutionIndex != -1)
        {
            var startRes = resolutions[currentResolutionIndex];
            if (startRes.x != Screen.currentResolution.width || startRes.y != Screen.currentResolution.height)
            {
                Debug.Log($"🔧 Setting resolution to: {startRes.x}x{startRes.y} to fix black bars");
                Screen.SetResolution(startRes.x, startRes.y, FullScreenMode.FullScreenWindow);
            }
        }
    }

    // Helper: krijg kwaliteit label voor resolutie
    string GetResolutionQuality(Vector2Int resolution)
    {
        if (resolution.x >= 3840) return "4K";
        if (resolution.x >= 2560) return "1440p";
        if (resolution.x >= 1920) return "1080p";
        if (resolution.x >= 1600) return "900p";
        if (resolution.x >= 1366) return "Laptop";
        return "720p";
    }

    void ChangeResolution(int direction)
    {
        if (resolutions.Count == 0)
        {
            Debug.LogWarning("⚠️ No resolutions available!");
            return;
        }

        currentResolutionIndex = (currentResolutionIndex + direction + resolutions.Count) % resolutions.Count;
        var newRes = resolutions[currentResolutionIndex];

        // Wijzig de scherm resolutie
        Screen.SetResolution(newRes.x, newRes.y, FullScreenMode.FullScreenWindow);

        UpdateResolutionText();

        string quality = GetResolutionQuality(newRes);
        Debug.Log($"📺 Resolution changed to: {newRes.x}x{newRes.y} ({quality})");

        if (newRes.x == 1920 && newRes.y == 1080)
        {
            Debug.Log($"✅ Perfect match with UI design resolution!");
        }
    }

    void UpdateResolutionText()
    {
        var resolutionLabel = uiDocument.rootVisualElement.Q<Label>("ResolutionLabel");
        if (resolutionLabel != null && currentResolutionIndex >= 0 && currentResolutionIndex < resolutions.Count)
        {
            var res = resolutions[currentResolutionIndex];
            resolutionLabel.text = $"{res.x} x {res.y}";
        }
        else
        {
            Debug.LogWarning("⚠️ Could not update resolution text!");
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
        SetupResolutionSystem(); // FIXED: Simpele resolutie setup
        SetupQualitySettings();
        SetupMouseHoverEvents();
        SetupACButtonStyling(); // AC Syndicate styling

        // Start met main menu
        ShowMainMenu();
        LoadSavedSettings();

        // Start in mouse mode
        SwitchToInputMode(InputMode.Mouse);

        // DEBUG: Log alle UI elementen
        LogAllUIElements();

        Debug.Log("🎮 AC Syndicate Main Menu Setup Complete - simple resolution system!");
    }

    void FindAllPanels()
    {
        var root = uiDocument.rootVisualElement;

        // Alle panels in je UXML
        allPanels = new VisualElement[]
        {
            root.Q<VisualElement>("Options"),
            root.Q<VisualElement>("Credits"),
            root.Q<VisualElement>("Sound"),
            root.Q<VisualElement>("Video"),
            root.Q<VisualElement>("Controls")
        };

        // Verstop alle panels (alleen als ze bestaan)
        HideAllPanels();
    }

    // AC Syndicate style - verbeterde button styling
    void SetupACButtonStyling()
    {
        var root = uiDocument.rootVisualElement;
        var allButtons = root.Query<Button>().ToList();

        foreach (var button in allButtons)
        {
            // BEHOUD focusable voor controller functionaliteit
            button.focusable = true;

            // Add our custom AC Syndicate button class
            button.AddToClassList("ac-button");

            // Voor main menu buttons, voeg extra class toe
            if (button.name == "ContinueButton" || button.name == "OptionsButton" || button.name == "QuitButton")
            {
                button.AddToClassList("main-menu-button");
            }

            // Voor navigation arrows
            if (button.name.Contains("Prev") || button.name.Contains("Next"))
            {
                button.AddToClassList("nav-arrow-button");
            }
        }

        Debug.Log($"✅ AC Syndicate styling applied to {allButtons.Count} buttons");
    }

    void SetupButtonClicks()
    {
        // Main menu buttons - MET DEBUG
        RegisterButtonClick("ContinueButton", StartGame);
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
            Debug.Log($"✅ Registered click for button: {buttonName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Button not found: {buttonName}");
        }
    }

    // FIXED: Setup mouse hover events voor containers
    void SetupMouseHoverEvents()
    {
        var root = uiDocument.rootVisualElement;

        // Alle buttons
        var allButtons = root.Query<Button>().ToList();
        foreach (var button in allButtons)
        {
            button.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            button.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement); // FIXED: MouseLeaveEvent
        }

        // Sound containers
        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");

        // Video containers - MET JUISTE NAMEN!
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");
        var qualityContainer = root.Q<VisualElement>("QaulityContainer"); // MET TYPO!

        // Setup mouse hover voor alle containers
        if (musicContainer != null)
        {
            musicContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            musicContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement); // FIXED: MouseLeaveEvent
            Debug.Log("✅ Music container mouse hover registered");
        }

        if (sfxContainer != null)
        {
            sfxContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            sfxContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement); // FIXED: MouseLeaveEvent
            Debug.Log("✅ SFX container mouse hover registered");
        }

        if (resolutionContainer != null)
        {
            resolutionContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            resolutionContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement); // FIXED: MouseLeaveEvent
            Debug.Log("✅ Resolution container mouse hover registered");
        }

        if (qualityContainer != null)
        {
            qualityContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            qualityContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement); // FIXED: MouseLeaveEvent
            Debug.Log($"✅ Quality container mouse hover registered: {qualityContainer.name}");
        }
        else
        {
            Debug.LogWarning("⚠️ QaulityContainer not found for mouse hover!");
        }
    }
    #endregion

    #region INPUT MODE MANAGEMENT - AC Syndicate Controller vs Mouse

    // VERBETERDE input mode switching met betere visuele feedback
    void SwitchToInputMode(InputMode newMode)
    {
        if (currentInputMode == newMode) return;

        currentInputMode = newMode;

        // Clear alle highlights
        ClearAllHighlights();
        ClearAllMouseHighlights();

        var root = uiDocument.rootVisualElement;
        var controllerHint = root.Q<Label>("ControllerHint");

        if (currentInputMode == InputMode.Controller)
        {
            // Controller mode: highlight current selection MAAR ALLEEN ALS ER MEERDERE ELEMENTEN ZIJN
            if (selectableElements.Count > 1)
            {
                HighlightCurrentElement();
            }
            else if (selectableElements.Count == 1)
            {
                // Voor panels met alleen een back button: geen automatische highlight
                // De gebruiker moet expliciet navigeren om de button te selecteren
                currentSelection = 0; // Zet selection klaar, maar highlight niet
            }

            if (controllerHint != null)
            {
                controllerHint.style.display = DisplayStyle.Flex;
            }

            Debug.Log("🎮 AC SYNDICATE CONTROLLER MODE ACTIVE");
        }
        else
        {
            // Mouse mode: hide controller hint
            if (controllerHint != null)
            {
                controllerHint.style.display = DisplayStyle.None;
            }

            Debug.Log("🖱️ MOUSE MODE ACTIVE");
        }
    }

    void OnMouseEnterElement(MouseEnterEvent evt)
    {
        // Switch naar mouse mode als mouse beweegt
        SwitchToInputMode(InputMode.Mouse);

        // Highlight het element
        var element = evt.target as VisualElement;
        if (element != null)
        {
            element.AddToClassList("mouse-hover");
        }
    }

    // FIXED: Correct parameter type
    void OnMouseLeaveElement(MouseLeaveEvent evt) // FIXED: MouseLeaveEvent instead of OnMouseLeaveEvent
    {
        // Remove highlight
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

    #region AUDIO INSTELLINGEN - GEFIXT VOOR CROSS-CONTAMINATION

    void SetupAudioSettings()
    {
        var root = uiDocument.rootVisualElement;

        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");

        // VERBETERD: Separate callbacks om cross-contamination te voorkomen
        if (musicSlider != null)
        {
            musicSlider.RegisterValueChangedCallback(evt =>
            {
                SetMusicVolume(evt.newValue);
                Debug.Log($"🎵 Music volume ONLY: {evt.newValue:F2}");
            });
            Debug.Log("✅ Music slider callback registered");
        }
        else
        {
            Debug.LogWarning("⚠️ MusicSlider not found in UXML");
        }

        if (sfxSlider != null)
        {
            sfxSlider.RegisterValueChangedCallback(evt =>
            {
                SetSFXVolume(evt.newValue);
                Debug.Log($"🔊 SFX volume ONLY: {evt.newValue:F2}");
            });
            Debug.Log("✅ SFX slider callback registered");
        }
        else
        {
            Debug.LogWarning("⚠️ SFXSlider not found in UXML");
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
            if (!success)
            {
                Debug.LogWarning("❌ Failed to set MusicVolume parameter in AudioMixer");
            }
        }
    }

    void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            bool success = audioMixer.SetFloat("SFXVolume", dbValue);
            if (!success)
            {
                Debug.LogWarning("❌ Failed to set SFXVolume parameter in AudioMixer");
            }
        }
    }

    #endregion

    #region QUALITY INSTELLINGEN - GEFIXT VOOR BEIDE SPELLINGEN

    void SetupQualitySettings()
    {
        currentQualityIndex = QualitySettings.GetQualityLevel();
        UpdateQualityText();

        // BEIDE SPELLINGEN PROBEREN VOOR BACKWARDS COMPATIBILITY
        RegisterButtonClick("QualityPrevBtn", () => ChangeQuality(-1));
        RegisterButtonClick("QaulityPrevBtn", () => ChangeQuality(-1));  // Oude typo versie
        RegisterButtonClick("QualityNextBtn", () => ChangeQuality(1));
        RegisterButtonClick("QaulityNextBtn", () => ChangeQuality(1));   // Oude typo versie

        Debug.Log($"🎨 Quality levels: {string.Join(", ", QualitySettings.names)}");
    }

    void ChangeQuality(int direction)
    {
        int totalQualityLevels = QualitySettings.names.Length;
        currentQualityIndex = (currentQualityIndex + direction + totalQualityLevels) % totalQualityLevels;

        QualitySettings.SetQualityLevel(currentQualityIndex);
        UpdateQualityText();

        Debug.Log($"🎨 Quality: {QualitySettings.names[currentQualityIndex]}");
    }

    void UpdateQualityText()
    {
        var qualityLabel = uiDocument.rootVisualElement.Q<Label>("QaulityLabel");
        if (qualityLabel == null)
        {
            Debug.LogWarning("QaulityLabel not found!");
            return;
        }

        if (currentQualityIndex < QualitySettings.names.Length)
        {
            qualityLabel.text = QualitySettings.names[currentQualityIndex];
        }
    }

    #endregion

    #region PANEL MANAGEMENT - VERBETERDE ERROR HANDLING

    void ShowMainMenu()
    {
        HideAllPanels();
        SetupMainMenuElements();
        Debug.Log("🏠 Main Menu Active");
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

            Debug.Log($"📋 Panel Active: {panelName}");
        }
        else
        {
            Debug.LogError($"❌ Panel '{panelName}' not found in UXML! Available panels should be created in UI Builder.");
            Debug.LogError($"💡 TIP: Open UI Builder and add a VisualElement with name='{panelName}' to your UXML file.");
            // Fallback: ga terug naar main menu
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

    #region CONTROLLER NAVIGATION - AC Syndicate Style VERBETERD

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

    // VERBETERDE panel setup voor betere visuele feedback
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

        // VERBETERD: Alleen highlight als we in controller mode zijn EN er meerdere elementen zijn
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
            // Voor panels met alleen back button: selection ready maar geen highlight
            currentSelection = 0;
            // Geen automatische highlight!
        }

        Debug.Log($"📋 Panel '{panelName}' setup: {selectableElements.Count} selectable elements");
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

        // Music Volume - ISOLATED SETUP
        var musicSlider = root.Q<Slider>("MusicSlider");
        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        if (musicSlider != null)
        {
            selectableElements.Add(new MenuElement("Music Volume", musicSlider, musicContainer));
            Debug.Log("✅ Music Volume container isolated");
        }

        // SFX Volume - ISOLATED SETUP
        var sfxSlider = root.Q<Slider>("SFXSlider");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");
        if (sfxSlider != null)
        {
            selectableElements.Add(new MenuElement("SFX Volume", sfxSlider, sfxContainer));
            Debug.Log("✅ SFX Volume container isolated");
        }

        AddButtonElement("CloseSound", "Back");
    }

    // GEFIXTE VIDEO ELEMENTS - Quality met juiste container naam!
    void SetupVideoElements()
    {
        var root = uiDocument.rootVisualElement;

        // RESOLUTION setup (blijft hetzelfde)
        var resolutionPrevBtn = root.Q<Button>("ResolutionPrevBtn");
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");

        if (resolutionPrevBtn != null && resolutionContainer != null)
        {
            selectableElements.Add(new MenuElement("Resolution", resolutionPrevBtn, resolutionContainer));
            Debug.Log("✅ Resolution: Button + Container added");
        }

        // QUALITY setup - MET DE JUISTE CONTAINER NAAM!
        var qualityPrevBtn = root.Q<Button>("QaulityPrevBtn"); // Typo versie werkt
        var qualityContainer = root.Q<VisualElement>("QaulityContainer"); // MET TYPO!

        Debug.Log($"🔍 Quality button found: {qualityPrevBtn != null}");
        Debug.Log($"🔍 Quality container found: {qualityContainer != null}");

        if (qualityPrevBtn != null && qualityContainer != null)
        {
            selectableElements.Add(new MenuElement("Quality", qualityPrevBtn, qualityContainer));
            Debug.Log("✅ Quality: Button + Container added - FIXED WITH CORRECT NAME!");
        }
        else if (qualityPrevBtn != null)
        {
            // Fallback: Alleen button
            selectableElements.Add(new MenuElement("Quality", qualityPrevBtn));
            Debug.Log("⚠️ Quality: Only button added (no container)");
        }
        else
        {
            Debug.LogError("❌ NO Quality button found!");
        }

        // Back button
        AddButtonElement("CloseVideo", "Back");

        Debug.Log($"📋 Video setup: {selectableElements.Count} elements total");
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
            Debug.Log($"✅ Added button element: {displayName}");
        }
        else
        {
            Debug.LogWarning($"⚠️ Button '{buttonName}' not found for element '{displayName}'");
        }
    }

    // NIEUWE methode om expliciet highlighting te starten
    void StartControllerNavigation()
    {
        if (currentInputMode == InputMode.Controller && selectableElements.Count > 0)
        {
            currentSelection = 0;
            HighlightCurrentElement();
        }
    }

    // VERBETERDE AC Syndicate style highlighting
    void HighlightCurrentElement()
    {
        if (currentInputMode != InputMode.Controller) return;

        ClearAllHighlights();

        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];

            if (element.button != null)
            {
                // Als er GEEN aparte container is, highlight de button
                if (element.container == element.button || element.container == null)
                {
                    element.button.AddToClassList("ac-button-selected");
                    element.button.AddToClassList("ac-pulse");
                    element.button.Focus();

                    Debug.Log($"🔴 Button highlighted: {element.name}");
                }
                // Als er WEL een aparte container is, highlight de container
                else
                {
                    element.container.AddToClassList("controller-selected");
                    element.container.AddToClassList("ac-pulse");

                    // EXTRA DEBUG VOOR QUALITY
                    if (element.name == "Quality")
                    {
                        Debug.Log($"🔴🔴🔴 QUALITY CONTAINER HIGHLIGHTED!");
                        Debug.Log($"Container name: {element.container.name}");
                        Debug.Log($"Container classes: {string.Join(", ", element.container.GetClasses())}");
                    }

                    Debug.Log($"🔴 Container highlighted: {element.name}");
                }
            }
            else if (element.slider != null && element.container != null)
            {
                element.container.AddToClassList("controller-selected");
                element.container.AddToClassList("ac-pulse");

                Debug.Log($"🔴 Slider container highlighted: {element.name}");
            }

            Debug.Log($"🎯 AC Syndicate Selected: {element.name}");
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

    #region INPUT HANDLING - VERBETERDE Controller Support

    void Update()
    {
        DetectInputModeChanges();

        if (currentInputMode == InputMode.Controller)
        {
            HandleControllerInput();
        }
    }

    // Verbeterde input detectie voor AC Syndicate feel
    void DetectInputModeChanges()
    {
        // Controller input detectie - verbeterd
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

        // Mouse movement detectie
        if (Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 1f)
        {
            SwitchToInputMode(InputMode.Mouse);
        }
    }

    // VERBETERDE controller input voor AC Syndicate responsiviteit
    void HandleControllerInput()
    {
        if (Gamepad.current == null) return;
        if (Time.time - lastInputTime < INPUT_DELAY) return;

        Vector2 stick = Gamepad.current.leftStick.ReadValue();
        Vector2 dpad = Gamepad.current.dpad.ReadValue();
        Vector2 combinedInput = stick + dpad;

        float threshold = 0.5f;

        // VERBETERD: Als er nog geen element geselecteerd is, start navigatie
        bool hasActiveSelection = selectableElements.Count > 0 &&
                                currentSelection < selectableElements.Count &&
                                (selectableElements[currentSelection].button?.ClassListContains("ac-button-selected") == true ||
                                 selectableElements[currentSelection].container?.ClassListContains("controller-selected") == true);

        // Up/Down navigatie
        if (combinedInput.y > threshold && !wasUp)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                // Start navigatie
                StartControllerNavigation();
            }
            else
            {
                NavigateUp();
            }
            wasUp = true;
            lastInputTime = Time.time;
            PlayNavigationSound();
        }
        else if (combinedInput.y <= threshold) wasUp = false;

        if (combinedInput.y < -threshold && !wasDown)
        {
            if (!hasActiveSelection && selectableElements.Count > 0)
            {
                // Start navigatie
                StartControllerNavigation();
            }
            else
            {
                NavigateDown();
            }
            wasDown = true;
            lastInputTime = Time.time;
            PlayNavigationSound();
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
                PlayConfirmSound();
            }
        }

        // B button (back)
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            GoBack();
            PlayBackSound();
        }
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

    // SIMPEL - Laat users alle resoluties proberen
    void AdjustCurrentElement(int direction)
    {
        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];

            if (element.slider != null)
            {
                float change = direction * 0.1f;
                element.slider.value = Mathf.Clamp(element.slider.value + change, 0f, 1f);
                Debug.Log($"🎵 Slider adjusted: {element.name} = {element.slider.value:F2}");
            }
            else if (element.name == "Resolution")
            {
                // SIMPEL: Laat users kiezen wat ze willen
                ChangeResolution(direction);
            }
            else if (element.name == "Quality")
            {
                ChangeQuality(direction);
                Debug.Log($"🎨 Quality adjusted: {direction} -> {QualitySettings.names[currentQualityIndex]}");
            }
            else
            {
                Debug.LogWarning($"⚠️ Cannot adjust element: {element.name}");
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

    // AC Syndicate style audio feedback
    void PlayNavigationSound()
    {
        Debug.Log("🔊 AC Navigation");
        // Voeg hier je navigation sound toe
    }

    void PlayConfirmSound()
    {
        Debug.Log("🔊 AC Confirm");
        // Voeg hier je confirm sound toe
    }

    void PlayBackSound()
    {
        Debug.Log("🔊 AC Back");
        // Voeg hier je back sound toe
    }

    #endregion

    #region GAME ACTIONS

    void StartGame()
    {
        SaveAllSettings();
        SceneManager.LoadScene("Test sceme");
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

        // NORMALE RESOLUTIE SAVE
        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);
        PlayerPrefs.SetInt("QualityLevel", currentQualityIndex);

        PlayerPrefs.Save();
        Debug.Log("💾 Settings saved!");
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

        // LOAD RESOLUTIE
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        if (currentResolutionIndex >= 0 && currentResolutionIndex < resolutions.Count)
        {
            var res = resolutions[currentResolutionIndex];
            Screen.SetResolution(res.x, res.y, FullScreenMode.FullScreenWindow);
            Debug.Log($"🔧 Loaded resolution: {res.x}x{res.y}");
        }
        UpdateResolutionText();

        currentQualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(currentQualityIndex);
        UpdateQualityText();

        Debug.Log("📁 Settings loaded!");
    }

    #endregion

    #region DEBUG METHODS

    // DEBUG METHOD - Log alle UI elementen voor troubleshooting
    void LogAllUIElements()
    {
        var root = uiDocument.rootVisualElement;

        Debug.Log("=== 🔍 UI ELEMENTS DEBUG ===");

        // Check alle panels
        var panels = new string[] { "Options", "Video", "Sound", "Credits", "Controls" };
        foreach (string panelName in panels)
        {
            var panel = root.Q<VisualElement>(panelName);
            Debug.Log($"Panel '{panelName}' found: {panel != null}");
        }

        // Check Video elements
        var qualityBtn1 = root.Q<Button>("QualityPrevBtn");
        var qualityBtn2 = root.Q<Button>("QaulityPrevBtn");
        var qualityContainer = root.Q<VisualElement>("qualityContainer");
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");

        Debug.Log($"QualityPrevBtn found: {qualityBtn1 != null}");
        Debug.Log($"QaulityPrevBtn found: {qualityBtn2 != null}");
        Debug.Log($"qualityContainer found: {qualityContainer != null}");
        Debug.Log($"ResolutionContainer found: {resolutionContainer != null}");

        // Check Sound elements
        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");
        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");

        Debug.Log($"MusicSlider found: {musicSlider != null}");
        Debug.Log($"SFXSlider found: {sfxSlider != null}");
        Debug.Log($"MusicVolumeContainer found: {musicContainer != null}");
        Debug.Log($"SFXVolumeContainer found: {sfxContainer != null}");

        // List alle buttons die bestaan
        var allButtons = root.Query<Button>().ToList();
        Debug.Log($"📋 Total buttons found: {allButtons.Count}");
        foreach (var btn in allButtons)
        {
            Debug.Log($"  🔘 Button: {btn.name} (text: '{btn.text}')");
        }

        // List alle VisualElements
        var allElements = root.Query<VisualElement>().ToList();
        Debug.Log($"📋 Total VisualElements found: {allElements.Count}");
        foreach (var elem in allElements)
        {
            if (!string.IsNullOrEmpty(elem.name))
            {
                Debug.Log($"  📦 Element: {elem.name} (type: {elem.GetType().Name})");
            }
        }

        Debug.Log("=== 🔍 END UI ELEMENTS DEBUG ===");
    }
    #endregion
}
