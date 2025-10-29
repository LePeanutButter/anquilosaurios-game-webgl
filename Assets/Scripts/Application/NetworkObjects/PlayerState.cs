using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static GameRoundManager;

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
    /// Current health value of the player.
    /// </summary>
    public readonly NetworkVariable<float> Health =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// Maximum health value the player can have.
    /// </summary>
    public readonly NetworkVariable<float> MaxHealth =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    /// <summary>
    /// Indicates whether the player is currently alive.
    /// </summary>
    public readonly NetworkVariable<bool> IsPlayerAlive =
        new NetworkVariable<bool>(
            false,
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

        Health.OnValueChanged += OnHealthChanged;
        IsPlayerAlive.OnValueChanged += OnIsAliveChanged;

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
        Health.OnValueChanged -= OnHealthChanged;
        IsPlayerAlive.OnValueChanged -= OnIsAliveChanged;
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
    [ClientRpc(RequireOwnership = false)]
    public void InitializeClientRpc(string name, int characterType, float maxHealth = 100f)
    {
        if (!IsServer) return;

        PlayerName.Value = name;
        Character.Value = characterType;
        MaxHealth.Value = maxHealth;
        Health.Value = maxHealth;
        IsPlayerAlive.Value = true;
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


    /// <summary>
    /// Applies exponential damage based on missing health.
    /// </summary>
    /// <param name="tickInterval">Time interval for damage calculation.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyExponentialDamageServerRpc(float tickInterval)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float baseDamage = 5f * tickInterval;
        float missingHealth = Mathf.Max(0f, MaxHealth.Value - Health.Value);
        float scalingDamage = Mathf.Pow(missingHealth, 1.2f) * tickInterval;
        float totalDamage = baseDamage + scalingDamage;

        float newHealth = Mathf.Max(0f, Health.Value - totalDamage);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Applies linear health recovery over time.
    /// </summary>
    /// <param name="tickInterval">Time interval for recovery calculation.</param>
    [ServerRpc(RequireOwnership = false)]
    public void ApplyLinearRecoveryServerRpc(float tickInterval)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float recoveryAmount = MaxHealth.Value * 0.1f * tickInterval;
        float newHealth = Mathf.Min(MaxHealth.Value, Health.Value + recoveryAmount);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Heals the player by a fixed amount.
    /// </summary>
    /// <param name="amount">Amount to heal.</param>
    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float amount)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float newHealth = Mathf.Min(MaxHealth.Value, Health.Value + amount);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Damages the player by a fixed amount.
    /// </summary>
    /// <param name="amount">Amount of damage.</param>
    [ServerRpc(RequireOwnership = false)]
    public void DamageServerRpc(float amount)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float newHealth = Mathf.Max(0f, Health.Value - amount);
        SetHealth(newHealth);
    }

    /// <summary>
    /// Resets the player's health to maximum.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        if (!IsServer) return;
        SetHealth(MaxHealth.Value);
    }

    /// <summary>
    /// Updates health and alive status.
    /// </summary>
    /// <param name="newHealth">New health value.</param>
    private void SetHealth(float newHealth)
    {
        Health.Value = newHealth;
        IsPlayerAlive.Value = Health.Value > 0f;
    }

    #region Network Variable Handlers

    private void OnCharacterSelected(int previous, int current)
    {
        Debug.Log($"[{PlayerName.Value}] Character seleccionado: {(CharacterType)previous} -> {(CharacterType)current}");
    }

    /// <summary>
    /// Logs health changes for debugging.
    /// </summary>
    private void OnHealthChanged(float previous, float current)
    {
        Debug.Log($"[{PlayerName.Value}] Health: {previous} - {current}");
    }

    /// <summary>
    /// Logs alive status changes for debugging.
    /// </summary>
    private void OnIsAliveChanged(bool previous, bool current)
    {
        Debug.Log($"[{PlayerName.Value}] Alive: {previous} - {current}");
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
                       $"Health: {Health.Value}\n" +
                       $"MaxHealth: {MaxHealth.Value}\n" +
                       $"IsPlayerAlive: {IsPlayerAlive.Value}\n" +
                       $"DeathCount: {DeathCount.Value}\n" +
                       $"RoundWins: {RoundWins.Value}";

        Debug.Log(state);
    }

    #region Public API

    /// <summary>
    /// Instancia el avatar del personaje en el servidor, basándose en el Character.Value seleccionado.
    /// Esto DEBE ser llamado por un sistema de juego (ej. RoundManager) cuando el avatar sea requerido.
    /// </summary>
    /// <returns>La instancia creada del PlayerPresenter o null si falla.</returns>
    public PlayerPresenter InstantiateAvatar()
    {
        if (!IsServer)
        {
            Debug.LogError("InstantiateAvatar solo puede ser llamado por el Host/Server.");
            return null;
        }

        if (_activePresenter != null)
        {
            Debug.LogWarning($"Avatar ya instanciado para el jugador {PlayerName.Value}. Retornando la instancia existente.");
            return _activePresenter;
        }

        CharacterType selectedType = (CharacterType)Character.Value;

        if (!_prefabMap.TryGetValue(selectedType, out GameObject selectedPrefab) || selectedPrefab == null)
        {
            Debug.LogError($"PlayerState para el cliente {OwnerClientId}: CharacterType {selectedType} no tiene prefab válido en el mapa.", this);
            return null;
        }

        GameObject avatarGO = Instantiate(selectedPrefab);

        _activePresenter = avatarGO.GetComponent<PlayerPresenter>();

        if (_activePresenter == null)
        {
            Debug.LogError($"El prefab de personaje '{selectedPrefab.name}' no contiene un PlayerPresenter.", this);
            Destroy(avatarGO);
            return null;
        }

        NetworkObject networkObject = avatarGO.GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError($"El PlayerAvatarPrefab debe tener un componente NetworkObject.", this);
            Destroy(avatarGO);
            return null;
        }

        Debug.Log($"[Server] Avatar {selectedPrefab.name} INSTANCIADO (NO SPAWNEADO) para cliente {OwnerClientId}. Está listo.");
        return _activePresenter;
    }

    #endregion
}