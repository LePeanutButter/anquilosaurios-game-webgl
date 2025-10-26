/// <summary>
/// Defines a contract for managing player-related operations such as identification and health adjustments.
/// </summary>
public interface IPlayerService
{
    /// <summary>
    /// Retrieves the unique identifier of the specified player.
    /// </summary>
    /// <param name="player">The player whose ID is to be retrieved.</param>
    /// <returns>The ID string of the player.</returns>
    string GetId(Player player);

    /// <summary>
    /// Assigns a unique identifier to the specified player.
    /// </summary>
    /// <param name="player">The player to assign the ID to.</param>
    /// <param name="id">The ID string to assign.</param>
    void SetId(Player player, string id);

    /// <summary>
    /// Applies exponential damage to the player's health.
    /// The damage increases exponentially based on a predefined formula or condition.
    /// </summary>
    /// <param name="player">The player to apply damage to.</param>
    /// <param name="tickInterval">Time in seconds since the last tick.</param>
    void ApplyExponentialDamage(Player player, float tickInterval);

    /// <summary>
    /// Applies linear recovery to the player's health.
    /// The recovery increases health in a linear fashion up to a specified maximum.
    /// </summary>
    /// <param name="player">The player to recover health for.</param>
    /// <param name="maxHealth">The maximum health value the player can reach.</param>
    /// <param name="tickInterval">Time in seconds since the last tick.</param>
    void ApplyLinearRecovery(Player player, float maxHealth, float tickInterval);

    /// <summary>
    /// Retrieves the current health value of the specified player.
    /// </summary>
    /// <param name="player">The player whose current health is to be retrieved.</param>
    /// <returns>The current health value of the player.</returns>
    float GetCurrentHealth(Player player);

    /// <summary>
    /// Retrieves the maximum health value of the specified player.
    /// </summary>
    /// <param name="player">The player whose maximum health is to be retrieved.</param>
    /// <returns>The maximum health value of the player.</returns>
    float GetMaxHealth(Player player);
}
