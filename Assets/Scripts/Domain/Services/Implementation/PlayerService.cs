using System;
using UnityEngine;

/// <summary>
/// Provides services for managing player identification and health-related operations.
/// </summary>
public class PlayerService : IPlayerService
{
    /// <summary>
    /// Retrieves the unique identifier of the specified player.
    /// </summary>
    /// <param name="player">The player whose ID is to be retrieved.</param>
    /// <returns>The player's ID string.</returns>
    public string GetId(Player player) => player.Id;

    /// <summary>
    /// Assigns a unique identifier to the specified player.
    /// </summary>
    /// <param name="player">The player to assign the ID to.</param>
    /// <param name="id">The ID string to assign.</param>
    public void SetId(Player player, string id) => player.SetId(id);

    /// <summary>
    /// Applies exponential damage to the player's health based on missing health.
    /// The lower the current health, the greater the damage applied.
    /// </summary>
    /// <param name="player">The player to apply damage to.</param>
    /// <param name="tickInterval">Time in seconds since the last tick.</param>
    public void ApplyExponentialDamage(Player player, float tickInterval)
    {
        float baseDamage = 5f * tickInterval;

        float missingHealth = Mathf.Max(0f, player.MaxHealth - player.Health);
        float scalingDamage = Mathf.Pow(missingHealth, 1.2f) * tickInterval;

        float totalDamage = baseDamage + scalingDamage;

        float newHealth = player.Health - totalDamage;
        player.SetHealth(Mathf.Max(0f, newHealth));
    }

    /// <summary>
    /// Applies linear recovery to the player's health.
    /// Increases health by 10% of the maximum health, without exceeding the maximum.
    /// </summary>
    /// <param name="player">The player to recover health for.</param>
    /// <param name="maxHealth">The maximum health value the player can reach.</param>
    /// <param name="tickInterval">Time in seconds since the last tick.</param>
    public void ApplyLinearRecovery(Player player, float maxHealth, float tickInterval)
    {
        float recoveryAmount = player.MaxHealth * 0.1f * tickInterval;
        float newHealth = Mathf.Min(player.MaxHealth, player.Health + recoveryAmount);
        player.SetHealth(newHealth);
    }

    /// <summary>
    /// Retrieves the current health value of the specified player.
    /// </summary>
    /// <param name="player">The player whose current health is to be retrieved.</param>
    /// <returns>The current health value of the player.</returns>
    public float GetCurrentHealth(Player player) => player.Health;

    /// <summary>
    /// Retrieves the maximum health value of the specified player.
    /// </summary>
    /// <param name="player">The player whose maximum health is to be retrieved.</param>
    /// <returns>The maximum health value of the player.</returns>
    public float GetMaxHealth(Player player) => player.MaxHealth;
}
