using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; // si vas a usar TextMeshPro (si no, comenta esta línea y usa UnityEngine.UI.Text)

public class QTESimulatedMultiplayerInputSystem : MonoBehaviour
{
    [System.Serializable]
    public class Player
    {
        public string playerName = "Player";
        // NOTE: usamos Input Actions, pero guardamos la tecla para mostrarla en la UI si quieres
        public string actionName;         // nombre de la acción en el InputActionAsset (ej: "Player1_Submit")
        public TextMeshProUGUI playerText; // o UnityEngine.UI.Text si no usas TMP
    }

    [Header("Input")]
    public InputActionAsset inputActionsAsset; // arrastra QTEInputActions aquí
    [Header("UI")]
    public GameObject qteUI;                   // panel que se muestra durante el QTE
    public TextMeshProUGUI qteMessage;         // texto central "¡Presionen su tecla!"
    public TextMeshProUGUI reactionText;       // texto para mostrar resultado ("Player X ganó")
    [Header("Players (mapAction names)")]
    public List<Player> players = new List<Player>();

    // Estado interno
    private bool qteActive = false;
    private bool qteDone = false;
    private float qteStartTime = 0f;

    // Mapea acciones del asset a InputAction variables para suscribirse
    private Dictionary<string, InputAction> actionMap = new Dictionary<string, InputAction>();

    void Awake()
    {
        if (inputActionsAsset == null)
        {
            Debug.LogError("Asigna QTEInputActions en inputActionsAsset del inspector.");
            return;
        }

        // Prepara las acciones: Start + todas las player submit según 'players' list
        // Start (opcional, para debug/local)
        var startAct = inputActionsAsset.FindActionMap("QTE")?.FindAction("Start");

        // Buscar y registrar las acciones de cada jugador
        foreach (var p in players)
        {
            if (string.IsNullOrEmpty(p.actionName)) continue;
            var act = inputActionsAsset.FindActionMap("QTE")?.FindAction(p.actionName);
            if (act != null)
            {
                actionMap[p.actionName] = act;
                // subscribe
                act.performed += ctx => OnPlayerSubmit(p);
            }
            else
            {
                Debug.LogWarning($"No se encontró la acción '{p.actionName}' en QTEInputActions.");
            }
        }

        // Enable asset
        inputActionsAsset.Enable();
    }

    void Start()
    {
        if (qteUI != null) qteUI.SetActive(false);
        if (reactionText != null) reactionText.gameObject.SetActive(false);

        // Limpia textos de players (UI)
        foreach (var p in players)
            if (p.playerText != null) p.playerText.text = "";
    }

    void OnDestroy()
    {
        // limpiar suscripciones
        foreach (var kv in actionMap)
        {
            kv.Value.performed -= ctx => { }; // no elimina un lambda fácilmente; desactivamos el asset completo
        }
        if (inputActionsAsset != null) inputActionsAsset.Disable();
    }

    // Llamado al recibir la acción de algun player (callback compartido)
    private void OnPlayerSubmit(Player p)
    {
        if (!qteActive || qteDone) return;

        qteDone = true;
        qteActive = false;
        float reaction = Time.time - qteStartTime;
        Debug.Log($"{p.playerName} ganó el QTE! Tiempo de reacción: {reaction:F3} s");

        // UI: actualizar paneles
        if (qteUI != null) qteUI.SetActive(false);
        if (reactionText != null)
        {
            reactionText.gameObject.SetActive(true);
            reactionText.text = $"{p.playerName} ganó! ({reaction:F3} s)";
            StartCoroutine(HideReactionTextAfterDelay(1.5f));
        }

        // Mostrar "Ganó / Perdió" por jugador
        foreach (var player in players)
        {
            if (player.playerText == null) continue;
            if (player == p)
                player.playerText.text = "¡Ganó!";
            else
                player.playerText.text = "Perdió";
        }

        // Aquí podrías llamar la lógica de recompensa, o enviar resultado a server, etc.
    }

    private IEnumerator HideReactionTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (reactionText != null) reactionText.gameObject.SetActive(false);
    }

    // Método público para iniciar el QTE (desde UI, desde Tester, o desde red)
    public void StartQTE()
    {
        qteActive = true;
        qteDone = false;
        qteStartTime = Time.time;

        if (qteUI != null) qteUI.SetActive(true);
        if (qteMessage != null) qteMessage.text = "¡Presionen su tecla!";
        if (reactionText != null) reactionText.gameObject.SetActive(false);

        // limpiar resultados previos
        foreach (var p in players)
            if (p.playerText != null) p.playerText.text = "";

        Debug.Log("QTE iniciado: presiona tu tecla asignada.");
    }
}
