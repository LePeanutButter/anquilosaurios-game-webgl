using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

public class RoundInterfaceManagerTests
{
    private RoundInterfaceManager roundInterfaceManager;
    private GameObject roundInterfaceManagerObject;
    private TextMeshProUGUI countdownText;
    private TextMeshProUGUI roundsText;

    [SetUp]
    public void SetUp()
    {
        // Crear el GameObject raíz
        roundInterfaceManagerObject = new GameObject("RoundInterfaceManager");
        roundInterfaceManager = roundInterfaceManagerObject.AddComponent<RoundInterfaceManager>();

        // Crear objetos de textos usando TextMeshProUGUI
        var countdownObject = new GameObject("CountdownText");
        countdownText = countdownObject.AddComponent<TextMeshProUGUI>();

        var roundsObject = new GameObject("RoundsText");
        roundsText = roundsObject.AddComponent<TextMeshProUGUI>();

        // Asignar referencias en el script a probar
        roundInterfaceManager.countdownText = countdownText;
        roundInterfaceManager.roundsText = roundsText;
    }

    [TearDown]
    public void TearDown()
    {
        if (roundInterfaceManagerObject != null)
            Object.DestroyImmediate(roundInterfaceManagerObject);

        if (countdownText != null)
            Object.DestroyImmediate(countdownText.gameObject);

        if (roundsText != null)
            Object.DestroyImmediate(roundsText.gameObject);
    }

    [Test]
    public void UpdateCountdown_WithPositiveSeconds_ShowsCountdownText()
    {
        roundInterfaceManager.UpdateCountdown(5);

        Assert.AreEqual("Listo en 5", countdownText.text, "Texto de countdown debe mostrar segundos restantes");
    }

    [Test]
    public void UpdateCountdown_WithZeroSeconds_ShowsReadyText()
    {
        roundInterfaceManager.UpdateCountdown(0);

        Assert.AreEqual("¡Vamos!", countdownText.text, "Texto de countdown debe mostrar mensaje listo cuando llegue a 0");
    }

    [Test]
    public void UpdateCountdown_WithNegativeSeconds_ShowsReadyText()
    {
        roundInterfaceManager.UpdateCountdown(-1);

        Assert.AreEqual("¡Vamos!", countdownText.text, "Texto de countdown debe mostrar mensaje listo con números negativos");
    }

    [Test]
    public void UpdateRoundInfo_UpdatesRoundTextCorrectly()
    {
        roundInterfaceManager.UpdateRoundInfo(2, 5);

        Assert.AreEqual("Ronda 2 / 5", roundsText.text, "Texto de rondas debe mostrar ronda actual y total");
    }

    [Test]
    public void UpdateRoundInfo_WithDifferentValues_WorksCorrectly()
    {
        roundInterfaceManager.UpdateRoundInfo(1, 3);
        Assert.AreEqual("Ronda 1 / 3", roundsText.text, "Primera actualización debe funcionar");

        roundInterfaceManager.UpdateRoundInfo(3, 3);
        Assert.AreEqual("Ronda 3 / 3", roundsText.text, "Segunda actualización debe funcionar");
    }

    [Test]
    public void TextReferences_AreAssignedCorrectly()
    {
        Assert.AreEqual(countdownText, roundInterfaceManager.countdownText, "Referencia de countdown debe asignarse");
        Assert.AreEqual(roundsText, roundInterfaceManager.roundsText, "Referencia de rounds debe asignarse");
    }

    [Test]
    public void Component_HasNetworkBehaviour()
    {
        Assert.IsNotNull(roundInterfaceManager as Unity.Netcode.NetworkBehaviour, "RoundInterfaceManager debe heredar de NetworkBehaviour");
    }

    [Test]
    public void Component_IsAttachedToCorrectGameObject()
    {
        Assert.AreEqual(roundInterfaceManagerObject, roundInterfaceManager.gameObject, "Componente debe estar en el GameObject correcto");
    }

    [Test]
    public void GameObject_HasCorrectName()
    {
        Assert.AreEqual("RoundInterfaceManager", roundInterfaceManager.gameObject.name, "GameObject debe tener el nombre correcto");
    }

    [Test]
    public void Texts_AreNotNull()
    {
        Assert.IsNotNull(countdownText, "CountdownText no debe ser null");
        Assert.IsNotNull(roundsText, "RoundsText no debe ser null");
    }

    [Test]
    public void Component_CanBeEnabledDisabled()
    {
        roundInterfaceManager.enabled = false;
        Assert.IsFalse(roundInterfaceManager.enabled, "Componente debe poder deshabilitarse");

        roundInterfaceManager.enabled = true;
        Assert.IsTrue(roundInterfaceManager.enabled, "Componente debe poder habilitarse");
    }

    [Test]
    public void Transform_IsAccessible()
    {
        Assert.IsNotNull(roundInterfaceManager.transform, "Transform debe ser accesible");
    }

    [Test]
    public void UpdateCountdown_HandlesLargeNumbers()
    {
        roundInterfaceManager.UpdateCountdown(999);

        Assert.AreEqual("Listo en 999", countdownText.text, "Debe manejar números grandes correctamente");
    }

    [Test]
    public void UpdateRoundInfo_HandlesEdgeCases()
    {
        roundInterfaceManager.UpdateRoundInfo(0, 1);
        Assert.AreEqual("Ronda 0 / 1", roundsText.text, "Debe manejar ronda 0");

        roundInterfaceManager.UpdateRoundInfo(1, 0);
        Assert.AreEqual("Ronda 1 / 0", roundsText.text, "Debe manejar total de rondas 0");
    }
}
