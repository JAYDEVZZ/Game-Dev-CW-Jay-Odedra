using UnityEngine;

public class Lure : MonoBehaviour
{
    [Header("Distraction")]
    public  float      distractionRadius   = 8f;
    [SerializeField] private float alertFillAmount     = 0.5f;
    [SerializeField] private float investigateDuration = 6f;
    [SerializeField] private float selfDestructDelay   = 10f;

    [Header("Audio")]
    [SerializeField] private AudioClip landClip;

    private bool _hasLanded = false;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Collider lureCol = GetComponent<Collider>();
            foreach (Collider pc in player.GetComponentsInChildren<Collider>())
                Physics.IgnoreCollision(lureCol, pc);
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (_hasLanded) return;
        _hasLanded = true;




        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {

            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = true;
        }


        if (landClip != null)
            AudioSource.PlayClipAtPoint(landClip, transform.position, 0.6f);

        Collider[] hits = Physics.OverlapSphere(transform.position, distractionRadius);
        foreach (Collider col in hits)
        {
            AITarget ai = col.GetComponent<AITarget>()
                       ?? col.GetComponentInParent<AITarget>();
            if (ai != null)
                ai.DistractToPoint(transform.position, alertFillAmount, investigateDuration);
        }

        Destroy(gameObject, selfDestructDelay);
    }

    

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, distractionRadius);
    }
}