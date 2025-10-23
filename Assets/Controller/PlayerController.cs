using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Player
{
    /// <summary>
    /// 2D player controller that moves the GameObject horizontally only (X axis).
    /// Consumes Unity Input System actions and moves the GameObject via Rigidbody2D
    /// to preserve physics behavior.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class PlayerController : MonoBehaviour
    {
        #region Inspector Fields

        /// <summary>
        /// Base walking speed in units per second.
        /// </summary>
        [SerializeField, Tooltip("Base walking speed (units/sec)."), Range(0f, 50f)]
        private float walkSpeed = 5f;

        /// <summary>
        /// Run speed multiplier applied to walkSpeed when running.
        /// </summary>
        [SerializeField, Tooltip("Run speed multiplier."), Range(1f, 5f)]
        private float runSpeed = 2f;

        /// <summary>
        /// Use Rigidbody2D interpolation for smoother movement.
        /// </summary>
        [SerializeField, Tooltip("Enable Rigidbody2D interpolation for smooth movement.")]
        private bool useInterpolation = true;

        [SerializeField, Tooltip("Force applied when jumping."), Range(0f, 50f)]
        private float jumpForce = 10f;

        [SerializeField, Tooltip("Layers considered as ground.")]
        private LayerMask groundLayer;

        [SerializeField, Tooltip("Offset and radius for ground check.")]
        private Vector2 groundCheckOffset = new Vector2(0f, -0.5f);

        [SerializeField, Tooltip("Radius for ground check.")]
        private float groundCheckRadius = 0.2f;

        #endregion

        #region Private Fields

        private Rigidbody2D _rb;
        private Vector2 _moveInput;
        private bool _isRunning;
        private bool _isGrounded;
        private bool _jumpRequested;

        #endregion

        #region Public Properties

        /// <summary>
        /// True when the player is providing movement input.
        /// </summary>
        public bool IsMoving { get; private set; }

        /// <summary>
        /// Current movement speed taking running into account.
        /// </summary>
        public float CurrentSpeed => walkSpeed * (_isRunning ? runSpeed : 1f);

        #endregion

        #region Unity Callbacks

        /// <summary>
        /// Unity callback invoked when the script instance is being loaded.
        /// Initializes the Rigidbody2D component and sets interpolation mode based on inspector settings.
        /// </summary>
        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            if (_rb == null)
            {
                Debug.LogError("PlayerController requires a Rigidbody2D component.");
                enabled = false;
                return;
            }

            _rb.interpolation = useInterpolation
                ? RigidbodyInterpolation2D.Interpolate
                : RigidbodyInterpolation2D.None;
        }

        /// <summary>
        /// Unity callback invoked at a fixed time interval, used for physics updates.
        /// Applies horizontal movement to the Rigidbody2D based on input and current speed.
        /// </summary>
        private void FixedUpdate()
        {
            CheckGrounded();

            float horizontal = _moveInput.x;
            if (!Mathf.Approximately(horizontal, 0f))
            {
                float movement = Mathf.Sign(horizontal) * Mathf.Abs(horizontal) * CurrentSpeed * Time.fixedDeltaTime;
                Vector2 newPosition = _rb.position + new Vector2(movement, 0f);
                _rb.MovePosition(newPosition);
            }

            if (_jumpRequested)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
                _jumpRequested = false;
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

        /// <summary>
        /// Checks if the player is currently grounded using an overlap circle.
        /// </summary>
        private void CheckGrounded()
        {
            Vector2 checkPosition = (Vector2)transform.position + groundCheckOffset;
            _isGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundLayer);
        }


#if UNITY_EDITOR
        private void OnValidate()
        {
            walkSpeed = Mathf.Max(0f, walkSpeed);
            runSpeed = Mathf.Clamp(runSpeed, 1f, 10f);
        }
        #endif

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
            if (context.performed && _isGrounded)
            {
                _jumpRequested = true;
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
}