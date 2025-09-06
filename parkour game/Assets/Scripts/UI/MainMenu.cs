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

    // Resolutie instellingen
    private List<Vector2Int> resolutions = new List<Vector2Int>();
    private int currentResolutionIndex = 0;

    // Quality instellingen
    private int currentQualityIndex = 0;

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

    #region SETUP - Initialisatie van het menu
    void Setup()
    {
        // Basis setup
        uiDocument = GetComponent<UIDocument>();
        FindAllPanels();
        SetupButtonClicks();
        SetupAudioSettings();
        SetupResolutionSettings();
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

        Debug.Log("🎮 AC Syndicate Main Menu Setup Complete");
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

    // Setup mouse hover events voor containers
    void SetupMouseHoverEvents()
    {
        var root = uiDocument.rootVisualElement;

        // Alle buttons
        var allButtons = root.Query<Button>().ToList();
        foreach (var button in allButtons)
        {
            button.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            button.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
        }

        // Sound containers (alleen als ze bestaan)
        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");

        // Video containers (alleen als ze bestaan)
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");
        var qualityContainer = root.Q<VisualElement>("qualityContainer");

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

    void OnMouseLeaveElement(MouseLeaveEvent evt)
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

    #region RESOLUTIE INSTELLINGEN
    void SetupResolutionSettings()
    {
        var supportedResolutions = Screen.resolutions;
        resolutions.Clear();

        HashSet<Vector2Int> uniqueResolutions = new HashSet<Vector2Int>();
        foreach (var res in supportedResolutions)
        {
            Vector2Int resolution = new Vector2Int(res.width, res.height);
            uniqueResolutions.Add(resolution);
        }

        resolutions = uniqueResolutions.OrderByDescending(r => r.x * r.y).ToList();

        Vector2Int currentRes = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        currentResolutionIndex = resolutions.FindIndex(r => r == currentRes);
        if (currentResolutionIndex == -1) currentResolutionIndex = 0;

        UpdateResolutionText();

        RegisterButtonClick("ResolutionPrevBtn", () => ChangeResolution(-1));
        RegisterButtonClick("ResolutionNextBtn", () => ChangeResolution(1));

        Debug.Log($"📺 Found {resolutions.Count} resolutions");
    }

    void ChangeResolution(int direction)
    {
        if (resolutions.Count == 0) return;

        currentResolutionIndex = (currentResolutionIndex + direction + resolutions.Count) % resolutions.Count;

        var newRes = resolutions[currentResolutionIndex];
        Screen.SetResolution(newRes.x, newRes.y, FullScreenMode.FullScreenWindow);

        UpdateResolutionText();
        Debug.Log($"📺 Resolution: {newRes.x}x{newRes.y}");
    }

    void UpdateResolutionText()
    {
        var resolutionLabel = uiDocument.rootVisualElement.Q<Label>("ResolutionLabel");
        if (resolutionLabel != null && currentResolutionIndex < resolutions.Count)
        {
            var res = resolutions[currentResolutionIndex];
            resolutionLabel.text = $"{res.x} x {res.y}";
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

    // KRITIEKE FIX VOOR QUALITY PROBLEEM
    void SetupVideoElements()
    {
        var root = uiDocument.rootVisualElement;

        // DIRECTE BUTTON SETUP - als containers niet bestaan
        var resolutionPrevBtn = root.Q<Button>("ResolutionPrevBtn");
        var qualityPrevBtn = root.Q<Button>("QualityPrevBtn") ?? root.Q<Button>("QaulityPrevBtn");

        // Probeer containers te vinden
        var resolutionContainer = root.Q<VisualElement>("ResolutionContainer");
        var qualityContainer = root.Q<VisualElement>("qualityContainer");

        // RESOLUTION SETUP
        if (resolutionPrevBtn != null)
        {
            if (resolutionContainer != null)
            {
                selectableElements.Add(new MenuElement("Resolution", resolutionPrevBtn, resolutionContainer));
                Debug.Log("✅ Resolution with container added");
            }
            else
            {
                selectableElements.Add(new MenuElement("Resolution", resolutionPrevBtn));
                Debug.Log("✅ Resolution button only added (no container)");
            }
        }

        // QUALITY SETUP - KRITIEKE FIX
        if (qualityPrevBtn != null)
        {
            if (qualityContainer != null)
            {
                selectableElements.Add(new MenuElement("Quality", qualityPrevBtn, qualityContainer));
                Debug.Log($"✅ Quality with container added: {qualityPrevBtn.name}");
            }
            else
            {
                // FALLBACK: Als container niet bestaat, behandel button als normaal element
                selectableElements.Add(new MenuElement("Quality", qualityPrevBtn));
                Debug.Log($"✅ Quality button added WITHOUT container: {qualityPrevBtn.name}");
            }
        }
        else
        {
            Debug.LogError("❌ NO Quality button found! Checking for: QualityPrevBtn, QaulityPrevBtn");

            // DEBUG: List alle buttons in Video panel
            var allButtons = root.Query<Button>().ToList();
            Debug.Log("📋 All buttons in UXML:");
            foreach (var btn in allButtons)
            {
                Debug.Log($"  🔘 {btn.name}");
            }
        }

        AddButtonElement("CloseVideo", "Back");

        Debug.Log($"📋 Video panel setup complete: {selectableElements.Count} elements");
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

    // KRITIEKE FIX VOOR QUALITY ADJUSTMENT
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
                ChangeResolution(direction);
                Debug.Log($"📺 Resolution adjusted: {direction}");
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

        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
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
