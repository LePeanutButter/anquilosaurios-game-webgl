using System.Collections;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Network adapter that synchronizes id, name and character between network and domain.
/// In a host-authoritative session the server stores the Player domain model in PlayerServiceMono (in-memory).
/// Clients read NetworkVariables and request changes via ServerRpc.
/// </summary>
public class PlayerState : NetworkBehaviour
{
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

    /// <summary>
    /// Called when the object is despawned from the network.
    /// Unsubscribes from health and alive status change events.
    /// </summary>
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Health.OnValueChanged -= OnHealthChanged;
        IsPlayerAlive.OnValueChanged -= OnIsAliveChanged;
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

    private void OnEnabled()
    {
        PlayerEvents.OnPlayerDied += OnPlayerDied;
    }

    private void OnDisabled()
    {
        PlayerEvents.OnPlayerDied -= OnPlayerDied;
    }

    private void OnPlayerDied(ulong playerId)
    {
        if (playerId != OwnerClientId)
            return;

        if (IsServer)
        {
            Health.Value = 0;
            Debug.Log($"PlayerState: Player {playerId} marked as dead.");
        }
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
}