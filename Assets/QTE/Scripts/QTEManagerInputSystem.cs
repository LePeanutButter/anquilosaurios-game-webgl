using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class QTEManagerInputSystem : MonoBehaviour
{
    [Header("UI")]
    public GameObject qteUI;         // Panel (desactivado al inicio)
    public TextMeshProUGUI qteText;            // TextMeshProUGUI (puedes usar TMPro si quieres)

    [Header("Input")]
    public InputActionAsset inputActionsAsset; // Arrastra QTEInputActions aquí (archivo .inputactions)
    private InputAction startAction;
    private InputAction submitAction;

    private bool canPress = false;
    private float qteStartTime;

    void Awake()
    {
        if (inputActionsAsset == null)
            Debug.LogError("Asigna QTEInputActions en el inspector (InputActionAsset).");

        // Busca las acciones dentro del asset: "QTE/Start" y "QTE/Submit"
        startAction = inputActionsAsset.FindActionMap("QTE")?.FindAction("Start");
        submitAction = inputActionsAsset.FindActionMap("QTE")?.FindAction("Submit");

        if (startAction == null || submitAction == null)
            Debug.LogError("No se encontraron las acciones Start/Submit en QTEInputActions.");
    }

    void OnEnable()
    {
        if (startAction != null) startAction.performed += OnStartPerformed;
        if (submitAction != null) submitAction.performed += OnSubmitPerformed;

        // Habilita todo el mapa QTE (o individ. las acciones)
        inputActionsAsset?.Enable();
    }

    void OnDisable()
    {
        if (startAction != null) startAction.performed -= OnStartPerformed;
        if (submitAction != null) submitAction.performed -= OnSubmitPerformed;
        inputActionsAsset?.Disable();
    }

    void Start()
    {
        if (qteUI != null) qteUI.SetActive(false);
    }

    // Callback si con StartAction quieres iniciar el QTE con una tecla (Space)
    private void OnStartPerformed(InputAction.CallbackContext ctx)
    {
        // Esto solo inicia el QTE si no está activo
        if (!canPress) StartQTE();
    }

    // Callback cuando se presiona Submit (E o mouse)
    private void OnSubmitPerformed(InputAction.CallbackContext ctx)
    {
        // Solo si el QTE está activo
        if (canPress) SubmitQTE();
    }

    public void StartQTE(string message = "¡Presiona E ahora!")
    {
        if (qteUI != null) qteUI.SetActive(true);
        if (qteText != null) qteText.text = message;

        canPress = true;
        qteStartTime = Time.time;
        Debug.Log("QTE iniciado: presiona Submit (E) ahora.");
    }

    // Llamable desde UI Button onClick (ya conectado en Start)
    public void SubmitQTE()
    {
        if (!canPress) return;

        canPress = false;
        float reaction = Time.time - qteStartTime;

        if (qteUI != null) qteUI.SetActive(false);

        Debug.Log($"¡QTE presionado! Tiempo de reacción: {reaction:F3} s");
        // Aquí pones la lógica de recompensa o notificación al servidor
    }
}
