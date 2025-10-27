using Unity.Netcode;
using UnityEngine;
using TMPro; // si usas TMP; si no, cambia a UnityEngine.UI.Text

/// <summary>
/// Manager central del QTE. Debe estar en un solo GameObject en la escena (NetworkBehaviour).
/// El servidor llama StartQTEServerRpc() para lanzar el QTE.
/// Los clientes reciben StartQTEClientRpc() para mostrar UI.
/// Los jugadores envían Submit al servidor con SubmitQTEServerRpc().
/// </summary>
public class QTEMultiplayerManagerNetwork : NetworkBehaviour
{
    public static QTEMultiplayerManagerNetwork Instance { get; private set; }

    [Header("UI (Client)")]
    public GameObject qteUI;            // QTE_Panel (en la escena)
    public TextMeshProUGUI qteMessage;  // QTE_Message (TMP)
    public TextMeshProUGUI reactionText; // ReactionText (TMP)

    [Header("Settings")]
    public float qteDuration = 5f; // tiempo máximo del QTE

    // Estado en servidor
    private bool qteActive = false;
    private bool qteDone = false;
    private ulong winnerClientId = 0;
    private float qteStartTime = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    void Start()
    {
        if (qteUI != null) qteUI.SetActive(false);
        if (reactionText != null) reactionText.gameObject.SetActive(false);
    }

    // ================= SERVER API =================
    // Llamar desde servidor/host para iniciar QTE
    [ServerRpc(RequireOwnership = false)]
    public void StartQTEServerRpc(ServerRpcParams rpcParams = default)
    {
        // Solo servidor debe ejecutar esta lógica: (NGO ejecuta ServerRpc en servidor)
        if (!IsServer) return;

        qteDone = false;
        winnerClientId = 0;
        qteActive = true;
        qteStartTime = Time.time;

        // Notificar a clientes
        StartQTEClientRpc();
        // Programar timeout
        Invoke(nameof(OnQTETimeout), qteDuration);
    }

    // Cuando un cliente hace submit, llama a este ServerRpc (RequireOwnership=false permite a cualquier cliente llamarlo)
    [ServerRpc(RequireOwnership = false)]
    public void SubmitQTEServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        if (!qteActive || qteDone) return;

        // primer que llegue gana
        qteDone = true;
        winnerClientId = rpcParams.Receive.SenderClientId;
        qteActive = false;

        // Cancelar timeout e informar a todos
        CancelInvoke(nameof(OnQTETimeout));
        NotifyWinnerClientRpc(winnerClientId);
    }

    private void OnQTETimeout()
    {
        // Si se acaba el tiempo y nadie ganó
        qteActive = false;
        qteDone = true;
        winnerClientId = 0;
        NotifyWinnerClientRpc(0);
    }

    // ================= CLIENT RPCs =================
    [ClientRpc]
    private void StartQTEClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (qteUI != null) qteUI.SetActive(true);
        if (qteMessage != null) qteMessage.text = "¡Presiona tu tecla!";
        if (reactionText != null) reactionText.gameObject.SetActive(false);
    }

    [ClientRpc]
    private void NotifyWinnerClientRpc(ulong winnerId, ClientRpcParams clientRpcParams = default)
    {
        if (qteUI != null) qteUI.SetActive(false);
        if (reactionText == null || qteMessage == null) return;

        if (winnerId == 0)
        {
            qteMessage.text = "¡Nadie ganó!";
            reactionText.text = "¡Nadie ganó!";
        }
        else if (NetworkManager.Singleton.LocalClientId == winnerId)
        {
            qteMessage.text = "¡Ganaste!";
            reactionText.text = "¡Ganaste!";
            // Puedes llamar aquí la lógica de recompensa local (opcional)
        }
        else
        {
            qteMessage.text = "Perdiste";
            reactionText.text = "Perdiste";
        }

        reactionText.gameObject.SetActive(true);
        // opcional: ocultar reactionText después de un tiempo
        Invoke(nameof(HideReactionText), 1.5f);
    }

    private void HideReactionText()
    {
        if (reactionText != null) reactionText.gameObject.SetActive(false);
    }
}
