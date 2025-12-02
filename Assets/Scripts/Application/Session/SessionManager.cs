using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Manages the lifecycle of a Unity Multiplayer Session,
/// specifically configured for the Relay architecture.
/// Handles session creation, joining, leaving, player character assignment, 
/// and interaction with Unity Services (Authentication and Multiplayer).
/// </summary>
public class SessionManager : NetworkBehaviour
{
    #region Serializable Fields

    [SerializeField]
    private int maxPlayers = 4;

    #endregion

    #region Private Fields

    private const string playerNamePropertyKey = "PlayerName";
    private bool servicesInitialized = false;
    private bool isSigningIn = false;
    private ISession activeSession;
    private List<CharacterType> allCharacters = new();
    private HashSet<CharacterType> assignedCharactersSet = new();
    private Dictionary<ulong, CharacterType> playerCharacterMap = new();
    private Dictionary<ulong, string> authIdByClientId = new();

    #endregion

    #region Public Properties

    /// <summary>
    /// Singleton instance of SessionManager.
    /// </summary>
    public static SessionManager Instance { get; private set; }

    /// <summary>
    /// The currently active session (Relay-compatible).
    /// Setting the session logs the session code for debugging purposes.
    /// </summary>
    public ISession ActiveSession
    { 
        get => activeSession;
        set
        { 
            activeSession = value;
            Debug.Log($"Active session set. Session Code: {activeSession?.Code}");
        }
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes singleton instance and the character list.
    /// </summary>
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        allCharacters = Enum.GetValues(typeof(CharacterType)).Cast<CharacterType>().ToList();
    }

    /// <summary>
    /// Starts initialization coroutine and Unity services sign-in.
    /// </summary>
    private async void Start()
    {
        StartCoroutine(WaitForNetworkManager());
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.LogLevel = LogLevel.Developer;
            Debug.Log("NetworkManager log level set to Developer");
        }
        else
        {
            Debug.LogWarning("NetworkManager.Singleton not available in Start().");
        }

