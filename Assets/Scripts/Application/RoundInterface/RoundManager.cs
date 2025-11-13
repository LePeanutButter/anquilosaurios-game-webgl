using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages game rounds in a networked multiplayer environment using Unity Netcode.
/// Handles round counting, countdown timers, scene transitions, and notifies clients of round changes.
/// </summary>
public class RoundManager : NetworkBehaviour
{
    #region Public Fields

    /// <summary>
    /// Singleton instance of the RoundManager.
    /// Ensures there is only one instance across scenes.
    /// </summary>
    public static RoundManager Instance { get; private set; }

    [Header("Settings")]
    public int totalRounds = 5;
    public float countdownSeconds = 5f;

    #endregion

    #region Private Fields

    /// <summary>
    /// Tracks the current round in a networked variable.
    /// Everyone can read, but only the server can update it.
    /// </summary>
    private NetworkVariable<int> currentRound = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    #endregion

    #region Public Properties and Events

    /// <summary>
    /// Gets the current round number.
    /// </summary>
    public int CurrentRound => currentRound.Value;

    /// <summary>
    /// Gets the duration of the countdown timer before a round starts.
    /// </summary>
    public float CountdownSeconds => countdownSeconds;

    /// <summary>
    /// Event invoked on clients whenever the countdown is updated.
    /// Sends the current countdown value in seconds.
    /// </summary>
    public event System.Action<int> OnCountdownUpdated;

    /// <summary>
    /// Event invoked on clients whenever the round changes.
    /// Sends the current round and total rounds.
    /// </summary>
    public event System.Action<int, int> OnRoundChanged;

    #endregion

    #region Initialization

    /// <summary>
    /// Ensures there is only one RoundManager instance and prevents it from being destroyed across scenes.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Called when the object spawns on the network.
    /// Registers a callback to handle scene load events if this instance is the server.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }
    }

    /// <summary>
    /// Called when a networked scene finishes loading for all clients.
    /// Handles starting countdowns for "RoundInterface" and incrementing rounds for "GameplayScene".
    /// </summary>
    /// <param name="sceneName">The name of the scene that finished loading.</param>
    /// <param name="mode">The load mode used to load the scene (Single or Additive).</param>
    /// <param name="clientsCompleted">A list of client IDs that successfully completed loading the scene.</param>
    /// <param name="clientsTimedOut">A list of client IDs that failed to load the scene within the timeout.</param>
    private void OnSceneLoaded(string sceneName, LoadSceneMode mode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer) return;

        if (sceneName == "RoundInterface")
        {
            StartCoroutine(ServerCountdownAndStartRound());
        }
        else if (sceneName == "GameplayScene")
        {
            IncrementRound();
        }
    }

    /// <summary>
    /// Runs a countdown timer on the server before starting a round.
    /// Updates all clients each second and transitions to the GameplayScene when complete.
    /// </summary>
    private IEnumerator ServerCountdownAndStartRound()
    {
        float t = countdownSeconds;
        while (t > 0f)
        {
            UpdateCountdownClientRpc(Mathf.CeilToInt(t));
            yield return new WaitForSeconds(1f);
            t -= 1f;
        }

        UpdateCountdownClientRpc(0);

        if (SceneManager.GetActiveScene().name == "RoundInterface")
            SceneTransitionManager.Instance.LoadSceneWithTransition("GameplayScene");
    }

    #endregion

    #region Network RPCs

    /// <summary>
    /// Called by clients or server to signal that a round has ended.
    /// Server checks if the maximum number of rounds has been reached.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void OnRoundEndedServerRpc()
    {
        if (!IsServer) return;

        if (currentRound.Value >= totalRounds)
        {
            NotifyRoundChangedClientRpc(currentRound.Value, totalRounds);
            return;
        }
    }

    /// <summary>
    /// Updates the countdown value on all clients.
    /// </summary>
    /// <param name="seconds">The current countdown in seconds.</param>
    [ClientRpc]
    private void UpdateCountdownClientRpc(int seconds)
    {
        OnCountdownUpdated?.Invoke(seconds);
    }

    /// <summary>
    /// Increments the current round on the server and notifies all clients of the change.
    /// </summary>
    private void IncrementRound()
    {
        if (!IsServer) return;
        currentRound.Value++;
        NotifyRoundChangedClientRpc(currentRound.Value, totalRounds);
    }

    /// <summary>
    /// Notifies all clients that the current round has changed.
    /// </summary>
    /// <param name="round">The current round number.</param>
    /// <param name="total">The total number of rounds.</param>
    [ClientRpc]
    private void NotifyRoundChangedClientRpc(int round, int total)
    {
        OnRoundChanged?.Invoke(round, total);
    }

    #endregion
}
