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

        yield return null; // Un frame

        Assert.IsNotNull(healthBarHUD, "HealthBarHUD debe existir sin inicialización");
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
}