        try
        {
            await InitializeServicesAndSignInAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"Start: Failed to initialize services or sign in: {e.Message}");
        }
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Waits until the NetworkManager is ready, then registers network events.
    /// </summary>
    private IEnumerator WaitForNetworkManager()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        RegisterNetworkEvents();
    }

    /// <summary>
    /// Registers NetworkManager event callbacks for server and client lifecycle events.
    /// </summary>
    private void RegisterNetworkEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        Debug.Log("Network events registered successfully.");
    }

    /// <summary>
    /// Called when the server has successfully started.
    /// Logs a confirmation message to the console.
    /// </summary>
    private void OnServerStarted()
    {
        Debug.Log("Server started successfully.");
    }

    /// <summary>
    /// Called when the server has stopped.
    /// Logs an error message indicating whether the server was acting as host.
    /// </summary>
    /// <param name="wasHost">Indicates if the server was running as host when it stopped.</param>
    private void OnServerStopped(bool wasHost)
    {
        Debug.LogError($"Server stopped! Was host: {wasHost}");
    }

    /// <summary>
    /// Handles client disconnection events.
    /// Logs the disconnection and, if on the server, releases the character assigned to the client
    /// and removes the client from the character map.
    /// </summary>
    /// <param name="clientId">The unique identifier of the disconnected client.</param>
    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogError($"Client {clientId} disconnected. Server active: {NetworkManager.Singleton.IsServer}");

        if (IsServer)
        {
            ReleaseCharacter(clientId);
            playerCharacterMap.Remove(clientId);
        }
    }

    /// <summary>
    /// Assigns a unique available character to a client.
    /// Uses the authenticated user name from WebAuthReceiver if available.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client to assign a character to.</param>
    private void AssignCharacterToClient(ulong clientId)
    {
        if (!IsServer) return;

        var playerState = GetPlayerStateForClient(clientId);
        if (playerState == null)
        {
            Debug.LogWarning($"AssignCharacterToClient: PlayerState not found for clientId {clientId}");
            return;
        }

        if (playerCharacterMap.ContainsKey(clientId))
        {
            Debug.Log($"Client {clientId} already has an assigned character.");
            return;
        }

        CharacterType assigned = GetUniqueCharacter();
        playerCharacterMap[clientId] = assigned;

        // CAMBIO: Inicializar con nombre temporal
        string tempName = $"Player_{clientId}";
        playerState.InitializeDataServer(tempName, (int)assigned);
    
        Debug.Log($"[SessionManager] Cliente {clientId} asignado temporalmente como '{tempName}' | Personaje: {assigned}");
        Debug.Log($"[SessionManager] Esperando nombre autenticado del cliente...");
    }

    // NUEVO: ServerRpc para que los clientes envíen su nombre
    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerNameServerRpc(string playerName, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong clientId = rpcParams.Receive.SenderClientId;
    
        var playerState = GetPlayerStateForClient(clientId);
        if (playerState == null)
        {
            Debug.LogError($"[SessionManager] No se encontró PlayerState para cliente {clientId}");
            return;
        }

        // Actualizar solo el nombre, mantener el personaje
        playerState.PlayerName.Value = playerName;
    
        Debug.Log($"[SessionManager] Nombre actualizado para cliente {clientId}: '{playerName}'");
    }

    /// <summary>
    /// Returns a unique character not currently assigned. If none available, selects a random fallback.
    /// </summary>
    private CharacterType GetUniqueCharacter()
    {
        var availableCharacters = allCharacters
        .Except(assignedCharactersSet)
        .Where(c => c != CharacterType.None)
        .ToList();

        if (availableCharacters.Count == 0)
        {
            Debug.LogWarning("No available characters left, reassigning a duplicate.");

            var fallbackCharacters = allCharacters
            .Where(c => c != CharacterType.None)
            .ToList();
            return fallbackCharacters[UnityEngine.Random.Range(0, fallbackCharacters.Count)];
        }

        var assignedCharacter = availableCharacters[UnityEngine.Random.Range(0, availableCharacters.Count)];
        assignedCharactersSet.Add(assignedCharacter);
        return assignedCharacter;
    }

    #endregion

    #region Public Methods - Unity Services

    /// <summary>
    /// Initializes Unity Services and signs in anonymously.
    /// </summary>
    public async Task InitializeServicesAndSignInAsync()
    {
        if (servicesInitialized || isSigningIn) return;
        try
        {
            isSigningIn = true;
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            servicesInitialized = true;
            Debug.Log($"Signed in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        finally
        {
            isSigningIn = false;
        }
    }

    /// <summary>
    /// Creates a new multiplayer session using Relay.
    /// </summary>
    public async Task CreateSessionAsync()
    {
        if (!servicesInitialized)
            await InitializeServicesAndSignInAsync();

        var playerProperties = await GetPlayerProperties();

        var options = new SessionOptions
        {
            MaxPlayers = maxPlayers,
            IsLocked = false,
            IsPrivate = false,
            PlayerProperties = playerProperties
        }.WithRelayNetwork();

        ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);

        Debug.Log($"Session created with name: {ActiveSession.Name}, Code: {ActiveSession.Code}");
    }

    /// <summary>
    /// Joins an existing session by session code.
    /// </summary>
    /// <param name="sessionCode">The unique code used to identify and join the target multiplayer session.</param>
    public async Task JoinSessionByCodeAsync(string sessionCode)
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        Debug.Log($"Joined session with name: {ActiveSession.Name}, Code: {ActiveSession.Code}");
    }

    /// <summary>
    /// Leaves the currently active session.
    /// </summary>
    public async Task LeaveSessionAsync()
    {
        if (ActiveSession == null) return;
        try
        {
            await ActiveSession.LeaveAsync();
        }
        catch (Exception e)
        {
            Debug.LogError($"LeaveSessionAsync failed: {e.Message}");
        }
        ActiveSession = null;
    }

    /// <summary>
    /// Kicks a player from the session (host-only operation).
    /// </summary>
    /// <param name="playerId">The unique identifier of the player to be removed from the session.</param>
    public async Task KickPlayerAsync(string playerId)
    {
        if (ActiveSession == null) return;
        if (!ActiveSession.IsHost) return;
        await ActiveSession.AsHost().RemovePlayerAsync(playerId);
    }

    /// <summary>
    /// Queries all available sessions.
    /// </summary>
    public async Task<IList<ISessionInfo>> QuerySessionsAsync()
    {
        if (!servicesInitialized) await InitializeServicesAndSignInAsync();
        var sessionQueryOptions = new QuerySessionsOptions();
        var results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);
        return results.Sessions;
    }

    /// <summary>
    /// Retrieves the current player's properties, such as player name.
    /// </summary>
    async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
    {
        var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        return new Dictionary<string, PlayerProperty> { { playerNamePropertyKey, playerNameProperty } };
    }

    #endregion

    #region Public Methods - Player Character Management

    /// <summary>
    /// Releases the character assigned to a client.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client whose character should be released.</param>
    public void ReleaseCharacter(ulong clientId)
    {
        if (playerCharacterMap.TryGetValue(clientId, out var character))
        {
            assignedCharactersSet.Remove(character);
            playerCharacterMap.Remove(clientId);
        }
    }

    /// <summary>
    /// Retrieves the PlayerState component for a given clientId.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client whose PlayerState component is being retrieved.</param>
    public PlayerState GetPlayerStateForClient(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient)) return null;
        var playerObject = networkClient.PlayerObject;
        if (playerObject == null) return null;
        return playerObject.GetComponent<PlayerState>();
    }

    /// <summary>
    /// Attempts to get the character assigned to a client.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client whose assigned character is being retrieved.</param>
    /// <param name="character">When this method returns, contains the character assigned to the client, if one exists.</param>
    /// <returns>True if a character was assigned to the client; otherwise, false.</returns>
    public bool TryGetAssignedCharacter(ulong clientId, out CharacterType character)
    {
        return playerCharacterMap.TryGetValue(clientId, out character);
    }

    #endregion

    #region Server RPCs

    /// <summary>
    /// Requests the server to assign a character to the client.
    /// </summary>
    /// <param name="rpcParams">Parameters containing metadata about the ServerRpc call, including the sender's client ID.</param>
    [ServerRpc(RequireOwnership = false)]
    public void RequestAssignCharacterServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        AssignCharacterToClient(requesterClientId);
    }

    /// <summary>
    /// Registers a client's authentication ID on the server.
    /// </summary>
    /// <param name="authId">The authentication ID to associate with the client.</param>
    /// <param name="rpcParams">Parameters containing metadata about the ServerRpc call, including the sender's client ID.</param>
    [ServerRpc(RequireOwnership = false)]
    public void RegisterAuthIdServerRpc(string authId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        var clientId = rpcParams.Receive.SenderClientId;
        authIdByClientId[clientId] = authId;
        Debug.Log($"AuthId registered for client {clientId}: {authId}");
    }

    #endregion

    #region Client Connection Callback

    /// <summary>
    /// Callback invoked when a client connects to the server.
    /// If running on the server, assigns a character to the client if one is not already assigned.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client that has connected.</param>
    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log($"OnClientConnectedCallback: {clientId}");
        if (!IsServer) return;

        if (!TryGetAssignedCharacter(clientId, out _))
            AssignCharacterToClient(clientId);
    }

    #endregion

    // ============ AGREGAR ESTOS 2 MÉTODOS AQUÍ ============

    /// <summary>
    /// Coroutine que espera a que WebAuthReceiver tenga datos antes de asignar el nombre
    /// </summary>
    private IEnumerator WaitForAuthAndAssignName(ulong clientId, CharacterType assigned, PlayerState playerState)
    {
        Debug.Log($"[SessionManager] Esperando datos de autenticación para cliente {clientId}...");
        
        WebAuthReceiver webAuthReceiver = null;
        float searchTime = 0f;
        float maxSearchTime = 2f;
        
        // Buscar WebAuthReceiver (puede tardar en crearse)
        while (webAuthReceiver == null && searchTime < maxSearchTime)
        {
            webAuthReceiver = FindObjectOfType<WebAuthReceiver>();
            if (webAuthReceiver == null)
            {
                yield return new WaitForSeconds(0.1f);
                searchTime += 0.1f;
            }
        }
        
        if (webAuthReceiver == null)
        {
            Debug.LogWarning($"[SessionManager] WebAuthReceiver no encontrado después de {searchTime:F1}s");
            AssignFallbackName(clientId, assigned, playerState);
            yield break;
        }
        
        Debug.Log($"[SessionManager] WebAuthReceiver encontrado, esperando autenticación...");
        
        // Esperar a que tenga datos de autenticación (máximo 5 segundos)
        float waitTime = 0f;
        float maxWaitTime = 5f;
        
        while (!webAuthReceiver.IsAuthenticated && waitTime < maxWaitTime)
        {
            yield return new WaitForSeconds(0.1f);
            waitTime += 0.1f;
            
            if (waitTime % 1f < 0.15f) // Log cada segundo
            {
                Debug.Log($"[SessionManager] Esperando autenticación... ({waitTime:F1}s / {maxWaitTime}s)");
            }
        }
        
        // Asignar nombre
        string playerName;
        
        if (webAuthReceiver.IsAuthenticated && 
            !string.IsNullOrEmpty(webAuthReceiver.UserName) && 
            webAuthReceiver.UserName != "Player")
        {
            playerName = webAuthReceiver.UserName;
            Debug.Log($"[SessionManager] Usando nombre autenticado: {playerName}");
        }
        else
        {
            playerName = $"Player_{clientId}";
            Debug.Log($"[SessionManager] Timeout, usando fallback: {playerName}");
        }
        
        // Inicializar PlayerState con el nombre
        playerState.InitializeDataServer(playerName, (int)assigned);
        Debug.Log($"[SessionManager] PlayerState inicializado - Cliente {clientId} | Nombre: '{playerName}' | Personaje: {assigned}");
    }

    /// <summary>
    /// Asigna un nombre por defecto cuando no hay datos de autenticación
    /// </summary>
    private void AssignFallbackName(ulong clientId, CharacterType assigned, PlayerState playerState)
    {
        string playerName = $"Player_{clientId}";
        playerState.InitializeDataServer(playerName, (int)assigned);
        Debug.Log($"[SessionManager] Fallback aplicado - Cliente {clientId} | Nombre: '{playerName}' | Personaje: {assigned}");
    }
}