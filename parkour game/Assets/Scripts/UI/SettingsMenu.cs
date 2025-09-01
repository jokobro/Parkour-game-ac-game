
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;
public class SettingsMenu : MonoBehaviour
{
    public static SettingsMenu Instance;
    [SerializeField] private AudioMixer audioMixer;
    private UIDocument UIDocument;
    private Slider musicSlider;
    private Slider sfxSlider;

    private DropdownField resolutionField;


    private void Awake()
    {
        Instance = this;

        UIDocument = GetComponent<UIDocument>();
        var root = UIDocument.rootVisualElement;
        musicSlider = root.Q<Slider>("MusicSlider");
        sfxSlider = root.Q<Slider>("SFXSlider");
        
        resolutionField = root.Q<DropdownField>("ResolutionDropDown");
    }

    private void Start()
    {
        loadVolume();

        musicSlider.RegisterValueChangedCallback(evt => UpdateMusicVolume(evt.newValue));
        sfxSlider.RegisterValueChangedCallback(evt => UpdateSFXVolume(evt.newValue));


        var availableResolutions = Screen.resolutions
            .Select(r => new Vector2Int(r.width, r.height))
            .Distinct()
            .OrderByDescending(r => r.x * r.y) // sorteer van groot naar klein
            .ToList();

        var wanted = new List<Vector2Int>()
        {
            new Vector2Int(3840, 2160),
            new Vector2Int(2560, 1440),
            new Vector2Int(1920, 1080),
            new Vector2Int(1600, 900),
            new Vector2Int(1280, 720)
        };

        var filterd = wanted.Where(r => availableResolutions.Contains(r)).ToList();

        var options = filterd.Select(r => $"{r.x}x{r.y}").ToList();


        resolutionField.choices = options;

        string current = $"{Screen.currentResolution.width}x{Screen.currentResolution.height}";
        resolutionField.value = options.Contains(current) ? current : options.FirstOrDefault();


        resolutionField.RegisterValueChangedCallback(evt =>
        {
            var parts = evt.newValue.Split('x');
            int width = int.Parse(parts[0]);
            int height = int.Parse(parts[1]);

            Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
            Debug.Log($"Resolutie veranderd naar {width}x{height}");
        }); 
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
    }

    float LinearToDecibel(float value)
    {
        return Mathf.Log10(Mathf.Clamp(value, 0.0001f, 1f)) * 20f;
    }
}
