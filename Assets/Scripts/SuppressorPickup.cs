using UnityEngine;

public class SuppressorPickup : MonoBehaviour
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
        GunSystem gun = other.GetComponent<GunSystem>()
                     ?? other.GetComponentInParent<GunSystem>()
                     ?? other.GetComponentInChildren<GunSystem>();

        if (gun == null) return;
        if (gun.SuppressorCharges >= gun.MaxSuppressorCharges) return;

        gun.RefillSuppressor();

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, 0.8f);
        Destroy(gameObject);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0f, 0.8f, 1f);
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.4f);
        Gizmos.DrawWireSphere(transform.position, 0.8f);
    }
}