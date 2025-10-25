/// <summary>
/// Domain-level controller responsible for managing player state and health logic.
/// Encapsulates interactions between the Player entity and the PlayerService,
/// allowing modular and testable health updates based on movement.
/// </summary>
public class PlayerController
{
    private readonly Player _player;
    private readonly IPlayerService _playerService;

    /// <summary>
    /// Initializes a new instance of the <see cref="PlayerController"/> class
    /// with the specified player and service.
    /// </summary>
    /// <param name="player">The player entity to control.</param>
    /// <param name="playerService">The service used to manage player health and identity.</param>
    public PlayerController(Player player, IPlayerService playerService)
    {
        _player = player;
        _playerService = playerService;
    }

    /// <summary>
    /// Updates the player's health based on movement state.
    /// Applies linear recovery if moving, or exponential damage if idle.
    /// </summary>
    /// <param name="isMoving">Indicates whether the player is currently moving.</param>
    public void Tick(bool isMoving)
    {
        if (isMoving)
        {
            _playerService.ApplyLinearRecovery(_player, _player.MaxHealth);
        }
        else
        {
            _playerService.ApplyExponentialDamage(_player);
        }
    }

    /// <summary>
    /// Determines whether the player is alive.
    /// A player is considered alive if their health is greater than zero.
    /// </summary>
    /// <returns><c>true</c> if the player is alive; otherwise, <c>false</c>.</returns>
    public bool IsAlive() => _playerService.GetCurrentHealth(_player) > 0;

    /// <summary>
    /// Retrieves the current health value of the player.
    /// </summary>
    /// <returns>The player's current health.</returns>
    public int GetHealth() => _playerService.GetCurrentHealth(_player);

    /// <summary>
    /// Retrieves the maximum possible health value of the player.
    /// </summary>
    /// <returns>The player's maximum health.</returns>
    public int GetMaxHealth() => _playerService.GetMaxHealth(_player);

    /// <summary>
    /// Retrieves the unique identifier assigned to the player.
    /// </summary>
    /// <returns>The player's ID string.</returns>
    public string GetId() => _playerService.GetId(_player);
}
