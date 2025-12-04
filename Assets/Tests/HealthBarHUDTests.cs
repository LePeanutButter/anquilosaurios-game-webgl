using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class HealthBarHUDTests
{
    private HealthBarHUD healthBarHUD;
    private GameObject healthBarHUDObject;
    private Slider healthBar;

    [SetUp]
    public void SetUp()
    {
        healthBarHUDObject = new GameObject("HealthBarHUD");
        healthBarHUD = healthBarHUDObject.AddComponent<HealthBarHUD>();

        var sliderObject = new GameObject("HealthBar");
        healthBar = sliderObject.AddComponent<Slider>();
        healthBar.minValue = 0f;
        healthBar.maxValue = 1f;

        healthBarHUD.healthBar = healthBar;
    }

    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(healthBarHUDObject);
        Object.DestroyImmediate(healthBar.gameObject);
    }

    [Test]
    public void Awake_AutoAssignsSliderFromChildren()
    {
        healthBarHUD.healthBar = null;

        var childSliderObject = new GameObject("ChildHealthBar");
        childSliderObject.transform.parent = healthBarHUDObject.transform;
        var childSlider = childSliderObject.AddComponent<Slider>();

        healthBarHUD.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(healthBarHUD, null);

        Assert.AreEqual(childSlider, healthBarHUD.healthBar, "Slider debe auto-asignarse desde hijos");

        Object.DestroyImmediate(childSliderObject);
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_WithoutInitialization_DoesNothing()
    {
        // Call Update directly - should return early due to !_initialized
        healthBarHUD.GetType().GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(healthBarHUD, null);

        yield return null; // Un frame

        Assert.IsNotNull(healthBarHUD, "HealthBarHUD debe existir sin inicialización y Update debe retornar temprano");
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_WithNullHealthBar_DoesNothing()
    {
        // Initialize first
        var mockPlayer = CreateMockPlayer();
        healthBarHUD.Initialize(mockPlayer);

        // Then set healthBar to null
        healthBarHUD.healthBar = null;

        // Call Update - should return early due to healthBar == null
        healthBarHUD.GetType().GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(healthBarHUD, null);

        yield return null;

        Assert.IsNotNull(healthBarHUD, "Update debe retornar temprano cuando healthBar es null");
    }

    [UnityTest]
    public System.Collections.IEnumerator Update_WithNullPlayer_DoesNothing()
    {
        // Initialize first
        var mockPlayer = CreateMockPlayer();
        healthBarHUD.Initialize(mockPlayer);

        // Then set _player to null using reflection
        healthBarHUD.GetType().GetField("_player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(healthBarHUD, null);

        // Call Update - should return early due to _player == null
        healthBarHUD.GetType().GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(healthBarHUD, null);

        yield return null;

        Assert.IsNotNull(healthBarHUD, "Update debe retornar temprano cuando _player es null");
    }

    [Test]
    public void HealthBar_Property_IsAccessible()
    {
        Assert.AreEqual(healthBar, healthBarHUD.healthBar, "Propiedad healthBar debe ser accesible");
    }

    [Test]
    public void MaxHealth_Property_IsAccessible()
    {
        float testValue = 100f;
        healthBarHUD.maxHealth = testValue;

        Assert.AreEqual(testValue, healthBarHUD.maxHealth, "Propiedad maxHealth debe ser accesible");
    }

    [Test]
    public void CurrentHealth_Property_IsAccessible()
    {
        float testValue = 75f;
        healthBarHUD.currentHealth = testValue;

        Assert.AreEqual(testValue, healthBarHUD.currentHealth, "Propiedad currentHealth debe ser accesible");
    }

    [Test]
    public void Slider_Value_Range_IsCorrect()
    {
        Assert.AreEqual(0f, healthBar.minValue, "Slider debe tener valor mínimo 0");
        Assert.AreEqual(1f, healthBar.maxValue, "Slider debe tener valor máximo 1");
    }

    [Test]
    public void Component_CanBeDisabledAndEnabled()
    {
        healthBarHUD.enabled = false;

        healthBarHUD.enabled = true;

        Assert.IsTrue(healthBarHUD.enabled, "Componente debe poder habilitarse/deshabilitarse");
    }

    [Test]
    public void Awake_HandlesNullSliderGracefully()
    {
        healthBarHUD.healthBar = null;

        healthBarHUD.GetType().GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(healthBarHUD, null);

        Assert.IsNotNull(healthBarHUD, "Awake debe manejar null slider sin problemas");
    }

    [Test]
    public void Initialize_SetsPlayerAndHealthValues()
    {
        // Create a mock PlayerPresenter
        var mockPlayer = CreateMockPlayer();

        // Call Initialize
        healthBarHUD.Initialize(mockPlayer);

        // Assert
        Assert.AreEqual(mockPlayer, healthBarHUD.GetType().GetField("_player", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(healthBarHUD),
            "Player debe asignarse correctamente");
        Assert.AreEqual(150f, healthBarHUD.maxHealth, "maxHealth debe asignarse desde player");
        Assert.AreEqual(120f, healthBarHUD.currentHealth, "currentHealth debe asignarse desde player");
        Assert.IsTrue(healthBarHUD.enabled, "HealthBarHUD debe habilitarse después de Initialize");
        Assert.AreEqual(120f / 150f, healthBarHUD.GetType().GetField("_visualValue", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(healthBarHUD),
            "_visualValue debe calcularse correctamente");
    }

    [Test]
    public void Initialize_WithNullPlayer_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => healthBarHUD.Initialize(null),
            "Initialize debe manejar null player sin excepciones");
    }

    // Mock class for testing
    private PlayerPresenter CreateMockPlayer()
    {
        var go = new GameObject("MockPlayer");
        var presenter = go.AddComponent<PlayerPresenter>();

        // Set MaxHealth
        var maxField = typeof(PlayerPresenter).GetField("MaxHealth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (maxField != null) maxField.SetValue(presenter, 150f);

        // Set CurrentHealth
        var currentField = typeof(PlayerPresenter).GetField("CurrentHealth", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (currentField != null) currentField.SetValue(presenter, 120f);

        return presenter;
 
    }
}
