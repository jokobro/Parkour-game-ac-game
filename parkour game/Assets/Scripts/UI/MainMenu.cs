using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class MainMenu : MonoBehaviour
{
    private UIDocument UIDocument;
    private VisualElement[] overlayPanels;
    private Dictionary<Button, EventCallback<ClickEvent>> registeredCallbacks = new();
    // Input mode tracking
    private InputDeviceDetector.InputMode lastInputMode = InputDeviceDetector.InputMode.None;
    // Navigatie state
    private bool wasDown, wasUp;
    private float lastNavigationTime = 0f;
    private const float NAVIGATION_COOLDOWN = 0.2f;

    private void Awake()
    {
        UIDocument = GetComponent<UIDocument>();
        var root = UIDocument.rootVisualElement;

        overlayPanels = new VisualElement[]
        {
            root.Q<VisualElement>("Options"),
            root.Q<VisualElement>("Credits"),
            root.Q<VisualElement>("Sound"),
            root.Q<VisualElement>("Video"),
            root.Q<VisualElement>("Controls"),
        };

        HideAllPanels();
        SetupButtons();

        // Voor gamepad: zet default focus
        // Voor mouse/keyboard: geen default focus
        if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.Gamepad)
        {
            var defaultButton = root.Q<Button>("ContinueButton");
            defaultButton?.Focus();
        }

        // Voor gamepad: zet default focus op eerste knop
        if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.Gamepad)
        {
            FocusTopButton();
        }
        EnhanceButtonFeedback();
    }

    private void Start()
    {
        var eventSystem = FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.sendNavigationEvents = false;
        }
    }

    private void FocusTopButton()
    {
        var visibleButtons = GetVisibleButtons();
        if (visibleButtons.Count > 0)
        {
            visibleButtons[0].Focus();
        }
    }

    private void SetupButtons()
    {
        RegisterButton("ContinueButton", _ => StartGame());
        RegisterButton("QuitButton", _ => QuitGame());

        RegisterButton("creditsButton", _ => ShowPanel("Credits"));
        RegisterButton("SoundButton", _ => ShowPanel("Sound"));
        RegisterButton("VideoButton", _ => ShowPanel("Video"));
        RegisterButton("ControlsButton", _ => ShowPanel("Controls"));

        RegisterButton("BackButton", _ => HideAllPanels());
        RegisterButton("OptionsButton", _ => ShowPanel("Options"));

        RegisterButton("CloseCredits", _ => ShowPanel("Options"));
        RegisterButton("CloseSound", _ => ShowPanel("Options"));
        RegisterButton("CloseVideo", _ => ShowPanel("Options"));
        RegisterButton("CloseControls", _ => ShowPanel("Options"));
    }

    private void StartGame()
    {
        SceneManager.LoadScene("Test sceme");
    }

    private void QuitGame()
    {
        SettingsMenu.Instance.SaveVolume();
        Application.Quit();
    }

    private void ShowPanel(string panelName)
    {
        HideAllPanels();
        var panel = UIDocument.rootVisualElement.Q<VisualElement>(panelName);
        if (panel != null)
        {
            panel.style.visibility = Visibility.Visible;
            FocusFirstButtonInPanel(panel);
        }
    }

    private void FocusFirstButtonInPanel(VisualElement panel)
    {
        // Alleen focussen bij gamepad input
        if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.Gamepad)
        {
            StartCoroutine(FocusFirstButtonCoroutine(panel));
        }
    }

    private IEnumerator FocusFirstButtonCoroutine(VisualElement panel)
    {
        yield return null;

        var visibleButtons = GetVisibleButtons();
        if (visibleButtons.Count > 0)
        {
            visibleButtons[0].Focus();
        }
    }

    private void HideAllPanels()
    {
        foreach (var panel in overlayPanels)
        {
            if (panel != null)
                panel.style.visibility = Visibility.Hidden;
        }
    }

    private void RegisterButton(string name, EventCallback<ClickEvent> callback)
    {
        var button = UIDocument.rootVisualElement.Q<Button>(name);
        if (button != null)
        {
            button.RegisterCallback(callback);
            registeredCallbacks[button] = callback;
        }
    }

    private void OnDisable()
    {
        foreach (var pair in registeredCallbacks)
        {
            pair.Key.UnregisterCallback(pair.Value);
        }
        registeredCallbacks.Clear();
    }

    private void Update()
    {
        // Detecteer input mode changes
        var currentInputMode = InputDeviceDetector.CurrentInputMode;

        if (currentInputMode != lastInputMode)
        {
            OnInputModeChanged(lastInputMode, currentInputMode);
            lastInputMode = currentInputMode;
        }

        // Handle input gebaseerd op current mode
        if (currentInputMode == InputDeviceDetector.InputMode.Gamepad)
        {
            HandleGamepadInput();
        }
        else if (currentInputMode == InputDeviceDetector.InputMode.keyboardMouse)
        {
            HandleKeyboardMouseInput();
        }
    }

    private void OnInputModeChanged(InputDeviceDetector.InputMode oldMode, InputDeviceDetector.InputMode newMode)
    {
        var root = UIDocument.rootVisualElement;

        /*Debug.Log($"Input mode changed: {oldMode} -> {newMode}");*/

        if (newMode == InputDeviceDetector.InputMode.Gamepad)
        {
            // Schakel naar gamepad: zet focus op eerste button
            var visibleButtons = GetVisibleButtons();
            if (visibleButtons.Count > 0)
            {
                visibleButtons[0].Focus();
            }
        }
        else if (newMode == InputDeviceDetector.InputMode.keyboardMouse)
        {
            // Schakel naar mouse/keyboard: clear alle focus
            ClearAllFocus();
        }
    }

    private void ClearAllFocus()
    {
        var root = UIDocument.rootVisualElement;
        var focusController = root.panel.focusController;

        // Blur huidige gefocuste element
        if (focusController.focusedElement is VisualElement focused)
        {
            focused.Blur();
        }
    }

    private void EnhanceButtonFeedback()
    {
        var root = UIDocument.rootVisualElement;
        var buttons = root.Query<Button>().ToList();

        foreach (var button in buttons)
        {
            button.RegisterCallback<MouseEnterEvent>(OnButtonMouseEnter);
            button.RegisterCallback<MouseLeaveEvent>(OnButtonMouseLeave);
            button.RegisterCallback<FocusInEvent>(OnButtonFocusIn);
            button.RegisterCallback<FocusOutEvent>(OnButtonFocusOut);
        }
    }

    private void OnButtonMouseEnter(MouseEnterEvent evt)
    {
        var button = evt.target as Button;
        if (button != null)
        {
            // Verschillende behandeling per input mode
            if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.keyboardMouse)
            {
                // Mouse/Keyboard: alleen visuele hover, geen focus
                button.AddToClassList("hovered");
            }
            else if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.Gamepad)
            {
                // Gamepad: focus op button bij mouse hover (mixed input)
                button.Focus();
            }
        }
    }

    private void OnButtonMouseLeave(MouseLeaveEvent evt)
    {
        var button = evt.target as Button;
        if (button != null)
        {
            if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.keyboardMouse)
            {
                // Mouse/Keyboard: verwijder hover state
                button.RemoveFromClassList("hovered");
            }
            // Voor gamepad: laat focus intact
        }
    }

    private void OnButtonFocusIn(FocusInEvent evt)
    {
        var button = evt.target as Button;
        if (button != null)
        {
            // Alleen focus styling voor gamepad
            if (InputDeviceDetector.CurrentInputMode == InputDeviceDetector.InputMode.Gamepad)
            {
                button.AddToClassList("focused");
            }
        }
    }

    private void OnButtonFocusOut(FocusOutEvent evt)
    {
        var button = evt.target as Button;
        if (button != null)
        {
            button.RemoveFromClassList("focused");
        }
    }

    private void HandleGamepadInput()
    {
        if (Gamepad.current == null) return;

        // Negeer rechter stick
        Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
        if (Mathf.Abs(rightStick.x) > 0.1f || Mathf.Abs(rightStick.y) > 0.1f)
        {
            return;
        }

        HandleControllerNavigation();
        HandleControllerConfirmCancel();
    }

    private void HandleKeyboardMouseInput()
    {
        // Keyboard navigation (zonder permanente focus)
        if (Keyboard.current != null)
        {
            if (Keyboard.current.tabKey.wasPressedThisFrame)
            {
                // Tab voor volgende button (tijdelijk)
                NavigateKeyboard(1);
            }
            else if (Keyboard.current.tabKey.wasPressedThisFrame &&
                     (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed))
            {
                // Shift+Tab voor vorige button (tijdelijk)
                NavigateKeyboard(-1);
            }

            // Arrow keys
            if (Keyboard.current.downArrowKey.wasPressedThisFrame)
            {
                NavigateKeyboard(1);
            }
            else if (Keyboard.current.upArrowKey.wasPressedThisFrame)
            {
                NavigateKeyboard(-1);
            }

            // Enter voor activeren
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                ActivateHoveredButton();
            }
        }
    }

    private void NavigateKeyboard(int direction)
    {
        var visibleButtons = GetVisibleButtons();
        if (visibleButtons.Count == 0) return;

        // Vind button met hover class
        var hoveredButton = visibleButtons.FirstOrDefault(b => b.ClassListContains("hovered"));

        int currentIndex = hoveredButton != null ? visibleButtons.IndexOf(hoveredButton) : -1;
        int nextIndex = currentIndex + direction;

        // Clamp binnen grenzen
        nextIndex = Mathf.Clamp(nextIndex, 0, visibleButtons.Count - 1);

        // Clear alle hover states
        foreach (var btn in visibleButtons)
        {
            btn.RemoveFromClassList("hovered");
        }

        // Zet nieuwe hover state
        if (nextIndex >= 0 && nextIndex < visibleButtons.Count)
        {
            visibleButtons[nextIndex].AddToClassList("hovered");
        }
    }

    private void ActivateHoveredButton()
    {
        var root = UIDocument.rootVisualElement;
        var hoveredButton = root.Query<Button>().Where(b => b.ClassListContains("hovered")).First();

        if (hoveredButton != null)
        {
            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = hoveredButton;
                hoveredButton.SendEvent(clickEvent);
            }
        }
    }

    private void HandleControllerNavigation()
    {
        if (Gamepad.current == null) return;

        if (Time.time - lastNavigationTime < NAVIGATION_COOLDOWN)
        {
            return;
        }

        Vector2 leftStick = Gamepad.current.leftStick.ReadValue();
        float threshold = 0.6f;

        if (leftStick.y < -threshold)
        {
            if (!wasDown)
            {
                MoveFocus(1);
                wasDown = true;
                lastNavigationTime = Time.time;
            }
        }
        else
        {
            wasDown = false;
        }

        if (leftStick.y > threshold)
        {
            if (!wasUp)
            {
                MoveFocus(-1);
                wasUp = true;
                lastNavigationTime = Time.time;
            }
        }
        else
        {
            wasUp = false;
        }
    }

    private void MoveFocus(int direction)
    {
        var visibleButtons = GetVisibleButtons();

        if (visibleButtons.Count == 0) return;

        var root = UIDocument.rootVisualElement;
        var focused = root.panel.focusController.focusedElement as Button;

        if (focused == null)
        {
            visibleButtons[0]?.Focus();
            return;
        }

        var currentIndex = visibleButtons.IndexOf(focused);

        if (currentIndex >= 0)
        {
            int nextIndex = currentIndex + direction;
            nextIndex = Mathf.Clamp(nextIndex, 0, visibleButtons.Count - 1);

            if (nextIndex != currentIndex)
            {
                visibleButtons[nextIndex]?.Focus();
            }
        }
        else
        {
            visibleButtons[0]?.Focus();
        }
    }

    private List<Button> GetVisibleButtons()
    {
        var root = UIDocument.rootVisualElement;
        string[] mainMenuButtonNames = { "ContinueButton", "OptionsButton", "QuitButton" };

        bool panelsOpen = overlayPanels.Any(panel =>
            panel != null && panel.style.visibility == Visibility.Visible);

        if (!panelsOpen)
        {
            var mainButtons = new List<Button>();
            foreach (var buttonName in mainMenuButtonNames)
            {
                var button = root.Q<Button>(buttonName);
                if (button != null && button.enabledSelf && button.canGrabFocus)
                {
                    mainButtons.Add(button);
                }
            }
            return mainButtons;
        }
        else
        {
            var panelButtons = new List<Button>();

            foreach (var panel in overlayPanels)
            {
                if (panel != null && panel.style.visibility == Visibility.Visible)
                {
                    var buttonsInPanel = panel.Query<Button>().ToList();
                    panelButtons.AddRange(buttonsInPanel.Where(b => b.enabledSelf && b.canGrabFocus));
                }
            }

            panelButtons.Sort((a, b) =>
            {
                var aTop = GetButtonYPosition(a);
                var bTop = GetButtonYPosition(b);
                return aTop.CompareTo(bTop);
            });

            return panelButtons;
        }
    }

    private float GetButtonYPosition(Button button)
    {
        var top = button.resolvedStyle.top;
        if (!float.IsNaN(top))
        {
            return top;
        }
        return button.worldBound.y;
    }

    private void HandleControllerConfirmCancel()
    {
        if (Gamepad.current == null) return;

        var root = UIDocument.rootVisualElement;
        var focused = root.panel.focusController.focusedElement as Button;

        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            if (focused != null)
            {
                using (var clickEvent = ClickEvent.GetPooled())
                {
                    clickEvent.target = focused;
                    focused.SendEvent(clickEvent);
                }
            }
        }
    }
}