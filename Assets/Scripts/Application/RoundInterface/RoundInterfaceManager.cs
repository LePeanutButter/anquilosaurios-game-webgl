using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundInterfaceManager : NetworkBehaviour
{
    [Header("UI")]
    public Transform playerListContent;
    public GameObject playerListEntryPrefab;
    public TMP_Text countdownText;
    public TMP_Text roundsText;

    [Header("Settings")]
    public int totalRounds = 5;
    public float countdownSeconds = 5f;

    private Dictionary<ulong, PlayerListEntryUI> entries = new Dictionary<ulong, PlayerListEntryUI>();
    private NetworkVariable<int> currentRound = new NetworkVariable<int>(0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private void Start()
    {
        PopulatePlayerList();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (currentRound.Value == 0) currentRound.Value = 0;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        RefreshUI();
        PopulatePlayerList();

        RefreshUI();
        PopulatePlayerList();

        if (IsServer)
        {
            StartCoroutine(ServerCountdownAndStartRound());
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    private void OnClientConnected(ulong clientId) => PopulatePlayerList();
    private void OnClientDisconnected(ulong clientId) => PopulatePlayerList();

    private void PopulatePlayerList()
    {
        foreach (var kv in entries) Destroy(kv.Value.gameObject);
        entries.Clear();

        foreach (var netObj in NetworkManager.Singleton.SpawnManager.SpawnedObjectsList)
        {
            var ps = netObj.GetComponent<PlayerState>();
            if (ps == null) continue;

            ulong owner = netObj.OwnerClientId;
            var entryGO = Instantiate(playerListEntryPrefab, playerListContent);
            var ui = entryGO.GetComponent<PlayerListEntryUI>();
            ui.SetPlayerState(ps);
            entries[owner] = ui;
        }
    }

    private void RefreshUI()
    {
        roundsText.text = $"Round {currentRound.Value} / {totalRounds}";
    }

    private System.Collections.IEnumerator ServerCountdownAndStartRound()
    {
        float t = countdownSeconds;
        while (t > 0f)
        {
            UpdateCountdownClientRpc(Mathf.CeilToInt(t));
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }

        UpdateCountdownClientRpc(0);
        IncrementRoundServerRpc();
        NetworkManager.Singleton.SceneManager.LoadScene("GameplayScene", LoadSceneMode.Single);
    }

    [ClientRpc]
    private void UpdateCountdownClientRpc(int seconds)
    {
        if (countdownText != null) countdownText.text = seconds > 0 ? $"Starting in {seconds}" : "Start!";
    }

    [ServerRpc(RequireOwnership = false)]
    private void IncrementRoundServerRpc()
    {
        if (!IsServer) return;
        currentRound.Value++;
        RefreshUI();
    }

    [ServerRpc(RequireOwnership = false)]
    public void OnRoundEndedServerRpc()
    {
        if (!IsServer) return;

        if (currentRound.Value >= totalRounds)
        {
            UpdateAllClientsRoundInfoClientRpc(currentRound.Value);
            return;
        }

        StartCoroutine(ServerCountdownAndStartRound());
    }

    [ClientRpc]
    private void UpdateAllClientsRoundInfoClientRpc(int round)
    {
        roundsText.text = $"Round {round} / {totalRounds}";
        PopulatePlayerList();
    }
}
