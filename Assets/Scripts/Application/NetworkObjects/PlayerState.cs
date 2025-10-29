using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network adapter that synchronizes id, name and character between network and domain.
/// In a host-authoritative session the server stores the Player domain model in PlayerServiceMono (in-memory).
/// Clients read NetworkVariables and request changes via ServerRpc.
/// </summary>
public class PlayerState : NetworkBehaviour
{
    [Header("Presentation")]
    [Tooltip("Lista de GameObjects Prefabs de avatar, mapeados por CharacterType.")]
    [SerializeField]
    private CharacterPrefabEntry[] characterPrefabs;
    private Dictionary<CharacterType, GameObject> _prefabMap;

    [System.Serializable]
    public struct CharacterPrefabEntry
    {
        public CharacterType characterType;
        public GameObject prefab;
    }

    private PlayerPresenter _activePresenter;

    public PlayerPresenter ActivePresenter => _activePresenter;

    /// <summary>
    /// Unique identifier for the player.
    /// </summary>
    public readonly NetworkVariable<ulong> PlayerId =
        new NetworkVariable<ulong>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// Display name of the player.
    /// </summary>
    public readonly NetworkVariable<FixedString64Bytes> PlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// Selected character type represented as an integer.
    /// </summary>
    public readonly NetworkVariable<int> Character =
        new NetworkVariable<int>(
            (int)CharacterType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// Number of times the player has died.
    /// </summary>
    public readonly NetworkVariable<int> DeathCount =
    new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>
    /// Number of rounds the player has won.
    /// </summary>
    public readonly NetworkVariable<int> RoundWins =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    private void Awake()
    {
        _prefabMap = new Dictionary<CharacterType, GameObject>();
        foreach (var entry in characterPrefabs)
        {
            if (entry.prefab != null && !_prefabMap.ContainsKey(entry.characterType))
            {
                _prefabMap.Add(entry.characterType, entry.prefab);
            }
        }
    }

    /// <summary>
    /// Called when the object is spawned on the network.
    /// Subscribes to health and alive status change events.
    /// </summary>
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        Character.OnValueChanged += OnCharacterSelected;

        if (IsServer)
        {
            PlayerId.Value = OwnerClientId;
        }

        Debug.Log($"PlayerState spawned. OwnerClientId: {OwnerClientId}, IsOwner: {IsOwner}");
    }

    /// <summary>
    /// Called when the object is despawned from the network.
    /// Unsubscribes from health and alive status change events.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Character.OnValueChanged -= OnCharacterSelected;

        if (IsServer && _activePresenter != null)
        {
            Destroy(_activePresenter.gameObject);
            _activePresenter = null;
        }
    }

    /// <summary>
    /// Initializes player data on the server.
    /// </summary>
    /// <param name="name">Player name.</param>
    /// <param name="characterType">Character type as integer.</param>
    /// <param name="maxHealth">Maximum health value.</param>
    public void InitializeDataServer(string name, int characterType)
    { 
        if (!IsServer) return;

        PlayerName.Value = name;
        Character.Value = characterType;
        DeathCount.Value = 0;
        RoundWins.Value = 0;

        Debug.Log($"[Server] Player initialized: {name} as {(CharacterType)characterType}");
        DebugNetworkState();
    }

    /// <summary>
    /// Increments the player's round win count.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddRoundWinServerRpc()
    {
        if (!IsServer) return;
        RoundWins.Value++;
        Debug.Log($"[{PlayerName.Value}] RoundWins incremented to {RoundWins.Value}");
    }

    /// <summary>
    /// Increments the player's death count.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddDeathServerRpc()
    {
        if (!IsServer) return;
        DeathCount.Value++;
        Debug.Log($"[{PlayerName.Value}] DeathCount incremented to {DeathCount.Value}");
    }

    #region Network Variable Handlers

    private void OnCharacterSelected(int previous, int current)
    {
        Debug.Log($"[{PlayerName.Value}] Character seleccionado: {(CharacterType)previous} -> {(CharacterType)current}");
    }

    #endregion

    /// <summary>
    /// Displays all the current values of the player's NetworkVariables in the console.
    /// </summary>
    [ContextMenu("Debug Network State")]
    public void DebugNetworkState()
    {
        string state = $"[PlayerState Debug] OwnerClientId: {OwnerClientId}\n" +
                       $"PlayerId: {PlayerId.Value}\n" +
                       $"PlayerName: {PlayerName.Value}\n" +
                       $"Character: {(CharacterType)Character.Value} ({Character.Value})\n" +
                       $"DeathCount: {DeathCount.Value}\n" +
                       $"RoundWins: {RoundWins.Value}";

        Debug.Log(state);
    }
}