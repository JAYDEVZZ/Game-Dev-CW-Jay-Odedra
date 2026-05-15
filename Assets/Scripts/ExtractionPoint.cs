using UnityEngine;

public class ExtractionPoint : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject lockedVisual;
    [SerializeField] private GameObject unlockedVisual;

    [Header("Extraction")]
    [SerializeField] private float holdTime = 2f;

    private bool  _isActive     = false;
    private float _holdTimer    = 0f;
    private bool  _playerInside = false;

    private void Start()
    {
        if (MissionManager.Instance != null)
            MissionManager.Instance.OnIntelCollected += CheckActivation;

        UpdateVisuals();
    }

    private void Update()
    {
        if (!_isActive || !_playerInside) return;

        _holdTimer += Time.deltaTime;

        if (_holdTimer >= holdTime)
            MissionManager.Instance?.CompleteMission();
    }



    private void OnTriggerEnter(Collider other)
    {
        Player player = other.GetComponent<Player>()
                     ?? other.GetComponentInParent<Player>()
                     ?? other.GetComponentInChildren<Player>();

        if (player == null) return;
        _playerInside = true;
        _holdTimer    = 0f;
    }

    private void OnTriggerExit(Collider other)
    {
        Player player = other.GetComponent<Player>()
                     ?? other.GetComponentInParent<Player>()
                     ?? other.GetComponentInChildren<Player>();

        if (player == null) return;
        _playerInside = false;
        _holdTimer    = 0f;
    }

    

    private void CheckActivation()
    {
        if (MissionManager.Instance == null) return;
        if (!MissionManager.Instance.AllIntelCollected) return;

        _isActive = true;
        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (lockedVisual   != null) lockedVisual.SetActive(!_isActive);
        if (unlockedVisual != null) unlockedVisual.SetActive(_isActive);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = _isActive ? Color.green : Color.red;
        Gizmos.DrawWireCube(transform.position, new Vector3(5f, 0.1f, 5f));
    }
}