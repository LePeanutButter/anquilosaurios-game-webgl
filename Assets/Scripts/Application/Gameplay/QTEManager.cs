using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;

/// <summary>
/// Manages the Quick Time Event (QTE) process, including starting the QTE, receiving input from players, and determining the outcome.
/// </summary>
public class QTEManager : MonoBehaviour
{
    #region Singleton

    /// <summary>
    /// Singleton instance of the QTEManager.
    /// </summary>
    public static QTEManager Instance { get; private set; }

    #endregion

    #region QTE Settings

    [Header("QTE Settings")]
    [SerializeField] private float qteDuration = 5f;
    [SerializeField] private float inputWindow = 2f;
    [SerializeField] private TextMeshProUGUI qteText;
    [SerializeField] private GameObject qtePanel;

    #endregion

    #region Input Actions

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    private InputAction qteAction;

    #endregion

    #region Private Fields

    private bool isActive;
    private bool inputSent;
    private float timer;

    #endregion

    #region Events

    /// <summary>
    /// Event triggered when the QTE is completed (either successfully or failed).
    /// </summary>
    public event Action<ulong> OnQTECompleted;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes the QTEManager instance and prepares input actions.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (qtePanel != null)
            qtePanel.SetActive(false);

        if (inputActionsAsset != null)
        {
            qteAction = inputActionsAsset.FindAction("QTE/PressE", true);
        }
        else
        {
            Debug.LogWarning("QTEManager: inputActionsAsset not assigned in the inspector.");
        }
    }

    /// <summary>
    /// Subscribes to the QTE input action when enabled.
    /// </summary>
    private void OnEnable()
    {
        if (qteAction != null)
        {
            qteAction.performed += OnQTEPressed;
            qteAction.Enable();
        }
    }

    /// <summary>
    /// Unsubscribes from the QTE input action when disabled.
    /// </summary>
    private void OnDisable()
    {
        if (qteAction != null)
        {
            qteAction.performed -= OnQTEPressed;
            qteAction.Disable();
        }
    }

    #endregion

    #region QTE Logic

    /// <summary>
    /// Starts the QTE event. Activates UI and begins the countdown for player input.
    /// </summary>
    public void StartQTE()
    {
        if (isActive) return;

        isActive = true;
        inputSent = false;
        timer = 0f;

        if (qtePanel != null) qtePanel.SetActive(true);
        if (qteText != null) qteText.text = "Presiona E";

        StartCoroutine(QTECountdown());
        Debug.Log("QTEManager: QTE started locally.");
    }

    /// <summary>
    /// Handles the countdown timer for the QTE event.
    /// Ends the QTE if the input window expires without any input.
    /// </summary>
    private IEnumerator QTECountdown()
    {
        while (isActive)
        {
            timer += Time.deltaTime;

            if (timer >= inputWindow && !inputSent)
            {
                isActive = false;
                if (qtePanel != null) qtePanel.SetActive(false);
                Debug.Log("QTEManager: Input window expired locally.");
            }

            yield return null;
        }
    }

    /// <summary>
    /// Called when the player presses the QTE input key (e.g., 'E').
    /// Sends the input to the server if it's the first valid input.
    /// </summary>
    /// <param name="context">The input action context.</param>
    private void OnQTEPressed(InputAction.CallbackContext context)
    {
        if (!isActive || inputSent) return;

        inputSent = true;

        Debug.Log("QTEManager: Button pressed. Sending input to server...");

        if (GameRoundManager.Instance != null)
        {
            try
            {
                GameRoundManager.Instance.ReportQTEPressServerRpc();
                Debug.Log("QTEManager: ReportQTEPressServerRpc() called.");
            }
            catch (Exception ex)
            {
                Debug.LogError("QTEManager: Error invoking ReportQTEPressServerRpc(): " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("QTEManager: GameRoundManager.Instance is null on this client.");
        }

        if (qtePanel != null) qtePanel.SetActive(false);
        isActive = false;
    }

    #endregion

    #region QTE End Logic

    /// <summary>
    /// Forces the end of the QTE, indicating the winner or a timeout. 
    /// Invokes the QTE completion event.
    /// </summary>
    /// <param name="winnerClientId">The client ID of the winner, or null if no one won.</param>
    public void ForceEndQTE(ulong? winnerClientId)
    {
        isActive = false;
        inputSent = false;

        if (qtePanel != null) qtePanel.SetActive(false);

        if (winnerClientId.HasValue)
        {
            var localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0ul;
            if (winnerClientId.Value == localId)
            {
                Debug.Log("QTEManager: You won! Immunity for " + qteDuration + " seconds.");
            }
            else
            {
                Debug.Log("QTEManager: Player " + winnerClientId.Value + " won.");
            }

            OnQTECompleted?.Invoke(winnerClientId.Value);
        }
        else
        {
            Debug.Log("QTEManager: No one pressed in time.");
        }

        StartCoroutine(ClearMessageAfterDelay(1.5f));
        Debug.Log("QTEManager: ForceEndQTE called. winner=" + (winnerClientId.HasValue ? winnerClientId.Value.ToString() : "none"));
    }

    /// <summary>
    /// Clears the QTE result message after a delay.
    /// </summary>
    /// <param name="delay">The delay in seconds before clearing the message.</param>
    /// <returns>IEnumerator for clearing the message after the specified delay.</returns>
    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (qteText != null) qteText.text = "";
    }

    #endregion
}
