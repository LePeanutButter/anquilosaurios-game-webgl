using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

    [Header("Prefabs and Scene")]
    [SerializeField] private GameObject mapGeneratorPrefab;
    [SerializeField] private GameObject lethalPrefab;

    [Header("Player Prefabs by CharacterType")]
    [SerializeField] private CharacterPrefabEntry[] playerPrefabs;

    [System.Serializable]
    public struct CharacterPrefabEntry
    {
        public CharacterType characterType;
        public GameObject prefab;
    }

    [Header("Match Settings")]
    [SerializeField] private float matchDurationSeconds = 30f;
    [SerializeField] private float spawnStartDelaySeconds = 2f;
    [SerializeField] private float initialSpawnInterval = 1f;
    [SerializeField] private float minSpawnInterval = 0.3f;
    [SerializeField] private int allowedSpawnWidthPixels = 1850;
    [SerializeField] private int spawnAboveScreenPixels = 50;

    private Coroutine spawnRoutine;
    private MapGenerator mapGenerator;

    private NetworkVariable<float> remainingTime = new(writePerm: NetworkVariableWritePermission.Server);

    private readonly Dictionary<CharacterType, GameObject> prefabMap = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        prefabMap.Clear();
        foreach (var entry in playerPrefabs)
        {
            if (entry.prefab != null && !prefabMap.ContainsKey(entry.characterType))
            {
                prefabMap.Add(entry.characterType, entry.prefab);
            }
        }
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
        if (mapGeneratorPrefab == null)
        {
            Debug.LogError("GameRoundManager: No mapGeneratorPrefab assigned.");
            return;
        }

        GameObject mapGO = Instantiate(mapGeneratorPrefab);
        mapGenerator = mapGO.GetComponent<MapGenerator>();
        if (mapGenerator != null)
        {
            mapGenerator.GenerateMap();
        }

        if (!mapGO.TryGetComponent(out NetworkObject netObj))
            netObj = mapGO.AddComponent<NetworkObject>();

        netObj.Spawn(true);
    }

    private void SpawnPlayer(ulong clientId)
    {
        PlayerState playerState = FindPlayerState(clientId);

        CharacterType charType = CharacterType.None;
        if (playerState != null)
        {
            charType = (CharacterType)playerState.Character.Value;
        }
        else
        {
            Debug.LogWarning($"GameRoundManager: No PlayerState found for client {clientId}, using default character.");
        }

        if (!prefabMap.TryGetValue(charType, out GameObject prefab))
        {
            Debug.LogWarning($"GameRoundManager: No prefab found for {charType}, using first available prefab.");
            prefab = prefabMap.Values.FirstOrDefault();
        }

        if (prefab == null)
        {
            Debug.LogError("GameRoundManager: No valid player prefab available!");
            return;
        }

        Vector2 spawnPos = new(Random.Range(-3f, 3f), 1f);
        GameObject playerObj = Instantiate(prefab, spawnPos, Quaternion.identity);

        if (!playerObj.TryGetComponent(out NetworkObject netObj))
            netObj = playerObj.AddComponent<NetworkObject>();

        netObj.SpawnWithOwnership(clientId);

        Debug.Log($"Spawned player for client {clientId} using CharacterType = {charType}");
    }

    private PlayerState FindPlayerState(ulong clientId)
    {
        var states = FindObjectsByType<PlayerState>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var state in states)
        {
            if (state.OwnerClientId == clientId)
                return state;
        }

        return null;
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