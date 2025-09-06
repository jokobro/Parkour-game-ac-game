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
    private const float INPUT_DELAY = 0.15f;

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

        // Start met main menu
        ShowMainMenu();
        LoadSavedSettings();

        // Start in mouse mode
        SwitchToInputMode(InputMode.Mouse);

        Debug.Log("Main Menu Setup Complete - Mouse Mode Active");
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

        // Verstop alle panels
        HideAllPanels();
    }

    void SetupButtonClicks()
    {
        // Main menu buttons
        RegisterButtonClick("ContinueButton", StartGame);
        RegisterButtonClick("OptionsButton", () => ShowPanel("Options"));
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

        // Containers (voor Sound panel)
        var musicContainer = root.Q<VisualElement>("MusicVolumeContainer");
        var sfxContainer = root.Q<VisualElement>("SFXVolumeContainer");

        // Quality container
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

        if (qualityContainer != null)
        {
            qualityContainer.RegisterCallback<MouseEnterEvent>(OnMouseEnterElement);
            qualityContainer.RegisterCallback<MouseLeaveEvent>(OnMouseLeaveElement);
            Debug.Log("Quality container mouse hover setup complete");
        }
        else
        {
            Debug.LogWarning("qualityContainer not found for mouse hover");
        }
    }
    #endregion

    #region INPUT MODE MANAGEMENT - Controller vs Mouse
    void SwitchToInputMode(InputMode newMode)
    {
        if (currentInputMode == newMode) return;

        currentInputMode = newMode;

        // Clear alle highlights
        ClearAllHighlights();
        ClearAllMouseHighlights();

        if (currentInputMode == InputMode.Controller)
        {
            // Controller mode: highlight current selection
            HighlightCurrentElement();
            Debug.Log("🎮 CONTROLLER MODE ACTIVE");
        }
        else
        {
            // Mouse mode: no controller highlights
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
            Debug.Log($"Mouse hover: {element.name}");
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

    #region AUDIO INSTELLINGEN
    void SetupAudioSettings()
    {
        var root = uiDocument.rootVisualElement;

        // Sliders
        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");

        if (musicSlider != null)
            musicSlider.RegisterValueChangedCallback(evt => SetMusicVolume(evt.newValue));

        if (sfxSlider != null)
            sfxSlider.RegisterValueChangedCallback(evt => SetSFXVolume(evt.newValue));

        // Volume buttons voor PC
        SetupVolumeButtons();
    }

    void SetupVolumeButtons()
    {
        var root = uiDocument.rootVisualElement;

        // Music volume +/- buttons
        var musicSlider = root.Q<Slider>("MusicSlider");
        RegisterButtonClick("MusicDecreaseBtn", () => ChangeSliderValue(musicSlider, -0.1f));
        RegisterButtonClick("MusicIncreaseBtn", () => ChangeSliderValue(musicSlider, 0.1f));

        // SFX volume +/- buttons
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
            // Converteer 0-1 naar decibels
            float dbValue = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat("MusicVolume", dbValue);
        }
    }

    void SetSFXVolume(float volume)
    {
        if (audioMixer != null)
        {
            float dbValue = Mathf.Log10(Mathf.Clamp(volume, 0.0001f, 1f)) * 20f;
            audioMixer.SetFloat("SFXVolume", dbValue);
        }
    }
    #endregion

    #region RESOLUTIE INSTELLINGEN
    void SetupResolutionSettings()
    {
        // Krijg ALLEEN resoluties die je monitor ondersteunt
        var supportedResolutions = Screen.resolutions;
        resolutions.Clear();

        // Converteer naar onze List en remove duplicates
        HashSet<Vector2Int> uniqueResolutions = new HashSet<Vector2Int>();
        foreach (var res in supportedResolutions)
        {
            Vector2Int resolution = new Vector2Int(res.width, res.height);
            uniqueResolutions.Add(resolution);
        }

        // Sorteer van groot naar klein
        resolutions = uniqueResolutions.OrderByDescending(r => r.x * r.y).ToList();

        // Vind huidige resolutie
        Vector2Int currentRes = new Vector2Int(Screen.currentResolution.width, Screen.currentResolution.height);
        currentResolutionIndex = resolutions.FindIndex(r => r == currentRes);
        if (currentResolutionIndex == -1) currentResolutionIndex = 0;

        UpdateResolutionText();

        // Resolution change buttons
        RegisterButtonClick("ResolutionPrevBtn", () => ChangeResolution(-1));
        RegisterButtonClick("ResolutionNextBtn", () => ChangeResolution(1));

        Debug.Log($"Found {resolutions.Count} supported resolutions for your monitor");
    }

    void ChangeResolution(int direction)
    {
        if (resolutions.Count == 0) return;

        currentResolutionIndex = (currentResolutionIndex + direction + resolutions.Count) % resolutions.Count;

        var newRes = resolutions[currentResolutionIndex];
        Screen.SetResolution(newRes.x, newRes.y, FullScreenMode.FullScreenWindow);

        UpdateResolutionText();
        Debug.Log($"Resolution changed to: {newRes.x}x{newRes.y}");
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

    #region QUALITY INSTELLINGEN
    void SetupQualitySettings()
    {
        // Krijg alle beschikbare quality levels
        currentQualityIndex = QualitySettings.GetQualityLevel();

        UpdateQualityText();

        // Quality change buttons - GEFIXTE NAMEN
        RegisterButtonClick("QaulityPrevBtn", () => ChangeQuality(-1));    // QaulityPrevBtn
        RegisterButtonClick("QualityNextBtn", () => ChangeQuality(1));     // QualityNextBtn blijft goed

        Debug.Log($"Quality levels available: {string.Join(", ", QualitySettings.names)}");
    }

    void ChangeQuality(int direction)
    {
        int totalQualityLevels = QualitySettings.names.Length;
        currentQualityIndex = (currentQualityIndex + direction + totalQualityLevels) % totalQualityLevels;

        QualitySettings.SetQualityLevel(currentQualityIndex);
        UpdateQualityText();

        Debug.Log($"Quality changed to: {QualitySettings.names[currentQualityIndex]}");
    }

    void UpdateQualityText()
    {
        // Gebruik de correcte naam uit je UXML (QaulityLabel - met foutje in spelling)
        var qualityLabel = uiDocument.rootVisualElement.Q<Label>("QaulityLabel");

        if (qualityLabel == null)
        {
            Debug.LogWarning("QaulityLabel not found! Check if it exists in qualityContainer");
            return;
        }

        if (currentQualityIndex < QualitySettings.names.Length)
        {
            qualityLabel.text = QualitySettings.names[currentQualityIndex];
            Debug.Log($"Quality text updated to: {QualitySettings.names[currentQualityIndex]}");
        }
    }
    #endregion

    #region PANEL MANAGEMENT - Welk scherm wordt getoond
    void ShowMainMenu()
    {
        HideAllPanels();
        SetupMainMenuElements();
        Debug.Log("Showing Main Menu");
    }

    void ShowPanel(string panelName)
    {
        HideAllPanels();

        var panel = uiDocument.rootVisualElement.Q<VisualElement>(panelName);
        if (panel != null)
        {
            // Toon het panel
            panel.style.display = DisplayStyle.Flex;
            panel.style.visibility = Visibility.Visible;

            // Setup welke elementen je kunt selecteren
            SetupPanelElements(panelName);

            Debug.Log($"Showing panel: {panelName}");
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

    #region CONTROLLER NAVIGATION - Rode highlighting en navigatie
    void SetupMainMenuElements()
    {
        selectableElements.Clear();
        var root = uiDocument.rootVisualElement;

        // Main menu buttons
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

        currentSelection = 0;
        if (currentInputMode == InputMode.Controller)
        {
            HighlightCurrentElement();
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

        // Volume sliders met containers (voor rode highlighting)
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

        // Quality container met GEFIXTE NAAM
        var qualityContainer = root.Q<VisualElement>("qualityContainer");
        var qualityPrevBtn = root.Q<Button>("QaulityPrevBtn");  // QaulityPrevBtn met foutje

        if (qualityPrevBtn != null && qualityContainer != null)
        {
            // Gebruik container voor highlighting, button voor functionaliteit
            selectableElements.Add(new MenuElement("Quality", qualityPrevBtn, qualityContainer));
        }
        else if (qualityPrevBtn != null)
        {
            // Fallback als geen container - GEFIXTE NAAM
            AddButtonElement("QaulityPrevBtn", "Quality");  // QaulityPrevBtn met foutje
        }

        // Resolution
        AddButtonElement("ResolutionPrevBtn", "Resolution");

        // Back button
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

    void HighlightCurrentElement()
    {
        // Alleen highlight als controller mode actief is!
        if (currentInputMode != InputMode.Controller) return;

        // Verwijder alle rode highlights
        ClearAllHighlights();

        // Highlight het huidige element
        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];
            element.container?.AddToClassList("controller-selected");

            // Focus voor keyboard input
            if (element.button != null)
                element.button.Focus();
            else if (element.slider != null)
                element.slider.Focus();

            Debug.Log($"🎮 Controller Selected: {element.name}");
        }
    }

    void ClearAllHighlights()
    {
        foreach (var element in selectableElements)
        {
            element.container?.RemoveFromClassList("controller-selected");
        }
    }
    #endregion

    #region INPUT HANDLING - Controller en keyboard input
    void Update()
    {
        // Check voor input mode switches
        DetectInputModeChanges();

        // Handle input alleen voor actieve mode
        if (currentInputMode == InputMode.Controller)
        {
            HandleControllerInput();
        }
    }

    void DetectInputModeChanges()
    {
        // Controller input detectie
        if (Gamepad.current != null)
        {
            Vector2 stick = Gamepad.current.leftStick.ReadValue();
            bool anyButtonPressed = Gamepad.current.buttonSouth.wasPressedThisFrame ||
                                  Gamepad.current.buttonEast.wasPressedThisFrame ||
                                  Gamepad.current.buttonWest.wasPressedThisFrame ||
                                  Gamepad.current.buttonNorth.wasPressedThisFrame;

            if (stick.magnitude > 0.3f || anyButtonPressed)
            {
                SwitchToInputMode(InputMode.Controller);
            }
        }

        // Mouse movement wordt al gedetecteerd in OnMouseEnterElement
    }

    void HandleControllerInput()
    {
        if (Gamepad.current == null) return;

        // Te snel input voorkomen
        if (Time.time - lastInputTime < INPUT_DELAY) return;

        Vector2 stick = Gamepad.current.leftStick.ReadValue();
        float threshold = 0.6f;

        // Up/Down navigatie
        if (stick.y > threshold && !wasUp)
        {
            NavigateUp();
            wasUp = true;
            lastInputTime = Time.time;
        }
        else if (stick.y <= threshold) wasUp = false;

        if (stick.y < -threshold && !wasDown)
        {
            NavigateDown();
            wasDown = true;
            lastInputTime = Time.time;
        }
        else if (stick.y >= -threshold) wasDown = false;

        // Left/Right voor sliders en resolutie
        if (stick.x < -threshold && !wasLeft)
        {
            AdjustCurrentElement(-1);
            wasLeft = true;
            lastInputTime = Time.time;
        }
        else if (stick.x >= -threshold) wasLeft = false;

        if (stick.x > threshold && !wasRight)
        {
            AdjustCurrentElement(1);
            wasRight = true;
            lastInputTime = Time.time;
        }
        else if (stick.x <= threshold) wasRight = false;

        // A button (confirm)
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            ActivateCurrentElement();
        }

        // B button (back)
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            GoBack();
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

    void AdjustCurrentElement(int direction)
    {
        if (currentSelection < selectableElements.Count)
        {
            var element = selectableElements[currentSelection];

            // Als het een slider is
            if (element.slider != null)
            {
                float change = direction * 0.1f;
                element.slider.value = Mathf.Clamp(element.slider.value + change, 0f, 1f);
            }
            // Als het resolutie is
            else if (element.name == "Resolution")
            {
                ChangeResolution(direction);
            }
            // Als het quality is
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
                // Correcte manier om button click te simuleren in UI Toolkit
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
        // Zoek back button
        var backElement = selectableElements.Find(e => e.name == "Back");
        if (backElement?.button != null)
        {
            // Correcte manier om button click te simuleren
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
    #endregion

    #region GAME ACTIONS - Start/Quit/Save
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

        // Save audio
        var musicSlider = root.Q<Slider>("MusicSlider");
        var sfxSlider = root.Q<Slider>("SFXSlider");

        if (musicSlider != null) PlayerPrefs.SetFloat("MusicVolume", musicSlider.value);
        if (sfxSlider != null) PlayerPrefs.SetFloat("SFXVolume", sfxSlider.value);

        // Save resolution
        PlayerPrefs.SetInt("ResolutionIndex", currentResolutionIndex);

        // Save quality
        PlayerPrefs.SetInt("QualityLevel", currentQualityIndex);

        PlayerPrefs.Save();
        Debug.Log("Settings saved!");
    }

    void LoadSavedSettings()
    {
        var root = uiDocument.rootVisualElement;

        // Load audio
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

        // Load resolution
        currentResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        UpdateResolutionText();

        // Load quality
        currentQualityIndex = PlayerPrefs.GetInt("QualityLevel", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(currentQualityIndex);
        UpdateQualityText();

        Debug.Log("Settings loaded!");
    }
    #endregion
}