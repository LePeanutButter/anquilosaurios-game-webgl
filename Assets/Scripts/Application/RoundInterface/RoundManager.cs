using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundManager : NetworkBehaviour
{
    #region Public Fields
    public static RoundManager Instance { get; private set; }

    [Header("Settings")]
    public int totalRounds = 5;
    public float countdownSeconds = 5f;

    #endregion

    #region Private Fields

    private NetworkVariable<int> currentRound = new NetworkVariable<int>(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    #endregion

    #region Public Properties and Events

    public int CurrentRound => currentRound.Value;
    public float CountdownSeconds => countdownSeconds;

    public event System.Action<int> OnCountdownUpdated;
    public event System.Action<int, int> OnRoundChanged;

    #endregion

    #region Initialization

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            NetworkManager.SceneManager.OnLoadEventCompleted += OnSceneLoaded;
        }
    }

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

    [ClientRpc]
    private void UpdateCountdownClientRpc(int seconds)
    {
        OnCountdownUpdated?.Invoke(seconds);
    }

    private void IncrementRound()
    {
        if (!IsServer) return;
        currentRound.Value++;
        NotifyRoundChangedClientRpc(currentRound.Value, totalRounds);
    }

    [ClientRpc]
    private void NotifyRoundChangedClientRpc(int round, int total)
    {
        OnRoundChanged?.Invoke(round, total);
    }

    #endregion
}
