using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class BriefingScreen : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI briefingText;
    [SerializeField] private GameObject continuePrompt;
    [SerializeField] private GameObject beginButton;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "Level1";
    [SerializeField] private float charDelay = 0.03f;
    [SerializeField] private float lineDelay = 0.6f;

    [Header("Briefing Content")]
    [SerializeField] private string[] lines = new string[]
    {
        "MISSION BRIEFING",
        "",
        "Location: Enemy Military Facility",
        "",
        "OBJECTIVES:",
        "",
        "01  Locate and secure 3 pieces of intelligence",
        "    scattered throughout the facility.",
        "",
        "02  Avoid detection.",
        "    If spotted — neutralise the threat.",
        "",
        "03  Reach the extraction point",
        "    once all intel is secured.",
        "",
        "You are operating alone.",
        "There will be no backup.",
        "",
        "Do not fail."
    };

    private bool _complete = false;
    private bool _skip = false;

    private void Start()
    {
        if (continuePrompt != null) continuePrompt.SetActive(false);
        if (beginButton != null) beginButton.SetActive(false);
        if (briefingText != null) briefingText.text = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(RunBriefing());
    }

    private void Update()
    {
        bool anyKeyPressed = Keyboard.current != null &&
                             Keyboard.current.anyKey.wasPressedThisFrame;

        if (!_complete && anyKeyPressed)
            _skip = true;

        if (_complete && anyKeyPressed)
            LoadGame();
    }

    private IEnumerator RunBriefing()
    {
        string displayed = "";

        foreach (string line in lines)
        {
            if (_skip) break;

            foreach (char c in line)
            {
                if (_skip) break;
                displayed += c;
                if (briefingText != null) briefingText.text = displayed;
                yield return new WaitForSeconds(charDelay);
            }

            displayed += "\n";
            if (briefingText != null) briefingText.text = displayed;

            if (!_skip)
                yield return new WaitForSeconds(lineDelay);
        }

        if (_skip) // show full text if player skipped

        {
            displayed = string.Join("\n", lines);
            if (briefingText != null) briefingText.text = displayed;
        }

        _complete = true;
        if (continuePrompt != null) continuePrompt.SetActive(true);
        if (beginButton != null) beginButton.SetActive(true);
    }


    // wired to the Begin Mission button
    public void LoadGame()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(gameSceneName);
    }
}