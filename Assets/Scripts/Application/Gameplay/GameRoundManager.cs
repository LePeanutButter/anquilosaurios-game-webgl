using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public static GameRoundManager Instance { get; private set; }

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
    private NetworkVariable<float> remainingTime = new(writePerm: NetworkVariableWritePermission.Server);

    #endregion

    #region Initialization & Network Spawn

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

    private void InitializePlayerPrefabsDict()
    {
        playerPrefabsDict.Clear();

        foreach (var entry in playerPrefabsMap)
        {
            if (playerPrefabsDict.ContainsKey(entry.Type))
            {
                Debug.LogWarning($"GameRoundManager: El CharacterType '{entry.Type}' ya existe en el diccionario. Se ignora la entrada duplicada.");
                continue;
            }

            playerPrefabsDict.Add(entry.Type, entry.Prefab);
        }

        Debug.Log($"GameRoundManager: Diccionario de Player Prefabs inicializado con {playerPrefabsDict.Count} entradas.");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("GameRoundManager: Server started, setting up round...");
            StartCoroutine(ServerInitializeRound());
        }
    }

    private IEnumerator ServerInitializeRound()
    {
        yield return new WaitForSeconds(1f);

        GenerateMap();

        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            SpawnPlayer(client.ClientId);
        }

        yield return new WaitForSeconds(spawnStartDelaySeconds);

        StartRoundServerRpc();
    }

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
        playerPresenters[clientId] = presenter;
        SpawnPlayerHealthHudClientRpc(netObj.NetworkObjectId);
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

    #endregion

    #region Network RPCs (Server Authority)

    [ClientRpc]
    private void SpawnPlayerHealthHudClientRpc(ulong playerNetworkObjectId)
    {
        if (playerHealthHudPrefab == null || healthHudParent == null)
        {
            Debug.LogError("GameRoundManager: playerHealthHudPrefab o healthHudParent no est�n asignados.");
            return;
        }

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(playerNetworkObjectId, out NetworkObject netObj))
        {
            Debug.LogError($"GameRoundManager: No se pudo encontrar el NetworkObject con ID: {playerNetworkObjectId}.");
            return;
        }

        GameObject hudInstance = Instantiate(playerHealthHudPrefab, healthHudParent);

        Debug.Log($"GameRoundManager: Cliente {NetworkManager.Singleton.LocalClientId} instanci� HUD para el jugador {playerNetworkObjectId}.");
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
    private void StartQTEClientRpc()
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
        StartQTEClientRpc();

        float t = 0f;
        while (t < qteInputWindow && qteActiveOnServer)
        {
            t += Time.deltaTime;
            yield return null;
        }

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

    private void SpawnLethalServer()
    {
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