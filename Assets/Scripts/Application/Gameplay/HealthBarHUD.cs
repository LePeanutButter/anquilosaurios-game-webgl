using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's health bar UI element.
/// Updates the slider to reflect current health and provides methods to set health values.
/// </summary>
public class HealthBarHUD : MonoBehaviour
{
    #region Public Fields

    public Slider healthBar;
    public float maxHealth;
    public float currentHealth;

    #endregion

    #region Private Fields

    private PlayerPresenter _player;
    private bool _initialized = false;
    private float _visualValue = 1f;

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Unity callback invoked on script start.
    /// Validates the health bar reference and initializes current health.
    /// </summary>
    private void Awake()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>();
            if (healthBar != null)
            {
                Debug.Log($"HealthBarHUD: Slider auto-assigned in {gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Unity callback invoked once per frame.
    /// Updates the health bar slider to reflect the current health percentage.
    /// </summary>
    void Update()
    {
        if (!_initialized || healthBar == null || _player == null) return;

        currentHealth = _player.CurrentHealth;
        _visualValue = currentHealth / _player.MaxHealth;
        healthBar.value = _visualValue;
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Initializes the health bar with the player's health values.
    /// This method sets up the maximum health, current health, and visual value.
    /// </summary>
    /// <param name="player">The PlayerPresenter instance that contains the player's health data.</param>
    public void Initialize(PlayerPresenter player)
    {
        _player = player;
        maxHealth = player.MaxHealth;
        currentHealth = player.CurrentHealth;
        _visualValue = currentHealth / maxHealth;
        _initialized = true;
        enabled = true;
    }

    #endregion
}
