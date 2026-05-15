using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Distance")]
    public float distance = 5f;
    public float minDistance = 1.5f;
    public float maxDistance = 10f;
    [Header("Height")]
    public float height = 2f;
    [Header("Shoulder Offset")]
    public float shoulderOffsetX = 0.8f;  
    public float aimOffsetX = 1.2f;       
    [Header("Aim Settings")]
    public float aimDistance = 2.5f;
    public float aimHeight = 1.6f;
    public float aimSmooth = 8f;
    
    public float CurrentPitchAngle => _currentY;

    [Header("Mouse Sensitivity")]
    public float sensitivityX = 0.2f;
    public float sensitivityY = 0.2f;
    [Header("Vertical Angle Limits")]
    public float minYAngle = -20f;
    public float maxYAngle = 60f;
    [Header("Collision")]
    public LayerMask collisionLayers;
    public float collisionRadius = 0.2f;
    private float _currentX = 0f;
    private float _currentY = 20f;
    private float _currentDistance;
    private float _currentHeight;
    private float _currentOffsetX;
    private bool _wasAiming = false;

    public bool IsAiming { get; private set; }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;

        _currentDistance = distance;
        _currentHeight   = height;
        _currentOffsetX  = shoulderOffsetX;
        float saved = PlayerPrefs.GetFloat("Sensitivity", 0.2f);
        sensitivityX = saved;
        sensitivityY = saved;
    }

    void LateUpdate()
    {
        if (target == null) return;

        IsAiming = Mouse.current.rightButton.isPressed;
        HandleInput();
        MoveCamera();
        _wasAiming = IsAiming;
    }

    void HandleInput()
    {
        if (IsAiming && !_wasAiming)
            _currentX = target.eulerAngles.y;
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        _currentX += mouseDelta.x * sensitivityX;
        _currentY -= mouseDelta.y * sensitivityY;
        _currentY  = Mathf.Clamp(_currentY, minYAngle, maxYAngle);
    }

    void MoveCamera()
    {
        float targetDist    = IsAiming ? aimDistance    : distance;
        float targetHeight  = IsAiming ? aimHeight      : height;
        float targetOffsetX = IsAiming ? aimOffsetX     : shoulderOffsetX;


        _currentDistance = Mathf.Lerp(_currentDistance, targetDist,    Time.deltaTime * aimSmooth);
        _currentHeight   = Mathf.Lerp(_currentHeight,   targetHeight,  Time.deltaTime * aimSmooth);
        _currentOffsetX  = Mathf.Lerp(_currentOffsetX,  targetOffsetX, Time.deltaTime * aimSmooth);
        Quaternion rotation = Quaternion.Euler(_currentY, _currentX, 0);
        Vector3 pivotPoint  = target.position + Vector3.up * _currentHeight;
        Vector3 desiredPos  = pivotPoint - rotation * Vector3.forward * _currentDistance;

        float actualDistance = _currentDistance;
        if (Physics.SphereCast(pivotPoint, collisionRadius, desiredPos - pivotPoint,
                               out RaycastHit hit, _currentDistance, collisionLayers))
        {
            actualDistance = Mathf.Clamp(hit.distance, minDistance, _currentDistance);
        }
        transform.position = pivotPoint - rotation * Vector3.forward * actualDistance;
        transform.rotation = rotation;

        //
        transform.position += transform.right * _currentOffsetX;
    }
}