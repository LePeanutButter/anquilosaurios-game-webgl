using System;

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
    public void ApplyExponentialDamage(Player player)
    {
        int baseDamage = 5;
        int missingHealth = Math.Max(0, player.MaxHealth - player.Health);
        int scalingDamage = (int)Math.Round(Math.Pow(missingHealth, 1.2));
        int totalDamage = baseDamage + scalingDamage;
        int newHealth = player.Health - totalDamage;
        player.SetHealth(Math.Max(0, newHealth));
    }

    /// <summary>
    /// Applies linear recovery to the player's health.
    /// Increases health by 10% of the maximum health, without exceeding the maximum.
    /// </summary>
    /// <param name="player">The player to recover health for.</param>
    /// <param name="maxHealth">The maximum health value the player can reach.</param>
    public void ApplyLinearRecovery(Player player, int maxHealth)
    {
        int recovery = (int)Math.Round(maxHealth * 0.1);
        player.SetHealth(Math.Min(maxHealth, player.Health + recovery));
    }

    /// <summary>
    /// Retrieves the current health value of the specified player.
    /// </summary>
    /// <param name="player">The player whose current health is to be retrieved.</param>
    /// <returns>The current health value of the player.</returns>
    public int GetCurrentHealth(Player player) => player.Health;

    /// <summary>
    /// Retrieves the maximum health value of the specified player.
    /// </summary>
    /// <param name="player">The player whose maximum health is to be retrieved.</param>
    /// <returns>The maximum health value of the player.</returns>
    public int GetMaxHealth(Player player) => player.MaxHealth;
}
