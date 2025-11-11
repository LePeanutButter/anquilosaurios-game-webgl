using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundInterfaceManager : NetworkBehaviour
{
    #region Serializable Fields

    [SerializeField] private GameObject roundManagerPrefab;

    #endregion

    #region Public Fields
    [Header("UI")]
    public Transform playerListContent;
    public GameObject playerListEntryPrefab;
    public TMP_Text countdownText;
    public TMP_Text roundsText;
    #endregion

    #region Private Fields
    private Dictionary<ulong, PlayerListEntryUI> entries = new Dictionary<ulong, PlayerListEntryUI>();
    private bool isRegisteredWithRoundManager = false;
    #endregion

    #region Initialization

    private void OnEnable()
    {

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }

        UnregisterFromRoundManager();
    }

    private void Start()
    {
        if (IsServer && RoundManager.Instance == null)
        {
            var rm = Instantiate(roundManagerPrefab);
            rm.GetComponent<NetworkObject>().Spawn(true);
        }

        StartCoroutine(LateInitialize());
    }

    private IEnumerator LateInitialize()
    {
        while (RoundManager.Instance == null || !RoundManager.Instance.IsSpawned)
        {
            yield return new WaitForSeconds(0.2f);
        }

        RegisterWithRoundManager();
        PopulatePlayerList();
    }

    private void RegisterWithRoundManager()
    {
        if (isRegisteredWithRoundManager) return;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnCountdownUpdated += UpdateCountdown;
            RoundManager.Instance.OnRoundChanged += UpdateRoundInfo;
            isRegisteredWithRoundManager = true;

            UpdateRoundInfo(RoundManager.Instance.CurrentRound, RoundManager.Instance.totalRounds);
            UpdateCountdown(Mathf.CeilToInt(RoundManager.Instance.CountdownSeconds));
        }
        else
        {
            StartCoroutine(RetryRegisterWithRoundManager());
        }
    }

    private IEnumerator RetryRegisterWithRoundManager()
    {
        int attempts = 0;
        while (RoundManager.Instance == null && attempts < 20)
        {
            yield return new WaitForSeconds(0.2f);
            attempts++;
        }

        if (RoundManager.Instance != null && !isRegisteredWithRoundManager)
        {
            RegisterWithRoundManager();
        }
    }

    private void UnregisterFromRoundManager()
    {
        if (!isRegisteredWithRoundManager) return;

        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnCountdownUpdated -= UpdateCountdown;
            RoundManager.Instance.OnRoundChanged -= UpdateRoundInfo;
        }

        isRegisteredWithRoundManager = false;
    }


    private void OnClientChanged(ulong clientId)
    {
        StartCoroutine(DelayedPopulatePlayerList());
    }

    private IEnumerator DelayedPopulatePlayerList()
    {
        yield return new WaitForSeconds(0.2f);
        PopulatePlayerList();
    }

    private void PopulatePlayerList()
    {
        foreach (var kv in entries)
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
        entries.Clear();

        if (NetworkManager.Singleton == null) return;

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

    public void UpdateCountdown(int seconds)
    {
        if (countdownText != null)
            countdownText.text = seconds > 0 ? $"Starting in {seconds}" : "Start!";
    }

    public void UpdateRoundInfo(int currentRound, int totalRounds)
    {
        if (roundsText != null)
            roundsText.text = $"Round {currentRound} / {totalRounds}";
    }
    #endregion
}