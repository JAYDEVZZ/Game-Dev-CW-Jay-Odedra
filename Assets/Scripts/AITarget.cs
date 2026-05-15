using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AITarget : MonoBehaviour
{
    public enum AIState { Patrolling, Suspicious, Combat }
    private enum CombatBehavior { StandAndShoot, Advance }

    [Header("Vision & Detection")]
    public Transform Target;
    public float ViewDistance = 15f;
    [Range(0, 180)] public float ViewAngle = 60f;
    public LayerMask ObstacleMask;

    [Header("Detection Meter")]
    [SerializeField] private float detectionFillCloseRate = 1.2f;
    [SerializeField] private float detectionFillFarRate = 0.15f;
    [SerializeField] private float detectionDrainRate = 0.3f;
    [SerializeField] private float suspiciousThreshold = 0.4f;

    [Header("Movement Profiles")]
    public Transform[] PatrolPoints;
    public float WaitTimeAtPoint = 3f;
    [SerializeField] private float patrolSpeed = 0.3f;
    [SerializeField] private float chaseSpeed = 3.5f;
    [SerializeField] private float stoppingDistance = 1.2f;

    [Header("Combat")]
    [SerializeField] private Transform gunBarrel;
    [SerializeField] private float damage = 10f;
    [SerializeField] private float fireRate = 1f;
    [SerializeField] private float shootingRange = 12f;
    [SerializeField] private float advanceRange = 5f;
    [SerializeField] private float behaviorSwitchInterval = 3f;
    [SerializeField] private float spread = 0.05f;
    [SerializeField] private GameObject muzzleFlashPrefab;

    [Header("Search Behaviour")]
    [SerializeField] private float lastKnownHoldTime = 2f;
    [SerializeField] private float searchRadius = 6f;
    [SerializeField] private int searchPointCount = 4;

    [Header("Animation")]
    [SerializeField] private AnyStateAnimator anyStateAnimator;

    [Header("Performance")]
    [SerializeField] private float updateInterval = 0.1f; // 10 updates per second
    private float _updateTimer = 0f;

    public AIState CurrentState { get; private set; } = AIState.Patrolling;
    public float DetectionMeter { get; private set; } = 0f;
    public bool IsInvestigatingLure { get; private set; } = false;

    private NavMeshAgent _navAgent;
    private CharacterController _playerController;
    private Health _health;
    private Health _playerHealth;
    private AIAudioSystem _audio;

    private int _waypointIndex = 0;
    private bool _isIdle = false;
    private bool _canSeePlayer = false;
    private bool _prevCanSee = false;

    private CombatBehavior _combatBehavior = CombatBehavior.Advance;
    private float _nextFireTime = 0f;
    private float _nextBehaviorSwitch = 0f;

    private Vector3 _lastKnownPos;
    private bool _hasLastKnownPos = false;

    private bool _isSearching = false;
    private bool _searchWaiting = false;
    private float _holdTimer = 0f;
    private List<Vector3> _searchWaypoints = new();
    private int _searchIndex = 0;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _health = GetComponent<Health>();
        _audio = GetComponent<AIAudioSystem>();

        if (Target == null)
        {
            var playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) Target = playerObj.transform;
        }

        if (Target != null)
        {
            _playerController = Target.GetComponent<CharacterController>();
            _playerHealth = Target.GetComponent<Health>();
        }

        if (_health != null)
            _health.onDamageTaken.AddListener(OnDamageTaken);

        ConfigureNavAgent();
    }

    private void Start()
    {
        if (anyStateAnimator != null)
        {
            anyStateAnimator.AddAnimation(new AnyStateAnimation("Stand"));
            anyStateAnimator.AddAnimation(new AnyStateAnimation("WalkForward"));
            anyStateAnimator.AddAnimation(new AnyStateAnimation("RunForward"));
            anyStateAnimator.AddAnimation(new AnyStateAnimation("Die"));
        }

        if (PatrolPoints != null && PatrolPoints.Length > 0)
            _navAgent.destination = PatrolPoints[0].position;

        // offset so guards don't all update the same frame
        _updateTimer = Random.Range(0f, updateInterval);
    }

    private void Update()
    {
        _updateTimer -= Time.deltaTime;
        if (_updateTimer > 0f) return;
        _updateTimer = updateInterval;

        if (_health != null && _health.IsDead)
        {
            _navAgent.isStopped = true;
            _navAgent.velocity = Vector3.zero;
            return;
        }

        if (Target == null) return;

        _prevCanSee = _canSeePlayer;
        _canSeePlayer = CheckLineOfSight();

        if (_prevCanSee && !_canSeePlayer &&
           (CurrentState == AIState.Combat || CurrentState == AIState.Suspicious))
        {
            _lastKnownPos = Target.position;
            _hasLastKnownPos = true;
        }

        if (_canSeePlayer) _lastKnownPos = Target.position;

        UpdateDetectionMeter();
        UpdateState();

        switch (CurrentState)
        {
            case AIState.Patrolling: ProcessPatrolState(); break;
            case AIState.Suspicious: ProcessSuspiciousState(); break;
            case AIState.Combat: ProcessCombatState(); break;
        }

        SyncAnimations();
    }

    private void OnDamageTaken(float _)
    {
        DetectionMeter = 1f;
        _hasLastKnownPos = true;
        _lastKnownPos = Target != null ? Target.position : transform.position;

        if (CurrentState != AIState.Combat)
            SetState(AIState.Combat);
    }

    private void UpdateDetectionMeter()
    {
        if (_canSeePlayer)
        {
            float dist = Vector3.Distance(transform.position, Target.position);
            float proximity = 1f - Mathf.Clamp01(dist / ViewDistance);
            float fillRate = Mathf.Lerp(detectionFillFarRate, detectionFillCloseRate, proximity); // fill faster when player is close
            DetectionMeter = Mathf.Min(1f, DetectionMeter + fillRate * updateInterval);
        }
        else
        {
            if (IsInvestigatingLure) return;
            float drainMult = CurrentState == AIState.Combat ? 0.15f : 1f;
            DetectionMeter = Mathf.Max(0f, DetectionMeter - detectionDrainRate * drainMult * updateInterval);
        }
    }

    private void UpdateState()
    {
        switch (CurrentState)
        {
            case AIState.Patrolling:
                if (DetectionMeter >= suspiciousThreshold) SetState(AIState.Suspicious);
                break;

            case AIState.Suspicious:
                if (DetectionMeter >= 1f) SetState(AIState.Combat);
                if (DetectionMeter <= 0f && !IsInvestigatingLure) SetState(AIState.Patrolling);
                break;

            case AIState.Combat:
                if (DetectionMeter <= 0.1f)
                {
                    ResetSearchState();
                    SetState(AIState.Patrolling);
                }
                break;
        }
    }



    private void SetState(AIState newState)
    {
        if (newState == AIState.Suspicious && CurrentState == AIState.Patrolling)
            _audio?.PlaySuspicious();
        else if (newState == AIState.Combat && CurrentState != AIState.Combat)
            _audio?.PlayCombat();

        CurrentState = newState;

        if (newState == AIState.Patrolling)
        {
            _isIdle = false;
            _hasLastKnownPos = false;
            if (PatrolPoints.Length > 0)
                _navAgent.destination = PatrolPoints[_waypointIndex].position;
        }

        if (newState == AIState.Combat)
        {
            PickCombatBehavior();
            _nextBehaviorSwitch = Time.time + behaviorSwitchInterval;
        }
    }

    private void ProcessPatrolState()
    {
        if (PatrolPoints == null || PatrolPoints.Length == 0 || _isIdle) return;

        _navAgent.isStopped = false;
        _navAgent.speed = patrolSpeed;

        if (!_navAgent.pathPending && _navAgent.remainingDistance <= _navAgent.stoppingDistance)
            StartCoroutine(StayAtWaypoint());
    }

    private void ProcessSuspiciousState()
    {
        _navAgent.isStopped = false;
        _navAgent.speed = patrolSpeed * 1.5f;
        if (_canSeePlayer) _navAgent.destination = Target.position;
    }

    private void ProcessCombatState()
    {
        FaceTarget(_canSeePlayer);

        if (Time.time >= _nextBehaviorSwitch)
        {
            PickCombatBehavior();
            _nextBehaviorSwitch = Time.time + behaviorSwitchInterval;
        }

        if (!_canSeePlayer)
        {
            _navAgent.isStopped = false;
            _navAgent.speed = chaseSpeed;

            if (!_isSearching)
            {


                if (_hasLastKnownPos)
                    _navAgent.destination = _lastKnownPos;

                bool arrived = !_hasLastKnownPos ||
                    Vector3.Distance(transform.position, _lastKnownPos) <= stoppingDistance + 0.5f;

                if (arrived)
                {
                    _navAgent.isStopped = true;
                    _navAgent.velocity = Vector3.zero;
                    _holdTimer += updateInterval;

                    if (_holdTimer >= lastKnownHoldTime)
                    {
                        _isSearching = true;
                        _searchWaiting = false;
                        GenerateSearchWaypoints();

                        if (_searchWaypoints.Count > 0)
                        {
                            _searchIndex = 0;
                            _navAgent.isStopped = false;
                            _navAgent.destination = _searchWaypoints[0];
                        }
                    }
                }
            }
            else
            {
                if (!_searchWaiting && _searchWaypoints.Count > 0)
                {
                    _navAgent.isStopped = false;
                    _navAgent.speed = patrolSpeed * 1.8f;

                    if (!_navAgent.pathPending &&
                        _navAgent.remainingDistance <= _navAgent.stoppingDistance + 0.3f)
                        StartCoroutine(SearchPointPause());
                }
            }
            return;
        }

        float dist = Vector3.Distance(transform.position, Target.position);

        switch (_combatBehavior)
        {
            case CombatBehavior.StandAndShoot:
                _navAgent.isStopped = true;
                _navAgent.velocity = Vector3.zero;
                TryShoot();
                break;

            case CombatBehavior.Advance:
                if (dist > advanceRange)
                {
                    _navAgent.isStopped = false;
                    _navAgent.speed = chaseSpeed;
                    _navAgent.destination = Target.position;
                }
                else
                {

                    _navAgent.isStopped = true;
                    _navAgent.velocity = Vector3.zero;
                }
                TryShoot();
                break;
        }
    }

    private IEnumerator SearchPointPause()
    {
        _searchWaiting = true;
        _navAgent.isStopped = true;
        _navAgent.velocity = Vector3.zero;

        yield return new WaitForSeconds(1.5f);

        if (CurrentState == AIState.Combat && !_canSeePlayer)
        {
            _searchIndex++;
            if (_searchIndex < _searchWaypoints.Count)
            {
                _navAgent.destination = _searchWaypoints[_searchIndex];
                _navAgent.isStopped = false;
            }
        }

        _searchWaiting = false;
    }

    private void GenerateSearchWaypoints()
    {
        _searchWaypoints.Clear();
        _searchIndex = 0;

        if (NavMesh.SamplePosition(_lastKnownPos, out NavMeshHit first, 2f, NavMesh.AllAreas))
            _searchWaypoints.Add(first.position);

        int attempts = 0;
        while (_searchWaypoints.Count < searchPointCount && attempts < 20)
        {
            attempts++;
            Vector2 rand = Random.insideUnitCircle * searchRadius;
            Vector3 candidate = _lastKnownPos + new Vector3(rand.x, 0f, rand.y);
            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
                _searchWaypoints.Add(hit.position);
        }
    }

    private void ResetSearchState()
    {


        _isSearching = false;
        _searchWaiting = false;
        _holdTimer = 0f;
        _searchWaypoints.Clear();
        _searchIndex = 0;
    }

    public void DistractToPoint(Vector3 point, float alertAmount, float duration)
    {
        if (_health != null && _health.IsDead) return;
        if (CurrentState == AIState.Combat) return;

        DetectionMeter = Mathf.Max(DetectionMeter, alertAmount);
        _lastKnownPos = point;
        _hasLastKnownPos = true;

        StopAllCoroutines();
        StartCoroutine(InvestigatePoint(point, alertAmount, duration));
    }

    private IEnumerator InvestigatePoint(Vector3 point, float alertAmount, float duration)
    {
        IsInvestigatingLure = true;
        SetState(AIState.Suspicious);

        _navAgent.isStopped = false;
        _navAgent.speed = patrolSpeed * 1.8f;
        _navAgent.destination = point;

        while (Vector3.Distance(transform.position, point) > stoppingDistance + 0.5f)
        {
            if (CurrentState == AIState.Combat) { IsInvestigatingLure = false; yield break; }
            yield return null;
        }

        _navAgent.isStopped = true;
        _navAgent.velocity = Vector3.zero;

        float timer = 0f;
        while (timer < duration)
        {
            if (CurrentState == AIState.Combat) { IsInvestigatingLure = false; yield break; }
            timer += Time.deltaTime;
            yield return null;
        }

        IsInvestigatingLure = false;

        if (CurrentState != AIState.Combat)
        {
            DetectionMeter = Mathf.Max(0f, DetectionMeter - alertAmount);
            SetState(AIState.Patrolling);
        }
    }

    private void PickCombatBehavior()
    {

        _combatBehavior = Random.value > 0.65f
            ? CombatBehavior.StandAndShoot
            : CombatBehavior.Advance;
    }

    private void FaceTarget(bool facePlayer)
    {
        Vector3 dir = facePlayer
            ? Target.position - transform.position
            : _navAgent.velocity.normalized;

        dir.y = 0f;
        if (dir.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            Time.deltaTime * 10f);
    }

    private void TryShoot()
    {
        if (!_canSeePlayer) return;
        if (Time.time < _nextFireTime) return;
        if (_playerHealth != null && _playerHealth.IsDead) return;
        if (Vector3.Distance(transform.position, Target.position) > shootingRange) return;

        _nextFireTime = Time.time + (1f / fireRate);
        Shoot();
    }

    private void Shoot()
    {
        if (muzzleFlashPrefab != null && gunBarrel != null)
        {
            GameObject flash = Instantiate(muzzleFlashPrefab, gunBarrel.position, gunBarrel.rotation);
            Destroy(flash, 0.1f);
        }

        _audio?.PlayGunshot();

        Vector3 shotOrigin = gunBarrel != null
            ? gunBarrel.position
            : transform.position + Vector3.up * 1.5f;

        SoundManager.EmitSound(shotOrigin, 30f, 1f);


        float pHeight = _playerController != null ? _playerController.height : 2f;
        Vector3 aimPoint = Target.position + Vector3.up * (pHeight * 0.6f);
        Vector3 origin = gunBarrel != null
            ? gunBarrel.position
            : transform.position + Vector3.up * 1.5f;

        Vector3 direction = (aimPoint - origin).normalized;
        direction += new Vector3(
            Random.Range(-spread, spread),
            Random.Range(-spread * 0.5f, spread * 0.5f),
            Random.Range(-spread, spread));
        direction.Normalize();

        if (Physics.Raycast(origin, direction, out RaycastHit hit, shootingRange, ~ObstacleMask))
        {
            Health h = hit.collider.GetComponentInParent<Health>();
            if (h != null) h.TakeDamage(damage);
        }
    }

    public void AlertFromSound(Vector3 soundPosition, float amount)
    {
        if (_health != null && _health.IsDead) return;

        DetectionMeter = Mathf.Min(1f, DetectionMeter + amount);
        _lastKnownPos = soundPosition;
        _hasLastKnownPos = true;

        if (DetectionMeter >= 1f && CurrentState != AIState.Combat)
            SetState(AIState.Combat);
        else if (DetectionMeter >= suspiciousThreshold && CurrentState == AIState.Patrolling)
            SetState(AIState.Suspicious);
    }

    private bool CheckLineOfSight()
    {
        float pHeight = _playerController != null ? _playerController.height : 2f;
        Vector3 targetHead = Target.position + Vector3.up * (pHeight * 0.9f);
        Vector3 eyePos = transform.position + Vector3.up * 1.7f;
        Vector3 toTarget = targetHead - eyePos;
        float dist = toTarget.magnitude;

        if (dist >= ViewDistance) return false;
        if (Vector3.Angle(transform.forward, toTarget.normalized) >= ViewAngle) return false;
        if (ObstacleMask != 0 && Physics.Raycast(eyePos, toTarget.normalized, dist, ObstacleMask))
            return false;


        return true;
    }

    private IEnumerator StayAtWaypoint()
    {
        _isIdle = true;
        _navAgent.isStopped = true;
        _navAgent.velocity = Vector3.zero;

        yield return new WaitForSeconds(WaitTimeAtPoint);

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

        if (_health != null && _health.IsDead)
        {
            anyStateAnimator.TryPlayAnimaiton("Die");
            return;
        }

        bool moving = !_navAgent.isStopped && _navAgent.velocity.magnitude > 0.15f;

        if (moving)
            anyStateAnimator.TryPlayAnimaiton(
                CurrentState == AIState.Combat ? "RunForward" : "WalkForward");
        else
            anyStateAnimator.TryPlayAnimaiton("Stand");
    }

    private void ConfigureNavAgent()
    {
        _navAgent.speed = patrolSpeed;
        _navAgent.stoppingDistance = stoppingDistance;
        _navAgent.acceleration = 12f;
        _navAgent.angularSpeed = 600f;
    }



    private void OnDrawGizmos()
    {
        if (PatrolPoints != null)
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

        if (Application.isPlaying && _isSearching)
        {
            Gizmos.color = Color.magenta;
            foreach (Vector3 p in _searchWaypoints)
                Gizmos.DrawSphere(p, 0.3f);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_lastKnownPos, 0.5f);
        }

        Vector3 eyes = transform.position + Vector3.up * 1.7f;
        Gizmos.color = CurrentState == AIState.Combat     ? Color.red
                     : CurrentState == AIState.Suspicious ? Color.yellow
                     : Color.green;


        Quaternion leftRot  = Quaternion.AngleAxis(-ViewAngle, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis( ViewAngle, Vector3.up);
        Gizmos.DrawLine(eyes, eyes + leftRot  * transform.forward * ViewDistance);
        Gizmos.DrawLine(eyes, eyes + rightRot * transform.forward * ViewDistance);

        if (Application.isPlaying && Target != null)
        {
            float   pHeight = _playerController != null ? _playerController.height : 2f;
            Vector3 head    = Target.position + Vector3.up * (pHeight * 0.9f);
            Gizmos.color    = _canSeePlayer ? Color.red : Color.green;
            Gizmos.DrawLine(eyes, head);
        }
    }
}
