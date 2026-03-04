using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class AITarget : MonoBehaviour
{
    // --- Configuration Settings ---
    
    [Header("Vision & Detection")]
    public Transform Target;
    public float ViewDistance = 15f;
    [Range(0, 180)] public float ViewAngle = 60f;
    public LayerMask ObstacleMask;

    [Header("Movement Profiles")]
    public Transform[] PatrolPoints;
    public float WaitTimeAtPoint = 3f;
    [SerializeField] private float patrolSpeed = 0.3f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 1.2f;
    
    [Header("Animation Reference")]
    [SerializeField] private AnyStateAnimator anyStateAnimator;

    // --- Private State Variabls ---
    
    private NavMeshAgent _navAgent;
    private CharacterController _playerController;
    private int _waypointIndex = 0;
    private bool _isChasing = false;
    private bool _isIdle = false;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();

        // Try to find the player by tag if the reference wasnt set in the Inspector
        if (Target == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) Target = playerObj.transform;
        }

        if (Target != null)
        {
            _playerController = Target.GetComponent<CharacterController>();
        }
        
        // Initialize agent physical behavior
        ConfigureNavAgent();
    }

    private void Start()
    {
        // Register the specific animation states we want to use with the AnyState system
        if (anyStateAnimator != null)
        {
            anyStateAnimator.AddAnimation(new AnyStateAnimation("Stand"));
            anyStateAnimator.AddAnimation(new AnyStateAnimation("WalkForward"));
            anyStateAnimator.AddAnimation(new AnyStateAnimation("RunForward"));
            anyStateAnimator.AddAnimation(new AnyStateAnimation("Die"));
        }

        // Set the initial destination to start the patrol loop
        if (PatrolPoints != null && PatrolPoints.Length > 0)
        {
            _navAgent.destination = PatrolPoints[0].position;
        }
    }

    private void Update()
    {
        if (Target == null) return;

        //  check if the player is currently visible
        bool spotted = CheckLineOfSight();

        // Handle state transitions based on visibility
        if (spotted)
        {
            _isChasing = true;
            _isIdle = false;
            StopCoroutine(nameof(StayAtWaypoint));
        }

        // Execute movement based on current behavior state
        if (_isChasing)
        {
            ProcessChaseState();
        }
        else
        {
            ProcessPatrolState();
        }

        //  Sync the visuals with the NavMesh movement
        SyncAnimations();
    }

    // --- Core Logic Methods ---

    private bool CheckLineOfSight()
    {
        // calculate target height dynamically based on the player's crouch state
        float pHeight = _playerController != null ? _playerController.height : 2f;
        Vector3 targetHead = Target.position + Vector3.up * (pHeight * 0.9f);
        Vector3 eyePos = transform.position + Vector3.up * 1.7f; // Guard eye level

        Vector3 toTarget = (targetHead - eyePos);
        float dist = toTarget.magnitude;

        // Check if the player is within the maximum detection range
        if (dist < ViewDistance)
        {
            // Check if the player is within the guard's field-of-view cone
            if (Vector3.Angle(transform.forward, toTarget.normalized) < ViewAngle)
            {
                // Final check: Is there a wall (ObstacleMask) in the way?
                if (ObstacleMask != 0)
                {
                    // If the raycast DOES NOT hit a wall, the player is seen
                    return !Physics.Raycast(eyePos, toTarget.normalized, dist, ObstacleMask);
                }
                return true; // No mask set, default to seen
            }
        }
        return false;
    }

    private void ProcessChaseState()
    {
        _navAgent.isStopped = false;
        _navAgent.speed = chaseSpeed;
        _navAgent.destination = Target.position;

        // Lose interest if the player gets too far away (1.8x original range)
        if (Vector3.Distance(transform.position, Target.position) > ViewDistance * 1.8f)
        {
            _isChasing = false;
            // Return to the last known patrol point
            if (PatrolPoints.Length > 0) 
                _navAgent.destination = PatrolPoints[_waypointIndex].position;
        }
    }

    private void ProcessPatrolState()
    {
        if (PatrolPoints == null || PatrolPoints.Length == 0 || _isIdle) return;

        _navAgent.speed = patrolSpeed;

        // Check if we have arrived at the current patrol destination
        if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
        {
            StartCoroutine(StayAtWaypoint());
        }
    }

    private IEnumerator StayAtWaypoint()
    {
        _isIdle = true;
        _navAgent.isStopped = true; 
        _navAgent.velocity = Vector3.zero; // Kill momentum to stop jittering
        
        yield return new WaitForSeconds(WaitTimeAtPoint);
        
        // Advance to the next waypoint in the array
        if (PatrolPoints.Length > 0)
        {
            _waypointIndex = (_waypointIndex + 1) % PatrolPoints.Length;
            _navAgent.destination = PatrolPoints[_waypointIndex].position;
        }
        
        _navAgent.isStopped = false;
        _isIdle = false;
    }

    private void SyncAnimations()
    {
        if (anyStateAnimator == null) return;

        // Use velocity magnitude to decide which move animation to play
        if (_navAgent.velocity.magnitude > 0.15f && !_navAgent.isStopped)
        {
            anyStateAnimator.TryPlayAnimaiton(_isChasing ? "RunForward" : "WalkForward");
        }
        else
        {
            anyStateAnimator.TryPlayAnimaiton("Stand");
        }
    }

    private void ConfigureNavAgent()
    {
        _navAgent.speed = patrolSpeed;
        _navAgent.stoppingDistance = stoppingDistance;
        _navAgent.acceleration = 12f; 
        _navAgent.angularSpeed = 600f; // High rotation speed prevents circling at points
    }

    // --- Visualization ---

    private void OnDrawGizmos()
    {
        // Draw the patrol route in Cyan
        if (PatrolPoints != null && PatrolPoints.Length > 0)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < PatrolPoints.Length; i++)
            {
                if (PatrolPoints[i] == null) continue;
                Gizmos.DrawSphere(PatrolPoints[i].position, 0.25f);
                
                int next = (i + 1) % PatrolPoints.Length;
                if (PatrolPoints[next] != null)
                    Gizmos.DrawLine(PatrolPoints[i].position, PatrolPoints[next].position);
            }
        }

        // Visualize FOV limits
        Vector3 eyes = transform.position + Vector3.up * 1.7f;
        Gizmos.color = _isChasing ? Color.red : Color.yellow;
        
        Quaternion leftRot = Quaternion.AngleAxis(-ViewAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(ViewAngle, Vector3.up);
        
        Gizmos.DrawLine(eyes, eyes + (leftRot * transform.forward) * ViewDistance);
        Gizmos.DrawLine(eyes, eyes + (rightRot * transform.forward) * ViewDistance);

        // Draw the dynamic detection raycast while running
        if (Application.isPlaying && Target != null)
        {
            float pHeight = _playerController != null ? _playerController.height : 2f;
            Vector3 head = Target.position + Vector3.up * (pHeight * 0.9f);

            Gizmos.color = CheckLineOfSight() ? Color.red : Color.green;
            Gizmos.DrawLine(eyes, head);
        }
    }
}