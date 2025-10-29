using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Manages the lifecycle of a Unity Multiplayer Session, specifically configured
/// for the Relay architecture.
/// </summary>
public class SessionManager : NetworkBehaviour
{

    [SerializeField] int maxPlayers = 4;
    const string playerNamePropertyKey = "PlayerName";

    bool servicesInitialized = false;
    bool isSigningIn = false;

    public static SessionManager Instance { get; private set; }

    /// <summary>
    /// The currently active session. We use ISession, which is the specific
    /// interface required for sessions using the Relay service.
    /// </summary>
    private ISession activeSession;
    public ISession ActiveSession
    { 
        get => activeSession;
        set
        { 
            activeSession = value;
            Debug.Log($"Active session set. Session Code: {activeSession?.Code}");
        }
    }

    private List<CharacterType> allCharacters = new List<CharacterType>();
    private HashSet<CharacterType> assignedCharactersSet = new HashSet<CharacterType>();
    private Dictionary<ulong, CharacterType> playerCharacterMap = new Dictionary<ulong, CharacterType>();
    private Dictionary<ulong, string> authIdByClientId = new Dictionary<ulong, string>();

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

    private IEnumerator WaitForNetworkManager()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);
        RegisterNetworkEvents();
    }

    private void RegisterNetworkEvents()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;

        Debug.Log("Network events registered successfully.");
    }

    private void OnServerStarted()
    {
        Debug.Log("Server started successfully.");
    }

    private void OnServerStopped(bool wasHost)
    {
        Debug.LogError($"Server stopped! Was host: {wasHost}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.LogError($"Client {clientId} disconnected. Server active: {NetworkManager.Singleton.IsServer}");

        if (IsServer)
        {
            ReleaseCharacter(clientId);
            playerCharacterMap.Remove(clientId);
        }
    }

    async void Start()
    {
        StartCoroutine(WaitForNetworkManager());
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.LogLevel = LogLevel.Developer;
            Debug.Log("NetworkManager log level set to Developer");
        }
        else
        {
            Debug.LogWarning("NetworkManager.Singleton no estaba disponible en Start().");
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

    private void OnClientConnectedCallback(ulong clientId)
    {
        Debug.Log($"OnClientConnectedCallback: {clientId}");
        if (!IsServer) return;

        if (!TryGetAssignedCharacter(clientId, out _))
            AssignCharacterToClient(clientId);
    }

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

    public async Task JoinSessionByCodeAsync(string sessionCode)
    {
        ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        Debug.Log($"Joined session with name: {ActiveSession.Name}, Code: {ActiveSession.Code}");
    }

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

    public async Task KickPlayerAsync(string playerId)
    {
        if (ActiveSession == null) return;
        if (!ActiveSession.IsHost) return;
        await ActiveSession.AsHost().RemovePlayerAsync(playerId);
    }

    public async Task<IList<ISessionInfo>> QuerySessionsAsync()
    {
        if (!servicesInitialized) await InitializeServicesAndSignInAsync();
        var sessionQueryOptions = new QuerySessionsOptions();
        var results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);
        return results.Sessions;
    }

    async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
    {
        var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
        var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
        return new Dictionary<string, PlayerProperty> { { playerNamePropertyKey, playerNameProperty } };
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestAssignCharacterServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong requesterClientId = rpcParams.Receive.SenderClientId;
        AssignCharacterToClient(requesterClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterAuthIdServerRpc(string authId, ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;
        var clientId = rpcParams.Receive.SenderClientId;
        authIdByClientId[clientId] = authId;
        Debug.Log($"AuthId registrado para client {clientId}: {authId}");
    }

    private void AssignCharacterToClient(ulong clientId)
    {
        if (!IsServer) return;

        var playerState = GetPlayerStateForClient(clientId);
        if (playerState == null)
        {
            Debug.LogWarning($"AssignCharacterToClient: PlayerState no encontrado para clientId {clientId}");
            return;
        }

        if (playerCharacterMap.ContainsKey(clientId))
        {
            Debug.Log($"Cliente {clientId} ya tiene personaje asignado.");
            return;
        }

        CharacterType assigned = GetUniqueCharacter();
        playerCharacterMap[clientId] = assigned;

        playerState.InitializeClientRpc($"Player_{clientId}", (int)assigned);
        Debug.Log($"Servidor inicializó PlayerState del cliente {clientId} con personaje {assigned}");
    }

    private CharacterType GetUniqueCharacter()
    {
        var availableCharacters = allCharacters
        .Except(assignedCharactersSet)
        .Where(c => c != CharacterType.None)
        .ToList();

        if (availableCharacters.Count == 0)
        {
            Debug.LogWarning("No hay personajes disponibles, se reasignará un personaje duplicado.");

            var fallbackCharacters = allCharacters
            .Where(c => c != CharacterType.None)
            .ToList();
            return fallbackCharacters[UnityEngine.Random.Range(0, fallbackCharacters.Count)];
        }

        var assignedCharacter = availableCharacters[UnityEngine.Random.Range(0, availableCharacters.Count)];
        assignedCharactersSet.Add(assignedCharacter);
        return assignedCharacter;
    }

    public void ReleaseCharacter(ulong clientId)
    {
        if (playerCharacterMap.TryGetValue(clientId, out var character))
        {
            assignedCharactersSet.Remove(character);
            playerCharacterMap.Remove(clientId);
        }
    }

    public PlayerState GetPlayerStateForClient(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return null;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var networkClient)) return null;
        var playerObject = networkClient.PlayerObject;
        if (playerObject == null) return null;
        return playerObject.GetComponent<PlayerState>();
    }

    public bool TryGetAssignedCharacter(ulong clientId, out CharacterType character)
    {
        return playerCharacterMap.TryGetValue(clientId, out character);
    }
}