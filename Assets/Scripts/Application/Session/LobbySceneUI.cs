using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class LobbySceneUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text sessionCodeText;
    public Button startGameButton;
    public Transform playerListParent;
    public GameObject playerCardPrefab;

    private readonly Dictionary<ulong, PlayerCardUI> playerCards = new();
    private readonly Dictionary<ulong, PlayerState> boundPlayerStates = new();

    private void Start()
    {
        SetupLobby();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }

        foreach (var kv in boundPlayerStates)
            UnbindPlayerState(kv.Key, kv.Value);

        playerCards.Clear();
        boundPlayerStates.Clear();
    }

    private void SetupLobby()
    {
        if (SessionManager.Instance?.ActiveSession != null)
            sessionCodeText.text = $"Código de sesión: {SessionManager.Instance.ActiveSession.Code}";
        else
            sessionCodeText.text = "Código de sesión no disponible";

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        startGameButton.gameObject.SetActive(isHost);
        startGameButton.onClick.RemoveAllListeners();
        startGameButton.onClick.AddListener(OnStartGameClicked);

        RefreshPlayerList();
    }

    private void RefreshPlayerList()
    {
        if (NetworkManager.Singleton == null) return;

        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        foreach (var kv in boundPlayerStates)
            UnbindPlayerState(kv.Key, kv.Value);

        playerCards.Clear();
        boundPlayerStates.Clear();

        var connectedClients = NetworkManager.Singleton.ConnectedClientsList;

        foreach (var client in connectedClients)
        {
            var clientId = client.ClientId;
            var playerObject = client.PlayerObject;

            if (playerObject == null)
            {
                Debug.LogWarning($"El PlayerObject del cliente {clientId} aún no está listo, se mostrará placeholder.");
                CreatePlayerCardPlaceholder(clientId);
                continue;
            }

            var playerState = playerObject.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogWarning($"No se encontró PlayerState en el cliente {clientId}");
                CreatePlayerCardPlaceholder(clientId);
                continue;
            }

            CreateAndBindCard(clientId, playerState);
        }
    }

    private void CreatePlayerCardPlaceholder(ulong clientId)
    {
        var cardGO = Instantiate(playerCardPrefab, playerListParent);
        if (cardGO.TryGetComponent(out PlayerCardUI cardUI))
        {
            cardUI.Setup($"Jugador {clientId}", null);
            playerCards[clientId] = cardUI;
        }
    }

    private void CreateAndBindCard(ulong clientId, PlayerState playerState)
    {
        var cardGO = Instantiate(playerCardPrefab, playerListParent);
        if (!cardGO.TryGetComponent(out PlayerCardUI cardUI))
        {
            Destroy(cardGO);
            return;
        }

        string playerName = playerState.PlayerName.Value.ToString();
        if (string.IsNullOrEmpty(playerName))
            playerName = $"Jugador {clientId}";

        CharacterType? assignedChar = playerState.Character.Value != (int)CharacterType.None
            ? (CharacterType?)playerState.Character.Value
            : null;

        cardUI.Setup(playerName, assignedChar);
        playerCards[clientId] = cardUI;

        // Suscribirse a cambios
        playerState.PlayerName.OnValueChanged += (prev, curr) =>
        {
            if (playerCards.TryGetValue(clientId, out var card))
            {
                string newName = string.IsNullOrEmpty(curr.ToString()) ? $"Jugador {clientId}" : curr.ToString();
                card.UpdateName(newName);
            }
        };

        playerState.Character.OnValueChanged += (prev, curr) =>
        {
            if (playerCards.TryGetValue(clientId, out var card))
            {
                var charToShow = curr != (int)CharacterType.None ? (CharacterType?)curr : null;
                card.UpdateCharacter(charToShow);
            }
        };

        boundPlayerStates[clientId] = playerState;
    }

    private void UnbindPlayerState(ulong clientId, PlayerState playerState)
    {
        if (playerState == null) return;

        try
        {
            playerState.PlayerName.OnValueChanged = null;
            playerState.Character.OnValueChanged = null;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Error al desuscribir PlayerState para client {clientId}: {e.Message}");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Cliente conectado: {clientId}");
        StartCoroutine(DelayedRefresh());
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Cliente desconectado: {clientId}");

        if (playerCards.TryGetValue(clientId, out var card))
        {
            Destroy(card.gameObject);
            playerCards.Remove(clientId);
        }

        if (boundPlayerStates.TryGetValue(clientId, out var ps))
        {
            UnbindPlayerState(clientId, ps);
            boundPlayerStates.Remove(clientId);
        }

        RefreshPlayerList();
    }

    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return null;
        RefreshPlayerList();
    }

    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("El host ha iniciado la partida.");

            if (SessionManager.Instance?.ActiveSession == null)
            {
                Debug.LogError("No hay sesión activa para cambiar de escena.");
                return;
            }

            var sceneManager = NetworkManager.Singleton.SceneManager;
            if (sceneManager != null)
            {
                sceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
            else
            {
                Debug.LogError("No se encontró SceneManager en NetworkManager.");
            }
        }
    }
}
