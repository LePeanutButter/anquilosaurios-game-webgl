using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Unity.Netcode;

public class QTEManager : MonoBehaviour
{
    public static QTEManager Instance { get; private set; }

    [Header("QTE Settings")]
    [SerializeField] private float qteDuration = 5f;
    [SerializeField] private float inputWindow = 2f;
    [SerializeField] private TextMeshProUGUI qteText;
    [SerializeField] private GameObject qtePanel;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActionsAsset;
    private InputAction qteAction;

    private bool isActive;
    private bool inputSent;
    private float timer;

    public event Action<ulong> OnQTECompleted;

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
            Debug.LogWarning("QTEManager: inputActionsAsset no asignado en el inspector.");
        }
    }

    private void OnEnable()
    {
        if (qteAction != null)
        {
            qteAction.performed += OnQTEPressed;
            qteAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (qteAction != null)
        {
            qteAction.performed -= OnQTEPressed;
            qteAction.Disable();
        }
    }

    public void StartQTE()
    {
        if (isActive) return;

        isActive = true;
        inputSent = false;
        timer = 0f;

        if (qtePanel != null) qtePanel.SetActive(true);
        if (qteText != null) qteText.text = "¡Presiona <b>E</b> rápidamente!";

        StartCoroutine(QTECountdown());
        Debug.Log("QTEManager: QTE started locally.");
    }

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

    private void OnQTEPressed(InputAction.CallbackContext context)
    {
        if (!isActive || inputSent) return;

        inputSent = true;

        if (qteText != null) qteText.text = "Enviando...";

        if (GameRoundManager.Instance != null)
        {
            try
            {
                GameRoundManager.Instance.ReportQTEPressServerRpc();
                Debug.Log("QTEManager: ReportQTEPressServerRpc() called.");
            }
            catch (Exception ex)
            {
                Debug.LogError("QTEManager: Error al invocar ReportQTEPressServerRpc(): " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("QTEManager: GameRoundManager.Instance es null en este cliente.");
        }

        if (qtePanel != null) qtePanel.SetActive(false);
        isActive = false;
    }

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
                if (qteText != null) qteText.text = $"¡Ganaste! Inmunidad por {qteDuration} s";
            }
            else
            {
                if (qteText != null) qteText.text = $"Jugador {winnerClientId.Value} ganó";
            }

            OnQTECompleted?.Invoke(winnerClientId.Value);
        }
        else
        {
            if (qteText != null) qteText.text = "Nadie presionó a tiempo";
        }

        StartCoroutine(ClearMessageAfterDelay(1.5f));
        Debug.Log("QTEManager: ForceEndQTE called. winner=" + (winnerClientId.HasValue ? winnerClientId.Value.ToString() : "none"));
    }

    private IEnumerator ClearMessageAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (qteText != null) qteText.text = "";
    }
}
