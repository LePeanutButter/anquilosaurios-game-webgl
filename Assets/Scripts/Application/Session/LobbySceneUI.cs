using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the UI of the Lobby scene, including session code display, player list management,
/// and starting the game for the host.
/// </summary>
public class LobbySceneUI : MonoBehaviour
{
    #region Public Fields

    [Header("UI References")]
    public TMP_Text sessionCodeText;
    public Button startGameButton;
    public Transform playerListParent;
    public GameObject playerCardPrefab;

    #endregion

    #region Private Fields

    private readonly Dictionary<ulong, PlayerCardUI> playerCards = new();
    private readonly Dictionary<ulong, PlayerState> boundPlayerStates = new();

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes the lobby UI and registers network events.
    /// </summary>
    private void Start()
    {
        SetupLobby();

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
    }

    /// <summary>
    /// Cleans up network event listeners and unbinds all player states when the object is destroyed.
    /// </summary>
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

    #endregion

    #region Lobby UI Setup

    /// <summary>
    /// Sets up the lobby UI, including session code, start game button visibility, and player list.
    /// </summary>
    private void SetupLobby()
    {
        if (SessionManager.Instance?.ActiveSession != null)
            sessionCodeText.text = $"Codigo de sesion: {SessionManager.Instance.ActiveSession.Code}";
        else
            sessionCodeText.text = "Codigo de sesion no disponible";

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        startGameButton.gameObject.SetActive(isHost);
        startGameButton.onClick.RemoveAllListeners();
        startGameButton.onClick.AddListener(OnStartGameClicked);

        RefreshPlayerList();
    }

    #endregion

    #region Player List Management

    /// <summary>
    /// Clears and rebuilds the player list UI based on currently connected clients.
    /// </summary>
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
                Debug.LogWarning($"Client {clientId}'s PlayerObject not ready, showing placeholder.");
                CreatePlayerCardPlaceholder(clientId);
                continue;
            }

            var playerState = playerObject.GetComponent<PlayerState>();
            if (playerState == null)
            {
                Debug.LogWarning($"PlayerState not found for client {clientId}");
                CreatePlayerCardPlaceholder(clientId);
                continue;
            }

            CreateAndBindCard(clientId, playerState);
        }
    }

    /// <summary>
    /// Creates a placeholder player card for clients whose PlayerState is not yet ready.
    /// </summary>
    /// <param name="clientId">The client ID for which to create the placeholder.</param>
    private void CreatePlayerCardPlaceholder(ulong clientId)
    {
        var cardGO = Instantiate(playerCardPrefab, playerListParent);
        if (cardGO.TryGetComponent(out PlayerCardUI cardUI))
        {
            cardUI.Setup($"Jugador {clientId}", null);
            playerCards[clientId] = cardUI;
        }
    }

    /// <summary>
    /// Creates a player card UI for a client and binds it to the corresponding PlayerState.
    /// </summary>
    /// <param name="clientId">The client ID of the player.</param>
    /// <param name="playerState">The PlayerState object to bind.</param>
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

    /// <summary>
    /// Unbinds the player state from its UI listeners.
    /// </summary>
    /// <param name="clientId">The client ID of the player to unbind.</param>
    /// <param name="playerState">The PlayerState to unbind.</param>
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
            Debug.LogWarning($"Error unsubscribing PlayerState for client {clientId}: {e.Message}");
        }
    }

    #endregion

    #region Network Event Handlers

    /// <summary>
    /// Callback invoked when a client connects to the session.
    /// </summary>
    /// <param name="clientId">The client ID that connected.</param>
    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
        StartCoroutine(DelayedRefresh());
    }

    /// <summary>
    /// Callback invoked when a client disconnects from the session.
    /// Removes their UI card and unbinds their state.
    /// </summary>
    /// <param name="clientId">The client ID that disconnected.</param>
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");

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

    /// <summary>
    /// Waits a single frame before refreshing the player list to ensure network objects are initialized.
    /// </summary>
    /// <returns>Enumerator for coroutine.</returns>
    private System.Collections.IEnumerator DelayedRefresh()
    {
        yield return null;
        RefreshPlayerList();
    }

    #endregion

    #region Game Start Logic

    /// <summary>
    /// Starts the game if the local player is the host.
    /// Loads the "RoundInterface" scene through the SceneTransitionManager.
    /// </summary>
    private void OnStartGameClicked()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            Debug.Log("Host has started the game.");

            if (SessionManager.Instance?.ActiveSession == null)
            {
                Debug.LogError("No active session to switch scenes.");
                return;
            }

            if (NetworkManager.Singleton.SceneManager != null)
            {
                SceneTransitionManager.Instance.LoadSceneWithTransition("RoundInterface");
            }
            else
            {
                Debug.LogError("NetworkManager SceneManager not found.");
            }
        }
    }

    #endregion
}
