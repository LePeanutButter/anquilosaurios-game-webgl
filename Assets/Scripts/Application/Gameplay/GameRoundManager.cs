using System.Collections;
using System.Collections.Generic;
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

    private Coroutine spawnRoutine;

    private NetworkVariable<float> remainingTime = new(writePerm: NetworkVariableWritePermission.Server);

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
            Debug.LogError($"GameRoundManager: No se encontró la información del cliente o el PlayerObject para el cliente {clientId}.");
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
            Debug.LogError($"GameRoundManager: No se encontró el prefab para CharacterType '{charType}' (Cliente {clientId}).");
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
}