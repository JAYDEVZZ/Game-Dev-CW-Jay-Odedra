using UnityEngine;

public class LurePickup : MonoBehaviour
{
    [Header("Lure")]
    [SerializeField] private int lureAmount = 1;

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
        LureSystem lures = other.GetComponent<LureSystem>()
                        ?? other.GetComponentInParent<LureSystem>()
                        ?? other.GetComponentInChildren<LureSystem>();

        if (lures == null) return;
        if (lures.CurrentLures >= lures.MaxLures) return;



        lures.AddLures(lureAmount);

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);

        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}