using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
public class MainMenu : MonoBehaviour
{
    private UIDocument UIDocument;
    private VisualElement[] overlayPanels;
    private Dictionary<Button, EventCallback<ClickEvent>> registeredCallbacks = new();

    private void Awake()
    {
        UIDocument = GetComponent<UIDocument>();
        var root = UIDocument.rootVisualElement;

        overlayPanels = new VisualElement[]
        {
            root.Q<VisualElement>("Options"),
            root.Q<VisualElement>("Credits"),
        };

        HideAllPanels();
        SetupButtons();
    }

    private void SetupButtons()
    {
        RegisterButton("ContinueButton", _ => StartFreshGame());
        RegisterButton("QuitButton", _ => Application.Quit());
        RegisterButton("OptionsButton", _ => ShowPanel("Options"));
        RegisterButton("BackOptionsButton", _ => ShowPanel("Options"));
        RegisterButton("creditsButton", _ => ShowPanel("Credits"));
        RegisterButton("BackButton", _ => HideAllPanels());





        /*RegisterButton("ControlsButton", _ => ShowPanel("Controls"));
        
        RegisterButton("ControlsReturnButton", _ => HideAllPanels());*/
    }

    private void StartFreshGame()
    {
        SceneManager.LoadScene("Test sceme");
    }

    private void ShowPanel(string panelName)
    {
        HideAllPanels();
        var panel = UIDocument.rootVisualElement.Q<VisualElement>(panelName);
        if (panel != null)
            panel.style.visibility = Visibility.Visible;
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
}
