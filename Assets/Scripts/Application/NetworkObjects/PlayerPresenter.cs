using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 2D player controller that moves the GameObject horizontally (X axis only).
/// Uses Unity's Input System and Rigidbody2D for physics-based movement.
/// Supports walking, running, jumping, and ground detection.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public sealed class PlayerPresenter : NetworkBehaviour
{
    #region Inspector Fields

    [Header("Movement Settings")]
    [SerializeField, Tooltip("Base walking speed (units/sec)."), Range(0f, 50f)]
    private float walkSpeed = 8f;
    [SerializeField, Tooltip("Run speed multiplier."), Range(1f, 5f)]
    private float runSpeed = 1.8f;
    [SerializeField, Tooltip("Velocidad vertical inicial del salto (unidades/seg).")]
    private float jumpVelocity = 8.5f;
    [SerializeField, Tooltip("Enable Rigidbody2D interpolation for smooth movement.")]
    private bool useInterpolation = true;

    [Header("Ground Check")]
    [SerializeField, Tooltip("Layer mask used to detect what counts as ground.")]
    private LayerMask groundLayer = ~0;
    [SerializeField] private Transform groundCheckPoint;
    [SerializeField] private float groundCheckRadius = 0.1f;

    [Header("Server Authority Settings")]
    [SerializeField, Tooltip("Rate at which the server synchronizes position and state to non-owners.")]
    private float serverSyncRate = 0.05f;

    [Header("Health Settings")]
    [SerializeField] private static float maxHealth = 100f;

    #endregion

    #region Private Fields

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private Animator _animator;
    private PlayerState _playerState;
    private Vector2 _moveInput;
    private bool _isRunning;
    private bool _isGrounded;
    private float _syncTimer;
    private float _currentHealth;
    private float _lastMoveTime;
    private bool _isAlive = true;

    private NetworkVariable<Vector3> _networkPosition = new(
        writePerm: NetworkVariableWritePermission.Server,
        readPerm: NetworkVariableReadPermission.Everyone);
    private NetworkVariable<float> _networkHealth = new NetworkVariable<float>(
        maxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    private static readonly int IsRunningParam = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");

    #endregion

    #region Public Properties

    /// <summary>
    /// Current movement speed based on walk speed and run multiplier.
    /// </summary>
    public float CurrentSpeed => walkSpeed * (_isRunning ? runSpeed : 1f);

    /// <summary>
    /// Indicates whether the player is currently grounded.
    /// </summary>
    public bool IsGrounded => _isGrounded;

    // <summary>
    /// PlayerId now references PlayerState's value.
    /// </summary>
    public ulong PlayerId => _playerState != null ? _playerState.PlayerId.Value : OwnerClientId;

    public float CurrentHealth => _networkHealth.Value;

    public float MaxHealth => maxHealth;
    public bool IsAlive => _isAlive;

    #endregion

    #region Initialization & Network Spawn

    /// <summary>
    /// Unity callback invoked when the script instance is being loaded.
    /// Initializes required components and sets Rigidbody2D interpolation mode based on inspector settings.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _animator = GetComponent<Animator>();

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

        if (_animator == null)
        {
            Debug.LogError("PlayerController requires an Animator.");
            enabled = false;
            return;
        }

        _rb.interpolation = useInterpolation
            ? RigidbodyInterpolation2D.Interpolate
            : RigidbodyInterpolation2D.None;
    }

    public void InitializePresenter(PlayerState state)
    {
        if (state == null)
        {
            Debug.LogError("El PlayerState proporcionado es nulo.");
            return;
        }
        _playerState = state;
        Debug.Log($"PlayerPresenter: Asignado PlayerState con ClientId: {state.OwnerClientId}");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PlayerInput playerInput = GetComponent<PlayerInput>();

        if (IsServer)
        {
            _currentHealth = maxHealth;
            _isAlive = true;
        }

        if (IsOwner)
        {
            Debug.Log("PlayerPresenter: ¡Soy el dueño de este avatar! (Control Local)");
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }

            EnableLocalControl();
            RequestPositionUpdateServerRpc(transform.position);
        }
        else
        {
            Debug.Log("PlayerPresenter: Soy un cliente remoto. (Sincronización remota)");
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }

        if (!IsOwner && !IsServer)
        {
            _networkPosition.OnValueChanged += OnNetworkPositionChanged;
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (!IsOwner && !IsServer)
        {
            _networkPosition.OnValueChanged -= OnNetworkPositionChanged;
        }
    }

    private void EnableLocalControl()
    {
        // Esto debería habilitar el manejo de input para el dueño local
        // Asegúrate de que los callbacks de Input (OnMove, OnRun, OnJump) solo se ejecuten si IsOwner es true.
        // Los métodos de input ya tienen la comprobación !IsOwner return;
    }

    [ServerRpc]
    private void RequestPositionUpdateServerRpc(Vector3 newPosition)
    {
        _networkPosition.Value = newPosition;
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Unity callback invoked at a fixed time interval, used for physics updates.
    /// Applies horizontal movement to the Rigidbody2D based on input and current speed.
    /// </summary>
    private void FixedUpdate()
    {
        if (!_isAlive) return;

        CheckGrounded();

        if (IsOwner)
        {
            float horizontal = _moveInput.x;
            float targetSpeed = horizontal * CurrentSpeed;
            _rb.linearVelocity = new Vector2(targetSpeed, _rb.linearVelocity.y);

            if (!IsServer)
            {
                UpdateMovementStateServerRpc(IsMoving(), _isRunning, _isGrounded);
            }
        }

        if (IsServer)
        {
            ServerAuthoritySync();
            float tickInterval = Time.fixedDeltaTime;

            bool moving = IsMoving();

            if (moving)
            {
                _lastMoveTime = Time.time;
                ApplyLinearRecoveryServerRpc(tickInterval);
            }
            else
            {
                float timeSinceMove = Time.time - _lastMoveTime;
                if (timeSinceMove >= 0.2f)
                {
                    ApplyExponentialDamageServerRpc(tickInterval);
                }
            }
            Debug.Log($"PlayerPresenter: Player {PlayerId} health is {_currentHealth}");
        }
    }

    private void ServerAuthoritySync()
    {
        _syncTimer += Time.deltaTime;

        if (_syncTimer >= serverSyncRate)
        {
            _networkPosition.Value = transform.position;

            UpdateAnimationClientRpc(IsMoving(), _isRunning, _isGrounded);

            _syncTimer = 0f;
        }
    }

    private void Update()
    {
        ApplyAnimations();
        if (IsOwner)
        {
            SetFacingDirection(_moveInput);
        }
    }

    /// <summary>
    /// Handler when the player's health or state change causes death.
    /// </summary>
    public void OnPlayerDiedStateChanged()
    {
        if (!IsOwner) return;

        enabled = false;

        Debug.Log($"PlayerPresenter: Player {PlayerId} received death notification from state.");

    }

    public void OnPlayerHealthChanged(float newHealth)
    {
        // Lógica de presentación: por ejemplo, actualizar la barra de salud del HUD.
        // Debug.Log($"Presenter {PlayerId} health updated: {newHealth}");
    }

    private void HandleDeath()
    {
        if (!IsOwner) return;

        SetHealth(0f);

        Debug.Log($"PlayerPresenter: Player {PlayerId} triggered death logic, requesting damage from state.");
    }

    /// <summary>
    /// Returns true if the player is effectively idle.
    /// </summary>
    public bool IsMoving(float velocityThreshold = 0.05f)
    {
        if (_rb == null) return true;

        return Mathf.Abs(_rb.linearVelocity.x) > velocityThreshold;
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

    #region Network RPCs (Server Authority)

    /// <summary>
    /// Owner -> Server: Envía la intención de movimiento y el estado actual.
    /// </summary>
    [ServerRpc]
    private void UpdateMovementStateServerRpc(bool isMoving, bool isRunning, bool isGrounded)
    {
        _isRunning = isRunning;
        _isGrounded = isGrounded;
    }

    /// <summary>
    /// Server -> Everyone: Sincroniza el estado de animación para que se visualice correctamente.
    /// Esto es más eficiente que sincronizar 3 NetworkVariables.
    /// </summary>
    [ClientRpc]
    private void UpdateAnimationClientRpc(bool isMoving, bool isRunning, bool isGrounded)
    {
        if (IsOwner) return;

        _isGrounded = isGrounded;
        _isRunning = isRunning;

    }

    /// <summary>
    /// Callback para clientes remotos (no dueños y no servidor) cuando la posición de red cambia.
    /// </summary>
    private void OnNetworkPositionChanged(Vector3 previous, Vector3 current)
    {
        if (!IsOwner)
        {
            transform.position = current;
        }
    }

    /// <summary>
    /// Applies exponential damage based on missing health.
    /// </summary>
    /// <param name="tickInterval">Time interval for damage calculation.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyExponentialDamageServerRpc(float tickInterval)
    {
        if (!IsServer || !_isAlive) return;

        float baseDamage = 5f * tickInterval;
        float missingHealth = Mathf.Max(0f, maxHealth - _currentHealth);
        float scalingDamage = Mathf.Pow(missingHealth, 1.2f) * tickInterval;
        float totalDamage = baseDamage + scalingDamage;

        float newHealth = Mathf.Max(0f, _currentHealth - totalDamage);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Applies linear health recovery over time.
    /// </summary>
    /// <param name="tickInterval">Time interval for recovery calculation.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyLinearRecoveryServerRpc(float tickInterval)
    {
        if (!IsServer || !_isAlive) return;

        float recoveryAmount = maxHealth * 0.1f * tickInterval;
        float newHealth = Mathf.Min(maxHealth, _currentHealth + recoveryAmount);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Resets the player's health to maximum.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        if (!IsServer) return;
        SetHealth(maxHealth);
    }

    /// <summary>
    /// Updates health and alive status.
    /// </summary>
    /// <param name="newHealth">New health value.</param>
    private void SetHealth(float newHealth)
    {
        if (!IsServer) return;
        _currentHealth = newHealth;
        _isAlive = _currentHealth > 0f;
        _networkHealth.Value = _currentHealth;
    }

    #endregion

    #region Animation


    /// <summary>
    /// Updates the Animator component parameters based on SYNCHRONIZED player state.
    /// This runs on all clients.
    /// </summary>
    private void ApplyAnimations()
    {
        if (_animator == null) return;

        if (IsOwner || IsServer)
        {
            _animator.SetBool(IsMovingParam, IsMoving());
            _animator.SetBool(IsRunningParam, _isRunning);
            _animator.SetBool(IsGroundedParam, _isGrounded);
        }
        else
        {
            _animator.SetBool(IsMovingParam, _moveInput.x != 0);
            _animator.SetBool(IsRunningParam, _isRunning);
            _animator.SetBool(IsGroundedParam, _isGrounded);
        }
    }

    #endregion

    #region Collision Detection

    private void CheckGrounded()
    {
        _isGrounded = false;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(groundCheckPoint.position, groundCheckRadius);

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject)
                continue;

            if (((1 << col.gameObject.layer) & groundLayer) != 0 || col.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                _isGrounded = true;
                break;
            }
        }
    }


    /// <summary>
    /// Unity callback invoked when another collider enters this GameObject's trigger collider.
    /// If the other object is on the "Lethal" layer, triggers the lethal collision handler.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Lethal"))
        {
            HandleLethalCollision(other.gameObject);
        }
    }

    /// <summary>
    /// Handles the logic when the player collides with a lethal object.
    /// Attempts to invoke a "Kill" method on the domain controller, or sets health to 0.
    /// Updates the health bar HUD and triggers death handling.
    /// </summary>
    /// <param name="lethalObject">The lethal GameObject that caused the collision.</param>
    private void HandleLethalCollision(GameObject lethalObject)
    {
        if (!IsOwner) return;

        Debug.Log($"PlayerPresenter: Player {PlayerId} collided with lethal object.");
        HandleDeath();
    }

    #endregion

    #region Input Callbacks

    /// <summary>
    /// Input System callback for movement. Accepts a Vector2 action but uses only X (left/right).
    /// </summary>
    /// <param name="context">Input action context.</param>
    public void OnMove(InputAction.CallbackContext context)
    {
        string prefabName = gameObject.name;
        if (!IsOwner) return;

        _moveInput = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Input System callback for run/sprint. Expect a button action (performed -> true; canceled -> false).
    /// </summary>
    /// <param name="context">Input action context.</param>
    public void OnRun(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        _isRunning = context.performed;
    }

    /// <summary>
    /// Input System callback for jump. Triggered when jump button is pressed.
    /// </summary>
    /// <param name="context">Input action context.</param>
    public void OnJump(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

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