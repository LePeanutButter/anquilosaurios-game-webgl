using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

/// <summary>
/// 2D player controller that moves the GameObject horizontally (X axis only).
/// Uses Unity's Input System and Rigidbody2D for physics-based movement.
/// Supports walking, running, jumping, and ground detection.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(NetworkTransform))]
public sealed class PlayerPresenter : NetworkBehaviour
{
    #region Inspector Fields

    [Header("Movement Settings")]
    [SerializeField, Tooltip("Base walking speed (units/sec)."), Range(0f, 50f)]
    private float walkSpeed = 8f;

    [SerializeField, Tooltip("Run speed multiplier."), Range(1f, 5f)]
    private float runSpeed = 1.8f;

    [SerializeField, Tooltip("Initial vertical jump velocity (units/sec).")]
    private float jumpVelocity = 8.5f;

    [SerializeField, Tooltip("Enable Rigidbody2D interpolation for smooth movement.")]
    private bool useInterpolation = true;

    [Header("Ground Check")]
    [SerializeField, Tooltip("Layer mask used to detect what counts as ground.")]
    private LayerMask groundLayer = ~0;

    [SerializeField] private Transform groundCheckPoint;

    [SerializeField] private float groundCheckRadius = 0.1f;

    [Header("Health Settings")]
    [SerializeField] private static float maxHealth = 100f;

    #endregion

    #region Private Fields

    private Rigidbody2D _rb;
    private Collider2D _collider;
    private NetworkTransform _netTransform;
    private Animator _animator;
    private PlayerState _playerState;
    private Vector2 _moveInput;

    private bool _isRunning;
    private bool _isGrounded;
    private bool _isClientMoving;
    private float _currentHealth;
    private float _lastMoveTime;
    private bool _isAlive = true;

    private static readonly int IsMovingParam = Animator.StringToHash("IsMoving");
    private static readonly int IsRunningParam = Animator.StringToHash("IsRunning");
    private static readonly int IsGroundedParam = Animator.StringToHash("IsGrounded");

    #endregion

    #region Network Variables

    private NetworkVariable<float> _networkHealth = new NetworkVariable<float>(
        maxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _networkIsAlive = new NetworkVariable<bool>(
        true, 
        NetworkVariableReadPermission.Everyone, 
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> _isImmune = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private NetworkVariable<bool> netIsMoving = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<bool> netIsRunning = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private NetworkVariable<bool> netIsGrounded = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );


    #endregion

    #region Public Properties & Events

    /// <summary>
    /// Gets the current speed of the player. This is calculated based on whether the player is running or walking.
    /// If the player is running, the speed will be `runSpeed`, otherwise, it will be `walkSpeed`.
    /// </summary>
    public float CurrentSpeed => walkSpeed * (_isRunning ? runSpeed : 1f);

    /// <summary>
    /// Gets a boolean indicating whether the player is currently grounded. 
    /// This is usually used to check if the player is standing on a solid surface.
    /// </summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>
    /// Gets the unique player ID. This will return the player's ID from the `_playerState` object if it exists, 
    /// otherwise, it falls back to the `OwnerClientId` as the player identifier.
    /// </summary>
    public ulong PlayerId => _playerState != null ? _playerState.PlayerId.Value : OwnerClientId;

    /// <summary>
    /// Gets the current health value of the player. This is typically updated over time as the player takes damage.
    /// The value is fetched from a networked health value, so it might be synchronized with a server.
    /// </summary>
    public float CurrentHealth => _networkHealth.Value;

    /// <summary>
    /// Gets the maximum health value of the player. This is typically set when the player is created or spawned.
    /// </summary>
    public float MaxHealth => maxHealth;

    /// <summary>
    /// Gets a boolean indicating whether the player is currently alive. 
    /// This is typically used to check if the player has died or if they are still active in the game.
    /// </summary>
    public bool IsAlive => _isAlive;

    /// <summary>
    /// Returns whether the player is currently immune.
    /// </summary>
    /// <returns>True if the player is immune, false otherwise.</returns>
    public bool IsImmune() => _isImmune.Value;

    /// <summary>
    /// Event triggered when the player dies. This could be used to trigger UI updates, animations, or other game mechanics 
    /// related to the player’s death (e.g., game over screen, respawn, etc.).
    /// </summary>
    public event Action OnDeath;

    #endregion

    #region Initialization & Network Lifecycle

    /// <summary>
    /// Unity callback invoked when the script instance is being loaded.
    /// Initializes required components and sets Rigidbody2D interpolation mode based on inspector settings.
    /// </summary>
    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _collider = GetComponent<Collider2D>();
        _netTransform = GetComponent<NetworkTransform>();
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

        if (_netTransform == null)
        {
            Debug.LogError("PlayerController requires a NetworkTransform component.");
        }

        _rb.interpolation = useInterpolation
            ? RigidbodyInterpolation2D.Interpolate
            : RigidbodyInterpolation2D.None;
    }

