using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Controls the entire game round lifecycle:
/// - Generates map (networked)
/// - Spawns players
/// - Starts and ends the match
/// - Spawns lethal obstacles periodically
/// Designed for use with Netcode for GameObjects.
/// </summary>
public class GameRoundManager : NetworkBehaviour
{
    #region Public Fields

    public static GameRoundManager Instance { get; private set; }
    public NetworkVariable<bool> isRoundInitialized = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    #endregion

    #region Inspector Fields

    [System.Serializable]
    public struct PlayerPrefabEntry
    {
        public CharacterType Type;
        public GameObject Prefab;
    }

    [Header("Prefabs and Scene")]
    [SerializeField] private GameObject lethalPrefab;
    [SerializeField] private GameObject[] mapPrefabs;

    [Header("Match Settings")]
    [SerializeField] private float matchDurationSeconds = 30f;
    [SerializeField] private float spawnStartDelaySeconds = 2f;
    [SerializeField] private float initialSpawnInterval = 1f;
    [SerializeField] private float minSpawnInterval = 0.3f;
    [SerializeField] private int allowedSpawnWidthPixels = 1850;
    [SerializeField] private int spawnAboveScreenPixels = 50;

    [Header("Player Settings")]
    [SerializeField] private List<PlayerPrefabEntry> playerPrefabsMap;
    private Dictionary<CharacterType, GameObject> playerPrefabsDict = new();

    [Header("QTE Settings")]
    [SerializeField] private float qteInputWindow = 2f;
    [SerializeField] private float qteTimeScale = 0.2f;

    [Header("HUD")]
    [SerializeField] private TMP_Text timeTextHud;
    [SerializeField] private GameObject playerHealthHudPrefab;
    [SerializeField] private Transform healthHudParent;

    #endregion

    #region Private Fields

    private Coroutine spawnRoutine;
    private Coroutine qteRoutine;
    private Dictionary<ulong, PlayerPresenter> playerPresenters = new();
    private bool qteActiveOnServer = false;
    private ulong? qteWinner = null;
    private float qteStartTimeServer = 0f;
    private float originalTimeScale = 1f;
    private NetworkVariable<float> remainingTime = new(writePerm: NetworkVariableWritePermission.Server);

    #endregion

    #region Initialization & Network Spawn

