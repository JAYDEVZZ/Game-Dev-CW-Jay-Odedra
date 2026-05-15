using UnityEngine;

public class IntelPickup : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;

    [Header("Visuals")]
    [SerializeField] private float bobHeight   = 0.25f;
    [SerializeField] private float bobSpeed    = 2f;
    [SerializeField] private float rotateSpeed = 80f;

    private Vector3 _startPos;

    private void Start() => _startPos = transform.position;

    private void Update()
    {
        transform.position = _startPos + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // ---Search for player component same way other pickups search for gunsystem---


        Player player = other.GetComponent<Player>()
                     ?? other.GetComponentInParent<Player>()
                     ?? other.GetComponentInChildren<Player>();

        if (player == null) return;
        if (MissionManager.Instance == null) return;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);

        MissionManager.Instance.CollectIntel();
        Destroy(gameObject);
    }
    

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}