using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the player's health bar UI element.
/// Updates the slider to reflect current health and provides methods to set health values.
/// </summary>
public class HealthBarHUD : MonoBehaviour
{
    public Slider healthBar;
    public int maxHealth;
    public int currentHealth;
    private bool _initialized = false;
    private float _visualValue = 1f;
    private float _lerpSpeed = 5f;

    private void Awake()
    {
        if (healthBar == null)
        {
            healthBar = GetComponentInChildren<Slider>();
            if (healthBar != null)
            {
                Debug.Log($"HealthBarHUD: Slider auto-asignado en {gameObject.name}");
            }
        }
    }

    /// <summary>
    /// Unity callback invoked on script start.
    /// Validates the health bar reference and initializes current health.
    /// </summary>
    public void Initialize()
    {
        if (healthBar == null)
        {
            Debug.LogError($"HealthBarHUD.Initialize: Slider no asignado en prefab {gameObject.name}. No se puede inicializar HUD.");
            enabled = false;
            _initialized = false;
            return;
        }

        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;
        healthBar.value = 1f;

        _initialized = true;
        enabled = true;
    }

    /// <summary>
    /// Unity callback invoked once per frame.
    /// Updates the health bar slider to reflect the current health percentage.
    /// </summary>
    void Update()
    {
        if (!_initialized || healthBar == null || maxHealth == 0)
            return;

        float currentSliderValue = healthBar.value;
        float targetValue = _visualValue;

        healthBar.value = Mathf.Lerp(currentSliderValue, targetValue, Time.deltaTime * _lerpSpeed);
    }

    /// <summary>
    /// Sets the current and maximum health values, and updates the health bar accordingly.
    /// </summary>
    /// <param name="current">The current health value.</param>
    /// <param name="max">The maximum health value.</param>
    public void SetHealth(int current, int max)
    {
        if (!_initialized || healthBar == null)
            return;

        if (max <= 0) max = 1;
        maxHealth = max;
        currentHealth = Mathf.Clamp(current, 0, maxHealth);

        _visualValue = (float)currentHealth / maxHealth;
    }
}