    /// <summary>
    /// Initializes the player presenter with the given PlayerState.
    /// </summary>
    /// <param name="state">The PlayerState associated with this player.</param>
    public void InitializePresenter(PlayerState state)
    {
        if (state == null)
        {
            Debug.LogError("Provided PlayerState is null.");
            return;
        }
        _playerState = state;
        Debug.Log($"PlayerPresenter: Assigned PlayerState with ClientId: {state.OwnerClientId}");
    }

    /// <summary>
    /// Unity callback invoked when the object is spawned over the network.
    /// Registers network variables and handles the initial setup for the local player.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        _networkIsAlive.OnValueChanged += OnIsAliveChanged;

        PlayerInput playerInput = GetComponent<PlayerInput>();

        if (IsServer)
        {
            _currentHealth = maxHealth;
            _isAlive = true;

            ActivateImmunityServerRpc(2f);
        }

        if (IsOwner)
        {
            Debug.Log("PlayerPresenter: I am the owner of this avatar! (Local Control)");
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }
        }
        else
        {
            Debug.Log("PlayerPresenter: I am a remote client. (Remote Synchronization)");
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }
        }
    }

    /// <summary>
    /// Callback invoked when the player's alive status changes.
    /// Handles death events for remote players.
    /// </summary>
    /// <param name="previousValue">The previous alive status of the player.</param>
    /// <param name="newValue">The new alive status of the player.</param>
    private void OnIsAliveChanged(bool previousValue, bool newValue)
    {
        if (!newValue)
        {
            HandleRemoteDeath();
        }
    }

    /// <summary>
    /// Called when the networked player object is despawned from the network.
    /// This callback is invoked when the object is removed from the network session.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Unity callback invoked at fixed time intervals (Physics Updates).
    /// Moves the player based on input and current speed, and synchronizes movement with the server.
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

            netIsMoving.Value = IsMoving();
            netIsRunning.Value = _isRunning;
            netIsGrounded.Value = _isGrounded;

            if (!IsServer)
            {
                UpdateMovementStateServerRpc(IsMoving(), _isRunning, _isGrounded);
            }
        }

        if (IsServer)
        {
            float tickInterval = Time.fixedDeltaTime;

            bool moving = IsOwner ? IsMoving() : _isClientMoving;

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
        }
    }

    /// <summary>
    /// Unity callback invoked every frame to update animations and movement.
    /// Also, adjusts the facing direction of the player based on input.
    /// </summary>
    private void Update()
    {
        ApplyAnimations();
        if (IsOwner)
        {
            SetFacingDirection(_moveInput);
        }
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

    #region Health & Death Handling

    /// <summary>
    /// Called when the player dies. Handles logic when death state is received.
    /// </summary>
    public void OnPlayerDiedStateChanged()
    {
        if (!IsOwner) return;

        enabled = false;

        Debug.Log($"PlayerPresenter: Player {PlayerId} received death notification from state.");

    }

    /// <summary>
    /// Handles the death of a remote player by disabling movement and animations.
    /// This method is called for remote players when they die.
    /// </summary>
    private void HandleRemoteDeath()
    {
        Debug.Log($"PlayerPresenter: Player {PlayerId} has died.");

        var input = GetComponent<PlayerInput>();
        if (input != null) input.enabled = false;

        _rb.simulated = false;
        _collider.enabled = false;

        if (_animator != null) _animator.enabled = false;
    }

    /// <summary>
    /// Updates the player's health and alive status, and triggers the death event if the player dies.
    /// This method is called whenever the player's health is modified.
    /// </summary>
    /// <param name="newHealth">The new health value to set for the player.</param>
    private void SetHealth(float newHealth)
    {
        if (!IsServer) return;

        bool wasAlive = _isAlive;

        _currentHealth = newHealth;
        _isAlive = _currentHealth > 0f;
        _networkHealth.Value = _currentHealth;
        _networkIsAlive.Value = _isAlive;

        if (wasAlive && !_isAlive)
        {
            Debug.Log($"Player {OwnerClientId} has died.");
            OnDeath?.Invoke();
        }
    }

    #endregion

    #region Network RPCs (Server Authority)

    /// <summary>
    /// Sends movement intent and current state.
    /// This RPC is called by the owner (local player) to update movement state on the server.
    /// </summary>
    [ServerRpc]
    private void UpdateMovementStateServerRpc(bool isMoving, bool isRunning, bool isGrounded)
    {
        _isRunning = isRunning;
        _isGrounded = isGrounded;
        _isClientMoving = isMoving;
    }

    /// <summary>
    /// Applies exponential damage based on missing health.
    /// The damage increases as the player's health decreases, with a higher damage scaling for larger health deficits.
    /// </summary>
    /// <param name="tickInterval">Time interval for damage calculation. This is typically the fixed time between physics updates.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyExponentialDamageServerRpc(float tickInterval)
    {
        if (!IsServer || !_isAlive) return;

        if (IsImmune())
        {
            Debug.Log($"[Server] Player {OwnerClientId} is immune, no damage applied.");
            return;
        }

        float baseDamage = 5f * tickInterval;
        float missingHealth = Mathf.Max(0f, maxHealth - _currentHealth);
        float scalingDamage = Mathf.Pow(missingHealth, 1.2f) * tickInterval;
        float totalDamage = baseDamage + scalingDamage;

        float newHealth = Mathf.Max(0f, _currentHealth - totalDamage);
        SetHealth(newHealth);

        if (totalDamage > 0.1f)
        {
            Debug.Log($"[Server] Player {OwnerClientId} received {totalDamage:F2} damage. Health: {newHealth:F1}/{maxHealth}");
        }
    }

    /// <summary>
    /// Applies linear health recovery over time. The recovery is a fixed percentage of the player's maximum health.
    /// </summary>
    /// <param name="tickInterval">Time interval for recovery calculation. This is typically the fixed time between physics updates.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyLinearRecoveryServerRpc(float tickInterval)
    {
        if (!IsServer || !_isAlive) return;

        float recoveryAmount = maxHealth * 0.1f * tickInterval;
        float newHealth = Mathf.Min(maxHealth, _currentHealth + recoveryAmount);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Resets the player's health to the maximum value.
    /// This method is typically called after the player respawns or when health needs to be fully restored.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        if (!IsServer) return;
        SetHealth(maxHealth);
    }

    /// <summary>
    /// Activates temporary immunity for the player during the specified duration.
    /// </summary>
    /// <param name="duration">Duration in seconds for which the player is immune.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ActivateImmunityServerRpc(float duration)
    {
        if (!IsServer) return;
        if (!gameObject.activeInHierarchy) return;

        StartCoroutine(ImmunityRoutine(duration));
    }

    /// <summary>
    /// Handles the logic when the player collides with a lethal object.
    /// Attempts to invoke a "Kill" method on the domain controller, or sets health to 0.
    /// Updates the health bar HUD and triggers death handling.
    /// </summary>
    /// <param name="lethalObject">The lethal GameObject that caused the collision.</param>
    [ServerRpc(RequireOwnership = false)]
    private void HandleLethalCollisionServerRpc(ulong playerId)
    {
        if (!_isAlive || IsImmune()) return;

        Debug.Log($"[Server] Applying lethal damage to Player {playerId}");
        SetHealth(0f);
    }

    #endregion

    #region Animation


    /// <summary>
    /// Updates the Animator component parameters based on synchronized player state.
    /// This method runs on all clients, updating the player's animation state (e.g., running, grounded, moving).
    /// </summary>
    private void ApplyAnimations()
    {
        if (_animator == null) return;

        bool moving = netIsMoving.Value;
        bool running = netIsRunning.Value;
        bool grounded = netIsGrounded.Value;

        _animator.SetBool(IsMovingParam, moving);
        _animator.SetBool(IsRunningParam, running);
        _animator.SetBool(IsGroundedParam, grounded);
    }

    #endregion

    #region Collision Detection

    /// <summary>
    /// Checks if the player is grounded by performing a circle overlap check at the ground check point.
    /// The check looks for colliders that belong to the "ground" layer or other relevant layers.
    /// </summary>
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
    /// If the other object is on the "Lethal" layer, it triggers the lethal collision handler.
    /// </summary>
    /// <param name="other">The collider that entered the trigger.</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsServer) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Lethal"))
        {
            ulong playerId = OwnerClientId;
            Debug.Log($"[Server] Player {playerId} collided with lethal object {other.gameObject.name}");

            HandleLethalCollisionServerRpc(playerId);
        }
    }

    #endregion

    #region Input System Callbacks

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
    /// Input System callback for jump. Triggered when the jump button is pressed.
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

            AudioManager.Instance.PlaySFXNetworked(AudioManager.Instance.jump);
        }
    }

    /// <summary>
    /// Updates the player's facing direction based on horizontal input.
    /// Flips the local scale on the X-axis only when direction changes.
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

    /// <summary>
    /// Returns true if the player is effectively moving (i.e., moving horizontally).
    /// This method checks the player's horizontal velocity against a threshold to determine if they are moving.
    /// </summary>
    /// <param name="velocityThreshold">The velocity threshold for considering the player as moving. Default is 0.05f.</param>
    /// <returns>True if the player is moving horizontally, otherwise false.</returns>
    public bool IsMoving(float velocityThreshold = 0.05f)
    {
        if (_rb == null) return true;

        return Mathf.Abs(_rb.linearVelocity.x) > velocityThreshold;
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

    #region Immunity (QTE Event)

    /// <summary>
    /// Coroutine that manages the player's immunity state and triggers events.
    /// </summary>
    /// <param name="duration">Duration of immunity in seconds.</param>
    private IEnumerator ImmunityRoutine(float duration)
    {
        SetImmune(true);
        Debug.Log($"[PlayerPresenter {OwnerClientId}] Player is now immune!");

        yield return new WaitForSeconds(duration);

        SetImmune(false);
        Debug.Log($"[PlayerPresenter {OwnerClientId}] Immunity ended.");
    }

    /// <summary>
    /// Setter to update the player's immunity status and trigger the event.
    /// </summary>
    /// <param name="value">True to activate immunity, false to deactivate.</param>
    private void SetImmune(bool value)
    {
        if (!IsServer) return;

        if (_isImmune.Value == value) return;

        _isImmune.Value = value;
        Debug.Log($"[PlayerPresenter {OwnerClientId}] Immunity changed to: {value}");
    }

    #endregion
}