    /// <summary>
    /// Ensures singleton instance and initializes player prefabs dictionary.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializePlayerPrefabsDict();
    }

    /// <summary>
    /// Initializes the dictionary mapping CharacterType to corresponding prefab.
    /// </summary>
    private void InitializePlayerPrefabsDict()
    {
        playerPrefabsDict.Clear();

        foreach (var entry in playerPrefabsMap)
        {
            if (playerPrefabsDict.ContainsKey(entry.Type))
            {
                Debug.LogWarning($"GameRoundManager: CharacterType '{entry.Type}' already exists in the dictionary. Duplicate entry ignored.");
                continue;
            }

            playerPrefabsDict.Add(entry.Type, entry.Prefab);
        }

        Debug.Log($"GameRoundManager: Player prefab dictionary initialized with {playerPrefabsDict.Count} entries.");
    }

    /// <summary>
    /// Called when the object is spawned on the network.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("GameRoundManager: Server started, setting up round...");
            StartCoroutine(ServerInitializeRound());
        }
    }

    /// <summary>
    /// Initializes the game round on the server: generates map, spawns players, and starts the match.
    /// </summary>
    private IEnumerator ServerInitializeRound()
    {
        yield return new WaitForSeconds(1f);

        GenerateMap();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayer(client.ClientId);
        }

        yield return new WaitForSeconds(1f);

        isRoundInitialized.Value = true;

        yield return new WaitForSeconds(spawnStartDelaySeconds);

        StartRoundServerRpc();
    }

    /// <summary>
    /// Randomly generates and spawns the map prefab on the server.
    /// </summary>
    private void GenerateMap()
    {
        if (!IsServer)
        {
            Debug.LogWarning("GameRoundManager: GenerateMap should only be called on the server.");
            return;
        }

        if (mapPrefabs == null || mapPrefabs.Length == 0)
        {
            Debug.LogError("GameRoundManager: No map prefabs have been assigned.");
            return;
        }

        int index = Random.Range(0, mapPrefabs.Length);
        GameObject prefab = mapPrefabs[index];

        GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);

        if (!instance.TryGetComponent(out NetworkObject netObj))
        {
            netObj = instance.AddComponent<NetworkObject>();
        }

        netObj.Spawn(true);

        Debug.Log($"GameRoundManager: Spawned map '{prefab.name}' at {instance.transform.position}");
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var clientInfo) || clientInfo.PlayerObject == null)
        {
            Debug.LogError($"GameRoundManager: No se encontro la informacion del cliente o el PlayerObject para el cliente {clientId}.");
            return;
        }

        if (!clientInfo.PlayerObject.TryGetComponent(out PlayerState playerState))
        {
            Debug.LogError($"GameRoundManager: El PlayerObject no tiene el componente PlayerState.");
            return;
        }

        CharacterType charType = (CharacterType)playerState.Character.Value;
        if (!playerPrefabsDict.TryGetValue(charType, out GameObject prefab) || prefab == null)
        {
            Debug.LogError($"GameRoundManager: No se encontro el prefab para CharacterType '{charType}' (Cliente {clientId}).");
            return;
        }

        GameObject go = Instantiate(prefab);
        PlayerPresenter presenter = go.GetComponent<PlayerPresenter>();

        if (presenter == null)
        {
            Debug.LogError($"GameRoundManager: El prefab instanciado '{go.name}' no contiene PlayerPresenter.");
            Destroy(go);
            return;
        }

        NetworkObject netObj = presenter.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError($"GameRoundManager: El Avatar instanciado '{presenter.gameObject.name}' no tiene NetworkObject.");
            Destroy(presenter.gameObject);
            return;
        }

        Vector3 spawnPos = new Vector3(Random.Range(-3f, 3f), 1f, 0f);
        presenter.transform.position = spawnPos;

        netObj.SpawnWithOwnership(clientId);

        SpawnPlayerHUDClientRpc(netObj.NetworkObjectId, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        });

        playerPresenters[clientId] = presenter;

        presenter.OnDeath += () =>
        {
            ulong ownerClientId = presenter.OwnerClientId;

            if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var clientInfo))
            {
                var playerState = clientInfo.PlayerObject.GetComponent<PlayerState>();
                if (playerState != null)
                {
                    playerState.AddDeathServerRpc();
                }
                else
                {
                    Debug.LogError($"PlayerState not found on PlayerObject of client {ownerClientId}");
                }
            }
            else
            {
                Debug.LogError($"No client information found for ClientId {ownerClientId}");
            }

            CheckRoundEndByDeaths();
        };
    }


    #endregion

    #region Update

    private void Update()
    {
        if (IsClient)
        {
            UpdateRemainingTimeDisplay();
        }
    }

    private void UpdateRemainingTimeDisplay()
    {
        if (timeTextHud == null) return;

        float time = remainingTime.Value;

        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);

        timeTextHud.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    private void CheckRoundEndByDeaths()
    {
        var alivePlayers = new List<PlayerPresenter>();

        foreach (var presenter in playerPresenters.Values)
        {
            if (presenter != null && presenter.IsAlive)
                alivePlayers.Add(presenter);
        }

        if (alivePlayers.Count <= 1)
        {
            EndRoundServerRpc();
        }
    }


    #endregion

    #region Network RPCs (Server Authority)

    [ClientRpc]
    private void SpawnPlayerHUDClientRpc(ulong networkObjectId, ClientRpcParams rpcParams = default)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
            return;

        if (netObj.IsOwner)
        {
            var hudInstance = Instantiate(playerHealthHudPrefab, healthHudParent);
            var hudScript = hudInstance.GetComponent<HealthBarHUD>();

            hudScript.Initialize(netObj.GetComponent<PlayerPresenter>());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartRoundServerRpc()
    {
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        spawnRoutine = StartCoroutine(ServerSpawnLethalRoutine());
    }

    private IEnumerator ServerSpawnLethalRoutine()
    {
        float elapsed = 0f;
        float spawnTimer = 0f;
        float spawnInterval = initialSpawnInterval;

        remainingTime.Value = matchDurationSeconds;
        qteRoutine = StartCoroutine(QTERoutine());

        while (remainingTime.Value > 0)
        {
            elapsed += Time.deltaTime;
            spawnTimer += Time.deltaTime;
            remainingTime.Value -= Time.deltaTime;

            float t = 1f - (remainingTime.Value / matchDurationSeconds);
            float currentInterval = Mathf.Lerp(initialSpawnInterval, minSpawnInterval, t);

            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                spawnInterval = currentInterval;
                SpawnLethalServer();
            }

            yield return null;
        }

        EndRoundServerRpc();
    }

    [ClientRpc]
    private void StartQTEClientRpc(ClientRpcParams rpcParams = default)
    {
        if (QTEManager.Instance != null)
            QTEManager.Instance.StartQTE();
    }

    [ClientRpc]
    private void EndQTEClientRpc(bool hasWinner, ulong winnerClientId)
    {
        if (QTEManager.Instance != null)
            QTEManager.Instance.ForceEndQTE(hasWinner ? (ulong?)winnerClientId : null);
    }

    private IEnumerator QTERoutine()
    {
        yield return new WaitForSeconds(10f);

        qteActiveOnServer = true;
        qteWinner = null;
        qteStartTimeServer = Time.time;

        var aliveIds = new List<ulong>(GetAlivePlayerIds());
        if (aliveIds.Count > 0)
        {
            originalTimeScale = Time.timeScale;
            Time.timeScale = qteTimeScale;

            StartQTEClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = aliveIds
                }
            });
        }

        float t = 0f;
        while (t < qteInputWindow && qteActiveOnServer)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Time.timeScale = originalTimeScale;

        if (!qteWinner.HasValue)
        {
            qteActiveOnServer = false;
            EndQTEClientRpc(false, 0);
        }
        else
        {
            EndQTEClientRpc(true, qteWinner.Value);
        }
    }

    private IEnumerable<ulong> GetAlivePlayerIds()
    {
        foreach (var kvp in playerPresenters)
        {
            var presenter = kvp.Value;
            if (presenter != null && presenter.IsAlive)
                yield return kvp.Key;
        }
    }

    private void SpawnLethalServer()
    {
        if (!IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;

        if (lethalPrefab == null || Camera.main == null)
            return;

        int screenW = Screen.width;
        int screenH = Screen.height;
        int allowedW = Mathf.Clamp(allowedSpawnWidthPixels, 1, screenW);
        int leftPx = (screenW - allowedW) / 2;
        int rightPx = leftPx + allowedW;

        float xPixel = Random.Range(leftPx, rightPx);
        float yPixel = screenH + spawnAboveScreenPixels;

        Vector3 screenPos = new(xPixel, yPixel, Mathf.Abs(Camera.main.transform.position.z));
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f;

        GameObject go = Instantiate(lethalPrefab, worldPos, Quaternion.identity);

        if (!go.TryGetComponent(out NetworkObject netObj))
            netObj = go.AddComponent<NetworkObject>();

        netObj.Spawn(true);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EndRoundServerRpc()
    {
        Debug.Log("GameRoundManager: Match ended!");
        if (spawnRoutine != null)
            StopCoroutine(spawnRoutine);

        foreach (var presenter in playerPresenters.Values)
        {
            if (presenter != null && presenter.IsAlive)
            {
                ulong ownerClientId = presenter.OwnerClientId;
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(ownerClientId, out var clientInfo))
                {
                    var playerState = clientInfo.PlayerObject.GetComponent<PlayerState>();
                    playerState?.AddRoundWinServerRpc();
                }
            }
        }

        NotifyRoundInterfaceOfEnd();

        StartCoroutine(LoadRoundInterfaceScene());
    }

    private void NotifyRoundInterfaceOfEnd()
    {
        var roundInterfaceObj = FindFirstObjectByType<RoundManager>();
        if (roundInterfaceObj != null)
        {
            roundInterfaceObj.OnRoundEndedServerRpc();
            Debug.Log("GameRoundManager: Notified RoundInterfaceManager of the end of the round.");
        }
        else
        {
            Debug.LogWarning("GameRoundManager: No active RoundInterfaceManager found at the end of the round.");
        }
    }

    private IEnumerator LoadRoundInterfaceScene()
    {
        yield return null;

        if (IsServer)
        {
            foreach (var presenter in playerPresenters.Values)
            {
                if (presenter != null && presenter.NetworkObject != null && presenter.NetworkObject.IsSpawned)
                {
                    presenter.NetworkObject.Despawn(true);
                }
            }

            playerPresenters.Clear();

            RoundManager roundManager = FindFirstObjectByType<RoundManager>();

            if (roundManager != null && roundManager.CurrentRound > 5)
            {
                SceneTransitionManager.Instance.LoadSceneWithTransition("WinnerScreen");
            }
            else
            {
                SceneTransitionManager.Instance.LoadSceneWithTransition("RoundInterface");
            }
        }
    }

    public float GetRemainingTime() => remainingTime.Value;

    [ServerRpc(RequireOwnership = false)]
    public void ReportQTEPressServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        RegisterQTEPressFromClient(senderId);
    }

    public void RegisterQTEPressFromClient(ulong senderId)
    {
        if (!IsServer) return;

        if (!qteActiveOnServer || qteWinner.HasValue)
        {
            Debug.Log($"[GameRoundManager] QTE ya finalizado o no activo. Ignorando press de {senderId}");
            return;
        }

        qteWinner = senderId;
        qteActiveOnServer = false;

        Debug.Log($"[GameRoundManager] Jugador {senderId} gano el QTE!");

        if (playerPresenters.TryGetValue(senderId, out var presenter))
        {
            presenter.ActivateImmunityServerRpc(5f);
            Debug.Log($"[GameRoundManager] Inmunidad de 5s activada para jugador {senderId}.");
        }
        else
        {
            Debug.LogError($"[GameRoundManager] No se encontro PlayerPresenter para cliente {senderId}");
        }

        EndQTEClientRpc(true, senderId);
    }

    #endregion
}