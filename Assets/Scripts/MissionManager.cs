using UnityEngine;
using UnityEngine.SceneManagement;

public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }


    [Header("Mission Settings")]
    [SerializeField] private int    totalIntel    = 3;
    [SerializeField] private float  winScreenDelay = 2f; 
    [SerializeField] private string nextScene     = "MainMenu"; 

    [Header("Win Screen")]
    [SerializeField] private GameObject winPanel; 

    private int  _collectedIntel = 0;
    private bool _missionComplete = false;

    public int  CollectedIntel     => _collectedIntel;
    public int  TotalIntel         => totalIntel;
    public bool AllIntelCollected  => _collectedIntel >= totalIntel;


    public event System.Action OnIntelCollected;
    public event System.Action OnMissionComplete;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (winPanel != null) winPanel.SetActive(false);
    }

    public void CollectIntel()
    {
        if (_missionComplete) return;
        if (_collectedIntel >= totalIntel) return;

        _collectedIntel++;
        OnIntelCollected?.Invoke();

        Debug.Log($"Intel collected: {_collectedIntel} / {totalIntel}");
    }

    public void CompleteMission()
    {
        if (_missionComplete) return;
        _missionComplete = true;

        OnMissionComplete?.Invoke();

        if (winPanel != null) winPanel.SetActive(true);

        // -unlocks curssor for win screen

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        Invoke(nameof(LoadNextScene), winScreenDelay);
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(nextScene);
    }
}