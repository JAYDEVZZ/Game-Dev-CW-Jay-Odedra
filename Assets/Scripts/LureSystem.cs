using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LureSystem : MonoBehaviour
{
    [Header("Lure")]
    [SerializeField] private GameObject lurePrefab;
    [SerializeField] private int        maxLures     = 3;
    [SerializeField] private float      throwForce   = 14f;
    [SerializeField] private float      throwUpAngle = 35f;

    [Header("Trajectory")]
    [SerializeField] private int       trajectoryResolution = 35;
    [SerializeField] private float     trajectoryTimeStep   = 0.05f;
    [SerializeField] private LayerMask trajectoryCollision;

    [Header("Landing Indicator")]
    [SerializeField] private int circleSegments = 40;

    

    private LineRenderer      _trajectoryLine;
    private LineRenderer      _circleRenderer;
    private int               _currentLures;
    private bool              _isAiming;
    private Vector3           _landingPoint;
    private bool              _hasLandingPoint;
    private PlayerAudioSystem _audio;

    public int CurrentLures => _currentLures;
    public int MaxLures     => maxLures;

    private void Awake()
    {
        _currentLures   = maxLures;
        _trajectoryLine = GetComponent<LineRenderer>();
        _audio          = GetComponent<PlayerAudioSystem>();

        if (_trajectoryLine != null) _trajectoryLine.enabled = false;

        BuildCircleRenderer();
    }

    private void BuildCircleRenderer()
    {
        GameObject go             = new GameObject("LureRadiusRing");
        go.transform.SetParent(transform);
        _circleRenderer               = go.AddComponent<LineRenderer>();
        _circleRenderer.loop          = true;
        _circleRenderer.useWorldSpace = true;
        _circleRenderer.startWidth    = 0.06f;
        _circleRenderer.endWidth      = 0.06f;
        _circleRenderer.positionCount = circleSegments + 1;
        _circleRenderer.startColor    = new Color(0.2f, 1f, 0.3f, 0.8f);
        _circleRenderer.endColor      = new Color(0.2f, 1f, 0.3f, 0.8f);

        if (_trajectoryLine != null && _trajectoryLine.sharedMaterial != null)
            _circleRenderer.sharedMaterial = _trajectoryLine.sharedMaterial;

        _circleRenderer.enabled = false;
    }

    private void Update()
    {
        bool holding = Keyboard.current.gKey.isPressed;

        if (holding && _currentLures > 0)
        {
            _isAiming = true;
            ShowTrajectory();
        }
        else if (_isAiming)
        {
            _isAiming = false;
            HideAll();
            if (_currentLures > 0) ThrowLure();
        }
        else
        {
            HideAll();
        }
    }

    private void ThrowLure()
    {
        if (lurePrefab == null)
        {
            Debug.LogWarning("LureSystem: lurePrefab is not assigned!");
            return;
        }

        _currentLures--;
        _audio?.PlayLureThrow();

        Vector3    spawnPos = transform.position
                            + Vector3.up * 1.5f
                            + Camera.main.transform.forward * 0.6f;

        GameObject lure = Instantiate(lurePrefab, spawnPos, Random.rotation);
        Rigidbody  rb   = lure.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity  = GetThrowVelocity();
            rb.angularVelocity = new Vector3(
                Random.Range(-6f, 6f),
                Random.Range(-6f, 6f),
                Random.Range(-3f, 3f));
        }
    }

    private Vector3 GetThrowVelocity()
    {
        Quaternion tilt = Quaternion.AngleAxis(-throwUpAngle, Camera.main.transform.right);
        return tilt * Camera.main.transform.forward * throwForce;
    }

    private void ShowTrajectory()
    {
        if (_trajectoryLine == null) return;
        _trajectoryLine.enabled = true;

        Vector3 startPos = transform.position
                         + Vector3.up * 1.5f
                         + Camera.main.transform.forward * 0.6f;
        Vector3       velocity = GetThrowVelocity();
        List<Vector3> points   = new();
        Vector3       prev     = startPos;
        _hasLandingPoint       = false;



        for (int i = 0; i < trajectoryResolution; i++)
        {
            float   t   = i * trajectoryTimeStep;
            Vector3 pos = SimulatePoint(startPos, velocity, t);


            if (Physics.Linecast(prev, pos, out RaycastHit hit, trajectoryCollision))
            {
                points.Add(hit.point);
                _landingPoint    = hit.point;
                _hasLandingPoint = true;
                break;
            }

            points.Add(pos);
            prev = pos;
        }

        _trajectoryLine.positionCount = points.Count;
        _trajectoryLine.SetPositions(points.ToArray());

        if (_hasLandingPoint)
            DrawRadiusRing(_landingPoint, GetLureRadius());
        else
            _circleRenderer.enabled = false;
    }

    private float GetLureRadius()
    {
        if (lurePrefab == null) return 8f;
        Lure l = lurePrefab.GetComponent<Lure>();
        return l != null ? l.distractionRadius : 8f;
    }

    private void DrawRadiusRing(Vector3 center, float radius)
    {
        if (_circleRenderer == null) return;
        _circleRenderer.enabled = true;

        for (int i = 0; i <= circleSegments; i++)
        {
            float   angle = (float)i / circleSegments * Mathf.PI * 2f;
            Vector3 point = center + new Vector3(
                Mathf.Cos(angle) * radius, 0.05f,
                Mathf.Sin(angle) * radius);
            _circleRenderer.SetPosition(i, point);
        }


    }



    private void HideAll()
    {
        if (_trajectoryLine != null) _trajectoryLine.enabled = false;
        if (_circleRenderer  != null) _circleRenderer.enabled = false;
    }

    private Vector3 SimulatePoint(Vector3 start, Vector3 velocity, float t) =>
        start + velocity * t + 0.5f * Physics.gravity * t * t;

    public void AddLures(int amount) =>
        _currentLures = Mathf.Min(_currentLures + amount, maxLures);
}