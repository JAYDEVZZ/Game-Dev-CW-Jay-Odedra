using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public class Player : MonoBehaviour
{
    private PlayerActions _inputActions;

    [SerializeField] private AnyStateAnimator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private NavMeshAgent navMeshAgent;

    #region Internal State
    private Vector2 _moveDir;
    private bool    _isRunning;
    private bool    _isCrouching;
    private bool    _isDead;
    private float   _currentSpeed;
    private float   _vVelocity;
    #endregion

    #region Tuning Parameters
    [Header("Speed Settings")]
    [SerializeField] private float walkSpeed   = 2.0f;
    [SerializeField] private float runSpeed    = 5.0f;
    [SerializeField] private float crouchSpeed = 1.0f;
    [Header("Physics & Rotation")]
    [SerializeField] private float rotationSpeed = 10.0f;
    [SerializeField] private float gravityForce  = -9.81f;
    [Header("Collision Heights")]
    [SerializeField] private float standingHeight  = 1.27f;
    [SerializeField] private float crouchingHeight = 0.8f;
    [Header("Aim Settings")]
    [SerializeField] private float aimSpeedMultiplier = 0.4f;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [Header("Footstep Sounds")]
    [SerializeField] private float walkSoundRadius     = 3f;
    [SerializeField] private float runSoundRadius      = 9f;
    [SerializeField] private float walkDetectionAmount = 0.04f;
    [SerializeField] private float runDetectionAmount  = 0.12f;
    #endregion

    private PlayerAudioSystem _audio;

    private void Awake()
    {
        _inputActions = new PlayerActions();
        _currentSpeed = walkSpeed;
        _audio        = GetComponent<PlayerAudioSystem>();

        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
        if (navMeshAgent != null)
        {
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = false;
        }

        BindInputEvents();
    }

    private void Start()
    {
        SetupAnimations();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void Update()
    {
        if (!_isDead)
        {
            HandleLocomotion();
            HandleRotation();
        }
    }

        public void ReturnToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;
        SceneManager.LoadScene("MainMenu");
    }

    // ---- called by anination events on walk/run clips ----
    public void PlayFootstep()
    {
        if (_isCrouching) return;

        _audio?.PlayFootstep(_isRunning);
        float radius = _isRunning ? runSoundRadius  : walkSoundRadius;
        float amount = _isRunning ? runDetectionAmount : walkDetectionAmount;
        SoundManager.EmitSound(transform.position, radius, amount);
    }

    public void OnDeath()
    {
        _isDead = true;
        StartCoroutine(HandleDeath());
    }

    private IEnumerator HandleDeath()
    {
        characterController.enabled = false;
        if (navMeshAgent != null) navMeshAgent.enabled = false;
        yield return new WaitForSeconds(2.5f);
    }

    private void BindInputEvents()
    {
        _inputActions.Controls.Move.performed += ctx => _moveDir = ctx.ReadValue<Vector2>();
        _inputActions.Controls.Move.canceled  += ctx => _moveDir = Vector2.zero;
        _inputActions.Controls.Run.performed  += ctx => ToggleRun();
        var crouch = _inputActions.Controls.Get().FindAction("Crouch");
        if (crouch != null) crouch.performed += ctx => ToggleCrouch();
    }

    private void HandleLocomotion()
    {
        bool isGrounded = Physics.SphereCast(
            transform.position + characterController.center,
            characterController.radius - 0.01f,
            Vector3.down, out _,
            (characterController.height / 2f) + 0.1f);

        if (isGrounded && _vVelocity < 0) _vVelocity = -5f;



        _vVelocity += gravityForce * Time.deltaTime;

        Vector3 moveSide     = transform.right   * _moveDir.x;
        Vector3 moveForward  = transform.forward * _moveDir.y;
        Vector3 verticalMove = Vector3.up * _vVelocity;

        bool  isAiming       = thirdPersonCamera != null && thirdPersonCamera.IsAiming;
        float speedThisFrame = _currentSpeed * (isAiming ? aimSpeedMultiplier : 1f);

        characterController.Move(
            ((moveSide + moveForward) * speedThisFrame + verticalMove) * Time.deltaTime);


        if (_moveDir.sqrMagnitude > 0.01f)
            DetermineMovementAnimation();
        else
            animator.TryPlayAnimaiton(_isCrouching ? "CrouchIdle" : "Stand");
    }

    private void HandleRotation()
    {

        bool isAiming = thirdPersonCamera != null && thirdPersonCamera.IsAiming;
        if (!isAiming && _moveDir.sqrMagnitude < 0.01f) return;

        float      camYaw         = Camera.main.transform.eulerAngles.y;
        Quaternion targetRotation = Quaternion.Euler(0, camYaw, 0);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void ToggleRun()
    {

        if (_isCrouching) return;
        _isRunning    = !_isRunning;
        _currentSpeed = _isRunning ? runSpeed : walkSpeed;
    }

    private void ToggleCrouch()
    {
        _isCrouching  = !_isCrouching;
        _isRunning    = false;
        _currentSpeed = _isCrouching ? crouchSpeed : walkSpeed;



        float h     = _isCrouching ? crouchingHeight : standingHeight;
        float footY = characterController.center.y - characterController.height / 2f;
        characterController.height = h;
        characterController.center = new Vector3(0, footY + h / 2f, 0);

        if (navMeshAgent != null) navMeshAgent.height = h;
    }

    private void DetermineMovementAnimation()
    {
        string prefix = _isCrouching ? "Crouch" : (_isRunning ? "Run" : "Walk");
        string dir    = "Forward";


        if      (_moveDir.y < -0.1f) dir = "Back";
        else if (_moveDir.x >  0.1f) dir = "Right";
        else if (_moveDir.x < -0.1f) dir = "Left";

        animator.TryPlayAnimaiton(prefix + dir);
    }

    private void SetupAnimations()
    {
        string p = "Die";
        animator.AddAnimation(
            new AnyStateAnimation("Stand",         p),
            new AnyStateAnimation("Die"),
            new AnyStateAnimation("CrouchIdle",    p),
            new AnyStateAnimation("WalkForward",   p),
            new AnyStateAnimation("WalkBack",      p),
            new AnyStateAnimation("WalkLeft",      p),
            new AnyStateAnimation("WalkRight",     p),
            new AnyStateAnimation("RunForward",    p),
            new AnyStateAnimation("RunBack",       p),
            new AnyStateAnimation("RunLeft",       p),
            new AnyStateAnimation("RunRight",      p),
            new AnyStateAnimation("CrouchForward", p),
            new AnyStateAnimation("CrouchBack",    p),
            new AnyStateAnimation("CrouchLeft",    p),
            new AnyStateAnimation("CrouchRight",   p)
        );
    }


    private void OnEnable()  => _inputActions.Enable();
    private void OnDisable() => _inputActions.Disable();
}