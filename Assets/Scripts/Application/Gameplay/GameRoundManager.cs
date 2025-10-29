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
    [SerializeField] private GameObject lethalPrefab;
    [SerializeField] private GameObject[] mapPrefabs;

    [Header("Match Settings")]
    [SerializeField] private float matchDurationSeconds = 30f;
    [SerializeField] private float spawnStartDelaySeconds = 2f;
    [SerializeField] private float initialSpawnInterval = 1f;
    [SerializeField] private float minSpawnInterval = 0.3f;
    [SerializeField] private int allowedSpawnWidthPixels = 1850;
    [SerializeField] private int spawnAboveScreenPixels = 50;

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


    /// <summary>
    /// Encuentra el PlayerState, le pide que instancie el avatar (si no lo ha hecho) y lo spawnea en la red.
    /// </summary>
    private void SpawnPlayer(ulong clientId)
    {
        if (!IsServer) return;

        PlayerState playerState = SessionManager.Instance?.GetPlayerStateForClient(clientId);

        if (playerState == null)
        {
            Debug.LogWarning($"GameRoundManager: No PlayerState found for client {clientId}. Cannot spawn player.");
            return;
        }

        PlayerPresenter presenter = playerState.ActivePresenter;
        if (presenter == null)
        {
            presenter = playerState.InstantiateAvatar();
        }

        if (presenter == null)
        {
            Debug.LogError($"GameRoundManager: Failed to get or instantiate avatar for client {clientId}.");
            return;
        }

        NetworkObject netObj = presenter.GetComponent<NetworkObject>();

        if (netObj == null)
        {
            Debug.LogError($"GameRoundManager: Avatar for client {clientId} is missing NetworkObject.");
            return;
        }

        presenter.InitializePresenter(playerState);

        Vector2 spawnPos = new(Random.Range(-3f, 3f), 1f);
        netObj.transform.position = spawnPos;

        netObj.SpawnWithOwnership(clientId);

        Debug.Log($"Spawned player avatar (CharacterType: {(CharacterType)playerState.Character.Value}) for client {clientId} at {spawnPos}.");
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