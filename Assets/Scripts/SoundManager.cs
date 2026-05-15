using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Obstacle Muffling")]
    [SerializeField] private LayerMask obstacleMask;
    [SerializeField] private float wallMuffleMultiplier = 0.15f; // sound through walls

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static void EmitSound(Vector3 position, float radius, float detectionAmount)
    {
        if (Instance == null) return;

        Collider[] hits = Physics.OverlapSphere(position, radius);

        foreach (Collider col in hits)
        {
            AITarget ai = col.GetComponent<AITarget>()
                       ?? col.GetComponentInParent<AITarget>();
            if (ai == null) continue;

            float dist          = Vector3.Distance(position, ai.transform.position);
            float distanceFactor = 1f - Mathf.Clamp01(dist / radius); 
            float amount        = detectionAmount * distanceFactor;

            // Muffle sound between walls and environment
            Vector3 toAI = ai.transform.position + Vector3.up * 1.5f - position;
            if (Instance.obstacleMask != 0 &&
                Physics.Raycast(position, toAI.normalized, toAI.magnitude, Instance.obstacleMask))
            {
                amount *= Instance.wallMuffleMultiplier;
            }
            if (amount > 0.001f)
                ai.AlertFromSound(position, amount);
        }
    }
}