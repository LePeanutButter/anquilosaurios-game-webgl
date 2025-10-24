/// <summary>
/// Represents a player entity with an optional ID and health management.
/// </summary>
public class Player
{
    /// <summary>
    /// Unique identifier for the player. Can be null if not assigned.
    /// </summary>
    public string Id { get; private set; }

    /// <summary>
    /// Current health value of the player.
    /// </summary>
    public int Health { get; private set; }

    /// <summary>
    /// Maximum health value of the player. Always returns 100.
    /// </summary>
    public int MaxHealth => 100;

    /// <summary>
    /// Initializes a new player with default health value of 100.
    /// </summary>
    public Player() : this(100) { }

    /// <summary>
    /// Initializes a new player with a given health value.
    /// </summary>
    /// <param name="initialHealth">Initial health value.</param>
    public Player(int initialHealth)
    {
        Health = initialHealth;
    }

    /// <summary>
    /// Assigns an ID to the player.
    /// </summary>
    /// <param name="id">Player ID string.</param>
    public void SetId(string id)
    {
        Id = id;
    }

    /// <summary>
    /// Updates the player's health.
    /// </summary>
    /// <param name="value">New health value.</param>
    public void SetHealth(int value)
    {
        Health = value;
    }
}
