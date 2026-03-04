using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;


public class Player : MonoBehaviour
{
    // --- Components ---
    private PlayerActions _inputActions;

    [SerializeField] private AnyStateAnimator animator;
    [SerializeField] private CharacterController characterController;
    [SerializeField] private NavMeshAgent navMeshAgent;

    // --- State + Movement Values ---
    #region Internal State
    private Vector2 _moveDir;
    private float _lookInput;
    private bool _isRunning;
    private bool _isCrouching;
    private bool _isDead;
    private float _currentSpeed;
    private float _vVelocity; // For gravity calculations
    #endregion

    #region Tuning Parameters
    [Header("Speed Settings")]
    [SerializeField] private float walkSpeed = 2.0f;
    [SerializeField] private float runSpeed = 5.0f;
    [SerializeField] private float crouchSpeed = 1.0f;
    
    [Header("Physics & Rotation")]
    [SerializeField] private float rotationSpeed = 80.0f;
    [SerializeField] private float gravityForce = -9.81f;

    [Header("Collision Heights")]
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float crouchingHeight = 1.0f;
    #endregion

    private void Awake()
    {
        _inputActions = new PlayerActions();
        _currentSpeed = walkSpeed;

        // Fallback for NavMesh if not assigned manualy
        if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();

        BindInputEvents();
    }

    private void Start()
    {
        SetupAnimations();
    }

    private void Update()
    {
        if (!_isDead) 
        { 
            HandleLocomotion(); 
            HandleRotation(); 
        }
    }

    // --- Input Binding ---

    private void BindInputEvents()
    {
        // Link New input system actions to local variables/methods
        _inputActions.Controls.Move.performed += ctx => _moveDir = ctx.ReadValue<Vector2>();
        _inputActions.Controls.Move.canceled += ctx => _moveDir = Vector2.zero;
        
        _inputActions.Controls.MouseMovement.performed += ctx => _lookInput = ctx.ReadValue<float>();
        
        _inputActions.Controls.Run.performed += ctx => ToggleRun();
        
        // Dynamic binding for Crouch
        var crouch = _inputActions.Controls.Get().FindAction("Crouch");
        if (crouch != null) crouch.performed += ctx => ToggleCrouch();
    }

    // --- Core Mechanics ---

    private void HandleLocomotion()
    {
        // Basic Grounded Physics check
        if (characterController.isGrounded && _vVelocity < 0)
        {
            _vVelocity = -2f; // Slight downward force to keep grounded
        }
        
        // Apply Gravity over time
        _vVelocity += gravityForce * Time.deltaTime;

        // Convert 2D Input into 3D World Space vectors relative to player rotation
        Vector3 moveSide = transform.right * _moveDir.x;
        Vector3 moveForward = transform.forward * _moveDir.y;
        Vector3 verticalMove = Vector3.up * _vVelocity;

        // Execute the move through the CharacterController
        characterController.Move(((moveSide + moveForward) * _currentSpeed + verticalMove) * Time.deltaTime);

        // Update Animations based on input magnitude
        if (_moveDir.sqrMagnitude > 0.01f)
        {
            DetermineMovementAnimation();
        }
        else
        {
            // Play Idle variants
            animator.TryPlayAnimaiton(_isCrouching ? "CrouchIdle" : "Stand");
        }
    }

    private void HandleRotation()
    {
        // Don't rotate character if holding RMB 
        if (Mouse.current.rightButton.isPressed) return;

        float yaw = _lookInput * rotationSpeed * Time.deltaTime;
        transform.Rotate(Vector3.up * yaw);
    }

    private void ToggleRun()
    {
        if (_isCrouching) return; // no running while crouched

        _isRunning = !_isRunning;
        _currentSpeed = _isRunning ? runSpeed : walkSpeed;
    }

    private void ToggleCrouch()
    {
        _isCrouching = !_isCrouching;
        _isRunning = false; // Stop running when entering crouch
        
        _currentSpeed = _isCrouching ? crouchSpeed : walkSpeed;

        // Adjust physical height so that the AI raycasts/physics can pass over the player
        float h = _isCrouching ? crouchingHeight : standingHeight;
        characterController.height = h;
        characterController.center = new Vector3(0, h / 2f, 0);

        // Sync NavMesh Agent height to prevent pathfinding issues while low
        if (navMeshAgent != null) navMeshAgent.height = h;
    }

    private void DetermineMovementAnimation()
    {
        // Build the animation string based on current flags
        string prefix = _isCrouching ? "Crouch" : (_isRunning ? "Run" : "Walk");
        string dir = "Forward";

        // Simple directional check for basic 4-way movement
        if (_moveDir.y < -0.1f) dir = "Back";
        else if (_moveDir.x > 0.1f) dir = "Right";
        else if (_moveDir.x < -0.1f) dir = "Left";

        animator.TryPlayAnimaiton(prefix + dir);
    }

    // --- Boilerplate & Setup ---

    private void SetupAnimations()
    {
        // Define priority: "Die" overrides everything
        string p = "Die";
        
        // Register all possible states with the AnyState system
        animator.AddAnimation(
            new AnyStateAnimation("Stand", p),
            new AnyStateAnimation("Die"),
            new AnyStateAnimation("CrouchIdle", p),
            new AnyStateAnimation("WalkForward", p),
            new AnyStateAnimation("WalkBack", p),
            new AnyStateAnimation("WalkLeft", p),
            new AnyStateAnimation("WalkRight", p),
            new AnyStateAnimation("RunForward", p),
            new AnyStateAnimation("RunBack", p),
            new AnyStateAnimation("RunLeft", p),
            new AnyStateAnimation("RunRight", p),
            new AnyStateAnimation("CrouchForward", p),
            new AnyStateAnimation("CrouchBack", p),
            new AnyStateAnimation("CrouchLeft", p),
            new AnyStateAnimation("CrouchRight", p)
        );
    }

    private void OnEnable() => _inputActions.Enable();
    private void OnDisable() => _inputActions.Disable();
}