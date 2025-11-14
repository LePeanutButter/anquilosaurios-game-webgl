using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using TMPro;
using UnityEngine;

public class RoundInterfaceManager : NetworkBehaviour
{
    #region Serializable Fields

    [SerializeField] private GameObject roundManagerPrefab;

    #endregion

    #region Public Fields

    [Header("UI")]
    [Tooltip("Parent transform for player list entries.")]
    public Transform playerListContent;

    [Tooltip("Prefab for an individual player list entry.")]
    public GameObject playerListEntryPrefab;

    [Tooltip("Text UI element to show the countdown timer.")]
    public TMP_Text countdownText;

    [Tooltip("Text UI element to show current round info.")]
    public TMP_Text roundsText;
    
    #endregion

    #region Private Fields

    private Dictionary<ulong, PlayerListEntryUI> entries = new Dictionary<ulong, PlayerListEntryUI>();
    private bool isRegisteredWithRoundManager = false;

    #endregion

    #region Unity Lifecycle Methods

    /// <summary>
    /// Subscribes to client connection/disconnection events when enabled.
    /// </summary>
    private void OnEnable()
    {

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientChanged;
        }
    }

    /// <summary>
    /// Unsubscribes from client events and unregisters from the RoundManager.
    /// </summary>
    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientChanged;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientChanged;
        }

        UnregisterFromRoundManager();
    }

    /// <summary>
    /// Starts the round manager if this is the server and begins late initialization coroutine.
    /// </summary>
    private void Start()
    {
        if (IsServer && RoundManager.Instance == null)
        {
            var rm = Instantiate(roundManagerPrefab);
            rm.GetComponent<NetworkObject>().Spawn(true);
        }

        StartCoroutine(LateInitialize());
    }

    #endregion

    #region Initialization Methods

    /// <summary>
    /// Waits until RoundManager is ready, then registers for events and populates the player list.
    /// </summary>
    private IEnumerator LateInitialize()
    {
        while (RoundManager.Instance == null || !RoundManager.Instance.IsSpawned)
        {
            yield return new WaitForSeconds(0.2f);
        }

        RegisterWithRoundManager();
        PopulatePlayerList();
    }

    /// <summary>
    /// Registers for round-related events and updates UI immediately.
    /// </summary>
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

    /// <summary>
    /// Attempts to register with the RoundManager multiple times in case it's not yet initialized.
    /// </summary>
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

    /// <summary>
    /// Unsubscribes from round events.
    /// </summary>
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

    #endregion

    #region Client Connection Methods

    /// <summary>
    /// Called whenever a client connects or disconnects. Refreshes the player list after a short delay.
    /// </summary>
    /// <param name="clientId">The client ID that changed.</param>
    private void OnClientChanged(ulong clientId)
    {
        StartCoroutine(DelayedPopulatePlayerList());
    }

    /// <summary>
    /// Waits briefly and then repopulates the player list.
    /// </summary>
    private IEnumerator DelayedPopulatePlayerList()
    {
        yield return new WaitForSeconds(0.2f);
        PopulatePlayerList();
    }

    #endregion

    #region Player List Methods

    /// <summary>
    /// Populates the player list UI by instantiating entry prefabs for each connected player.
    /// </summary>
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

    #endregion

    #region UI Update Methods

    /// <summary>
    /// Updates the countdown text in the UI.
    /// </summary>
    /// <param name="seconds">Remaining seconds for countdown.</param>
    public void UpdateCountdown(int seconds)
    {
        if (countdownText != null)
            countdownText.text = seconds > 0 ? $"Listo en {seconds}" : "¡Vamos!";
    }

    /// <summary>
    /// Updates the round number text in the UI.
    /// </summary>
    /// <param name="currentRound">Current round number.</param>
    /// <param name="totalRounds">Total number of rounds.</param>
    public void UpdateRoundInfo(int currentRound, int totalRounds)
    {
        if (roundsText != null)
            roundsText.text = $"Ronda {currentRound} / {totalRounds}";
    }

    #endregion
}