using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D player controller that moves the GameObject horizontally (X axis only).
/// Uses Unity's Input System and Rigidbody2D for physics-based movement.
/// Supports walking, running, jumping, and ground detection.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class PlayerPresenter : MonoBehaviour
{
    #region Inspector Fields

    [SerializeField, Tooltip("Base walking speed (units/sec)."), Range(0f, 50f)]
    private float walkSpeed = 8f;
    [SerializeField, Tooltip("Run speed multiplier."), Range(1f, 5f)]
    private float runSpeed = 1.8f;
    [SerializeField, Tooltip("Velocidad vertical inicial del salto (unidades/seg).")]
    private float jumpVelocity = 6f;
    [SerializeField, Tooltip("Enable Rigidbody2D interpolation for smooth movement.")]
    private bool useInterpolation = true;
    [SerializeField, Tooltip("Layer mask used to detect what counts as ground.")]
    private LayerMask groundLayer = ~0;
    [SerializeField, Tooltip("Time in seconds between health updates.")]
    private float healthTickInterval = 1f;

    #endregion

    #region Private Fields

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private PlayerController _domainController;
    private HealthBarHUD _healthBarHUD;
    private Vector2 _moveInput;
    private bool _isRunning;
    private bool _isGrounded;
    private float _tickTimer;

    #endregion

    #region Public Properties

    /// <summary>
    /// Indicates whether the player is currently moving.
    /// </summary>
    public bool IsMoving { get; private set; }

    /// <summary>
    /// Current movement speed based on walk speed and run multiplier.
    /// </summary>
    public float CurrentSpeed => walkSpeed * (_isRunning ? runSpeed : 1f);

    /// <summary>
    /// Indicates whether the player is currently grounded.
    /// </summary>
    public bool IsGrounded => _isGrounded;
    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the player controller and health bar HUD components for the current instance.
    /// This method should be called during the setup phase to establish necessary gameplay references.
    /// </summary>
    /// <param name="domainController">The player controller managing player-specific logic and interactions.</param>
    /// <param name="hud">The health bar HUD component responsible for displaying the player's health status.</param>
    public void Initialize(PlayerController domainController, HealthBarHUD hud)
    {
        _domainController = domainController;
        _healthBarHUD = hud;

        if (_healthBarHUD != null && _domainController != null)
        {
            _healthBarHUD.SetHealth(_domainController.GetHealth(), _domainController.GetMaxHealth());
        }
    }


    /// <summary>
    /// Unity callback invoked when the script instance is being loaded.
    /// Initializes required components and sets Rigidbody2D interpolation mode based on inspector settings.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();

        if (_rb == null)
        {
            Debug.LogError("PlayerController requires a Rigidbody2D component.");
            enabled = false;
            return;
        }

        if (_collider == null)
        {
            Debug.LogError("PlayerController requires a Collider2D for ground checks.");
            enabled = false;
            return;
        }

        _rb.interpolation = useInterpolation
            ? RigidbodyInterpolation2D.Interpolate
            : RigidbodyInterpolation2D.None;
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Unity callback invoked at a fixed time interval, used for physics updates.
    /// Applies horizontal movement to the Rigidbody2D based on input and current speed.
    /// </summary>
    private void FixedUpdate()
    {
        float horizontal = _moveInput.x;
        float targetSpeed = horizontal * CurrentSpeed;
        _rb.linearVelocity = new Vector2(targetSpeed, _rb.linearVelocity.y);
    }

    private void Update()
    {
        if (_domainController == null || _healthBarHUD == null)
            return;

        _tickTimer += Time.deltaTime;
        if (_tickTimer >= healthTickInterval)
        {
            _tickTimer = 0f;

            _domainController.Tick(IsMoving);

            int currentHealth = _domainController.GetHealth();
            int maxHealth = _domainController.GetMaxHealth();
            _healthBarHUD.SetHealth(currentHealth, maxHealth);

            if (!_domainController.IsAlive())
            {
                HandleDeath();
            }
        }
    }

    private void HandleDeath()
    {
        Debug.Log($"Player {_domainController.GetId()} has died.");
        enabled = false;
        // TODO: Trigger animation, notify game manager, etc.
    }

#if UNITY_EDITOR

    /// <summary>
    /// Unity Editor callback invoked when a value is changed in the Inspector.
    /// Ensures walk speed and run multiplier remain within valid bounds.
    /// </summary>
    private void OnValidate()
    {
        walkSpeed = Mathf.Max(0f, walkSpeed);
        runSpeed = Mathf.Clamp(runSpeed, 1f, 10f);
    }

#endif

    #endregion

    #region Collision Detection

    /// <summary>
    /// Unity callback invoked when the GameObject starts colliding with another 2D collider.
    /// Sets the grounded state to true if the collided object is on the ground layer.
    /// </summary>
    /// <param name="collision">Collision data associated with the contact.</param>
    private void OnCollisionEnter2D(Collision2D collision)
    {
        int colLayer = collision.gameObject.layer;
        string layerName = LayerMask.LayerToName(colLayer);
        bool isGroundLayerMatch = ((1 << colLayer) & groundLayer) != 0;

        if (isGroundLayerMatch)
        {
            _isGrounded = true;
        }
    }

    /// <summary>
    /// Unity callback invoked when the GameObject stops colliding with another 2D collider.
    /// Sets the grounded state to false if the object exited was on the ground layer.
    /// </summary>
    /// <param name="collision">Collision data associated with the contact.</param>
    private void OnCollisionExit2D(Collision2D collision)
    {
        int colLayer = collision.gameObject.layer;

        if (((1 << colLayer) & groundLayer) != 0)
        {
            _isGrounded = false;
        }
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Input System callback for movement. Accepts a Vector2 action but uses only X (left/right).
    /// </summary>
    /// <param name="context">Input action context.</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>();
        IsMoving = !Mathf.Approximately(_moveInput.x, 0f);

        SetFacingDirection(_moveInput);
    }

    /// <summary>
    /// Input System callback for run/sprint. Expect a button action (performed -> true; canceled -> false).
    /// </summary>
    /// <param name="context">Input action context.</param>
    public void OnRun(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _isRunning = true;
        }
        else if (context.canceled)
        {
            _isRunning = false;
        }
    }

    /// <summary>
    /// Input System callback for jump. Triggered when jump button is pressed.
    /// </summary>
    /// <param name="context">Input action context.</param>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!context.performed)
            return;

        if (_isGrounded)
        {
            float horizontalVelocity = _rb.linearVelocity.x;
            _rb.linearVelocity = new Vector2(horizontalVelocity, jumpVelocity);
            _isGrounded = false;
        }
    }

    /// <summary>
    /// Updates the player's facing direction based on horizontal input.
    /// Flips the local scale on X axis only when direction changes.
    /// </summary>
    /// <param name="input">Movement input vector.</param>
    private void SetFacingDirection(Vector2 input)
    {
        float direction = input.x;

        if (Mathf.Approximately(direction, 0f))
            return;

        float currentScaleX = transform.localScale.x;
        float desiredScaleX = Mathf.Sign(direction) * Mathf.Abs(currentScaleX);

        if (!Mathf.Approximately(currentScaleX, desiredScaleX))
        {
            Vector3 newScale = transform.localScale;
            newScale.x = desiredScaleX;
            transform.localScale = newScale;
        }
    }


    #endregion

    #region Public API

    /// <summary>
    /// Set the walk speed at runtime.
    /// </summary>
    /// <param name="speed">New base walk speed. Clamped to zero or greater.</param>
    public void SetWalkSpeed(float speed)
    {
        walkSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>
    /// Set the run multiplier at runtime.
    /// </summary>
    /// <param name="multiplier">Run multiplier. Minimum value is 1.</param>
    public void SetRunMultiplier(float multiplier)
    {
        runSpeed = Mathf.Max(1f, multiplier);
    }

    #endregion
}