using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject settingsPanel;

    [Header("Sliders")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider volumeSlider;

    [Header("Slider Labels")]
    [SerializeField] private TextMeshProUGUI sensitivityLabel;
    [SerializeField] private TextMeshProUGUI volumeLabel;

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Level1";

    private void Start()
    {
        sensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity", 0.2f);
        volumeSlider.value      = PlayerPrefs.GetFloat("Volume", 1f);

        AudioListener.volume = volumeSlider.value;

        ShowMenu();
    }

    private void Update()
    {
        // Update labels every frame so they always reflect current slider value
        
        if (sensitivityLabel != null)
            sensitivityLabel.text = $"Sensitivity: {sensitivitySlider.value:F2}";

        if (volumeLabel != null)
            volumeLabel.text = $"Volume: {Mathf.RoundToInt(volumeSlider.value * 100)}%";

        // volume in real time as slider moves
        AudioListener.volume = volumeSlider.value;
    }

    // --- Buttons ----

    public void OnStartGame()
    {
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetFloat("Volume",      volumeSlider.value);
        PlayerPrefs.Save();

        SceneManager.LoadScene("Briefing"); 
    }


    public void OnSettingsOpen()
    {
        menuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnSettingsBack()
    {
        PlayerPrefs.SetFloat("Sensitivity", sensitivitySlider.value);
        PlayerPrefs.SetFloat("Volume",      volumeSlider.value);
        PlayerPrefs.Save();

        ShowMenu();
    }

    public void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ShowMenu()
    {
        menuPanel.SetActive(true);
        settingsPanel.SetActive(false);
    }
}