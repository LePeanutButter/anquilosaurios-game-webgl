using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Network adapter that synchronizes id, name, character, death count, and round wins between the network and the domain.
/// In a host-authoritative session, the server stores the Player domain model in PlayerServiceMono (in-memory).
/// Clients read NetworkVariables and request changes via ServerRpc methods.
/// </summary>
public class PlayerState : NetworkBehaviour
{
    #region Serializable Fields

    [Header("Presentation")]
    [Tooltip("List of GameObject prefabs for each CharacterType.")]
    [SerializeField]
    private CharacterPrefabEntry[] characterPrefabs;

    #endregion

    #region Private Fields

    private Dictionary<CharacterType, GameObject> _prefabMap;
    private PlayerPresenter _activePresenter;

    #endregion

    #region Public Properties

    /// <summary>
    /// Currently active presenter for this player (avatar representation).
    /// </summary>
    public PlayerPresenter ActivePresenter => _activePresenter;

    #endregion

    #region Network Variables

    /// <summary>
    /// NetworkVariable representing the unique player ID.
    /// </summary>
    public readonly NetworkVariable<ulong> PlayerId =
        new NetworkVariable<ulong>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// NetworkVariable representing the player's display name.
    /// </summary>
    public readonly NetworkVariable<FixedString64Bytes> PlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// NetworkVariable representing the selected character as an integer.
    /// </summary>
    public readonly NetworkVariable<int> Character =
        new NetworkVariable<int>(
            (int)CharacterType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// NetworkVariable representing the number of times the player has died.
    /// </summary>
    public readonly NetworkVariable<int> DeathCount =
    new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    /// <summary>
    /// NetworkVariable representing the number of rounds the player has won.
    /// </summary>
    public readonly NetworkVariable<int> RoundWins =
        new NetworkVariable<int>(
            0,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    #endregion

    #region Serializable Structs

    /// <summary>
    /// Serializable mapping of CharacterType to prefab GameObject.
    /// </summary>
    [System.Serializable]
    public struct CharacterPrefabEntry
    {
        public CharacterType characterType;
        public GameObject prefab;
    }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes the prefab dictionary from the serialized array.
    /// </summary>
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
    /// Subscribes to network variable change events and initializes PlayerId on the server.
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
    /// Unsubscribes from network variable change events and cleans up the active presenter.
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

    #endregion

    #region Server Methods

    /// <summary>
    /// Initializes the player's data on the server.
    /// </summary>
    /// <param name="name">The player's display name.</param>
    /// <param name="characterType">The selected character type as integer.</param>
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
    /// Increments the player's round win count on the server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddRoundWinServerRpc()
    {
        if (!IsServer) return;
        RoundWins.Value++;
        Debug.Log($"[{PlayerName.Value}] RoundWins incremented to {RoundWins.Value}");
    }

    /// <summary>
    /// Increments the player's death count on the server.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void AddDeathServerRpc()
    {
        if (!IsServer) return;
        DeathCount.Value++;
        Debug.Log($"[{PlayerName.Value}] DeathCount incremented to {DeathCount.Value}");
    }

    #endregion

    #region NetworkVariable Callbacks

    /// <summary>
    /// Handler for when the player's character selection changes.
    /// </summary>
    /// <param name="previous">Previous character type value.</param>
    /// <param name="current">Current character type value.</param>
    private void OnCharacterSelected(int previous, int current)
    {
        Debug.Log($"[{PlayerName.Value}] Character selected: {(CharacterType)previous} -> {(CharacterType)current}");
    }

    #endregion

    #region Helpers / Debug

    /// <summary>
    /// Prints all current network variable values to the console for debugging purposes.
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

    #endregion
}