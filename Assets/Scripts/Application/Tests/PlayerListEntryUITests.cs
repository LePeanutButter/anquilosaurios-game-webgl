using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TMPro;

public class PlayerListEntryUITests
{
    private PlayerListEntryUI playerListEntryUI;
    private GameObject playerListEntryObject;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI deathCountText;
    private TextMeshProUGUI roundWinsText;

    [SetUp]
    public void SetUp()
    {
        // Crear el objeto principal
        playerListEntryObject = new GameObject("PlayerListEntryUI");
        playerListEntryUI = playerListEntryObject.AddComponent<PlayerListEntryUI>();

        // Crear y asignar los TMP correctos
        var nameObject = new GameObject("NameText");
        nameText = nameObject.AddComponent<TextMeshProUGUI>();

        var deathObject = new GameObject("DeathCountText");
        deathCountText = deathObject.AddComponent<TextMeshProUGUI>();

        var winsObject = new GameObject("RoundWinsText");
        roundWinsText = winsObject.AddComponent<TextMeshProUGUI>();

        // Asignación de referencias a tu script
        playerListEntryUI.nameText = nameText;
        playerListEntryUI.deathCountText = deathCountText;
        playerListEntryUI.roundWinsText = roundWinsText;
    }

    [TearDown]
    public void TearDown()
    {
        if (playerListEntryObject != null)
            Object.DestroyImmediate(playerListEntryObject);

        if (nameText != null)
            Object.DestroyImmediate(nameText.gameObject);

        if (deathCountText != null)
            Object.DestroyImmediate(deathCountText.gameObject);

        if (roundWinsText != null)
            Object.DestroyImmediate(roundWinsText.gameObject);
    }

    [Test]
    public void TextReferences_AreAssignedCorrectly()
    {
        Assert.AreEqual(nameText, playerListEntryUI.nameText, "Referencia de texto de nombre debe asignarse");
        Assert.AreEqual(deathCountText, playerListEntryUI.deathCountText, "Referencia de texto de muertes debe asignarse");
        Assert.AreEqual(roundWinsText, playerListEntryUI.roundWinsText, "Referencia de texto de victorias debe asignarse");
    }

    [Test]
    public void Component_IsAttachedToGameObject()
    {
        Assert.AreEqual(playerListEntryObject, playerListEntryUI.gameObject, "Componente debe estar en el GameObject correcto");
    }

    [Test]
    public void Texts_CanBeModified()
    {
        nameText.text = "TestPlayer";
        deathCountText.text = "5";
        roundWinsText.text = "3";

        Assert.AreEqual("TestPlayer", nameText.text, "Texto de nombre debe poder modificarse");
        Assert.AreEqual("5", deathCountText.text, "Texto de muertes debe poder modificarse");
        Assert.AreEqual("3", roundWinsText.text, "Texto de victorias debe poder modificarse");
    }

    [Test]
    public void GameObject_HasCorrectName()
    {
        Assert.AreEqual("PlayerListEntryUI", playerListEntryUI.gameObject.name, "GameObject debe tener el nombre correcto");
    }

    [Test]
    public void Transform_IsAccessible()
    {
        Assert.IsNotNull(playerListEntryUI.transform, "Transform debe ser accesible");
    }

    [Test]
    public void Component_CanBeEnabledDisabled()
    {
        playerListEntryUI.enabled = false;
        Assert.IsFalse(playerListEntryUI.enabled, "Componente debe poder deshabilitarse");

        playerListEntryUI.enabled = true;
        Assert.IsTrue(playerListEntryUI.enabled, "Componente debe poder habilitarse");
    }

    [Test]
    public void Texts_AreNotNull()
    {
        Assert.IsNotNull(nameText, "Texto de nombre no debe ser null");
        Assert.IsNotNull(deathCountText, "Texto de muertes no debe ser null");
        Assert.IsNotNull(roundWinsText, "Texto de victorias no debe ser null");
    }

    [Test]
    public void TextComponents_AreProperlyConfigured()
    {
        Assert.IsInstanceOf<TextMeshProUGUI>(nameText, "nameText debe ser TMP_Text");
        Assert.IsInstanceOf<TextMeshProUGUI>(deathCountText, "deathCountText debe ser TMP_Text");
        Assert.IsInstanceOf<TextMeshProUGUI>(roundWinsText, "roundWinsText debe ser TMP_Text");
    }
}
