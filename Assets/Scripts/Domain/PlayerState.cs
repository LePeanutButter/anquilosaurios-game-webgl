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
    public readonly NetworkVariable<FixedString64Bytes> PlayerId =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<FixedString64Bytes> PlayerName =
        new NetworkVariable<FixedString64Bytes>(
            default,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<int> Character =
        new NetworkVariable<int>(
            (int)CharacterType.None,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<float> Health =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<float> MaxHealth =
        new NetworkVariable<float>(
            0f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public readonly NetworkVariable<bool> IsPlayerAlive =
        new NetworkVariable<bool>(
            false,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            if (Health.Value <= 0f)
            {
                Health.Value = MaxHealth.Value;
                IsPlayerAlive.Value = true;
            }
        }

        Health.OnValueChanged += OnHealthChanged;
        IsPlayerAlive.OnValueChanged += OnIsAliveChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        Health.OnValueChanged -= OnHealthChanged;
        IsPlayerAlive.OnValueChanged -= OnIsAliveChanged;
    }

    [ServerRpc(RequireOwnership = false)]
    public void InitializeServerRpc(string id, string name, int characterType, float maxHealth = 100f)
    {
        if (!IsServer) return;

        PlayerId.Value = id;
        PlayerName.Value = name;
        Character.Value = characterType;
        MaxHealth.Value = maxHealth;
        Health.Value = maxHealth;
        IsPlayerAlive.Value = true;

        Debug.Log($"[Server] Player initialized: {name} (ID: {id}) as {(CharacterType)characterType}");

        Debug.Log(
        $"[PlayerState Initialized]\n" +
        $"- IsServer: {IsServer}\n" +
        $"- OwnerClientId: {OwnerClientId}\n" +
        $"- NetworkObjectId: {NetworkObjectId}\n" +
        $"- PlayerId: {PlayerId.Value}\n" +
        $"- PlayerName: {PlayerName.Value}\n" +
        $"- Character: {(CharacterType)Character.Value}\n" +
        $"- Health: {Health.Value}\n" +
        $"- MaxHealth: {MaxHealth.Value}\n" +
        $"- IsPlayerAlive: {IsPlayerAlive.Value}\n"
    );
    }
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

    [ServerRpc(RequireOwnership = false)]
    public void ApplyLinearRecoveryServerRpc(float tickInterval)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float recoveryAmount = MaxHealth.Value * 0.1f * tickInterval;
        float newHealth = Mathf.Min(MaxHealth.Value, Health.Value + recoveryAmount);
        SetHealth(newHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void HealServerRpc(float amount)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float newHealth = Mathf.Min(MaxHealth.Value, Health.Value + amount);
        SetHealth(newHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageServerRpc(float amount)
    {
        if (!IsServer || !IsPlayerAlive.Value) return;

        float newHealth = Mathf.Max(0f, Health.Value - amount);
        SetHealth(newHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetHealthServerRpc()
    {
        if (!IsServer) return;
        SetHealth(MaxHealth.Value);
    }

    private void SetHealth(float newHealth)
    {
        Health.Value = newHealth;
        IsPlayerAlive.Value = Health.Value > 0f;
    }

    private void OnHealthChanged(float previous, float current)
    {
        Debug.Log($"[{PlayerName.Value}] Health: {previous} - {current}");
    }

    private void OnIsAliveChanged(bool previous, bool current)
    {
        Debug.Log($"[{PlayerName.Value}] Alive: {previous} - {current}");
    }
}