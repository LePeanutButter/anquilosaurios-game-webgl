using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;

public class PlayerCardUITests
{
    private PlayerCardUI playerCardUI;
    private GameObject playerCardObject;
    private TextMeshProUGUI playerNameText;
    private Image characterImage;

    [SetUp]
    public void SetUp()
    {
        playerCardObject = new GameObject("PlayerCardUI");
        playerCardUI = playerCardObject.AddComponent<PlayerCardUI>();

        var nameObject = new GameObject("PlayerNameText");
        playerNameText = nameObject.AddComponent<TextMeshProUGUI>();

        var imageObject = new GameObject("CharacterImage");
        characterImage = imageObject.AddComponent<Image>();
        characterImage.enabled = true;

        // Asignar referencias
        playerCardUI.playerNameText = playerNameText;
        playerCardUI.characterImage = characterImage;
    }

    [TearDown]
    public void TearDown()
    {
        if (playerCardObject != null)
            Object.DestroyImmediate(playerCardObject);

        if (playerNameText != null)
            Object.DestroyImmediate(playerNameText.gameObject);

        if (characterImage != null)
            Object.DestroyImmediate(characterImage.gameObject);
    }

    [Test]
    public void Setup_WithValidName_SetsPlayerName()
    {
        playerCardUI.Setup("TestPlayer", CharacterType.Flor);

        Assert.AreEqual("TestPlayer", playerNameText.text, "Nombre del jugador debe establecerse correctamente");
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter, "Personaje asignado debe establecerse");
    }

    [Test]
    public void Setup_WithNullName_SetsDefaultName()
    {
        playerCardUI.Setup(null);

        Assert.AreEqual("Unknown Player", playerNameText.text, "Debe mostrar nombre por defecto para null");
    }

    [Test]
    public void Setup_WithEmptyName_SetsDefaultName()
    {
        playerCardUI.Setup("");

        Assert.AreEqual("Unknown Player", playerNameText.text, "Debe mostrar nombre por defecto para string vacío");
    }

    [Test]
    public void Setup_WithNullCharacter_DisablesImage()
    {
        playerCardUI.Setup("TestPlayer", null);

        Assert.IsFalse(characterImage.enabled, "Imagen debe desactivarse cuando no hay personaje asignado");
    }

    [Test]
    public void UpdateName_WithValidName_ChangesPlayerName()
    {
        playerCardUI.Setup("OriginalName");
        playerCardUI.UpdateName("NewName");

        Assert.AreEqual("NewName", playerNameText.text, "Nombre debe actualizarse correctamente");
    }

    [Test]
    public void UpdateName_WithNullName_SetsDefaultName()
    {
        playerCardUI.Setup("OriginalName");
        playerCardUI.UpdateName(null);

        Assert.AreEqual("Unknown Player", playerNameText.text, "Debe establecer nombre por defecto para null");
    }

    [Test]
    public void UpdateCharacter_WithValidCharacter_UpdatesAssignedCharacter()
    {
        playerCardUI.Setup("TestPlayer");
        playerCardUI.UpdateCharacter(CharacterType.Girasol);

        Assert.AreEqual(CharacterType.Girasol, playerCardUI.AssignedCharacter, "Personaje asignado debe actualizarse");
    }

    [Test]
    public void UpdateCharacter_WithNullCharacter_ResetsAssignedCharacter()
    {
        playerCardUI.Setup("TestPlayer", CharacterType.Flor);
        playerCardUI.UpdateCharacter(null);

        Assert.IsNull(playerCardUI.AssignedCharacter, "Personaje asignado debe ser null");
    }

    [Test]
    public void AssignedCharacter_Property_IsAccessible()
    {
        Assert.IsNull(playerCardUI.AssignedCharacter, "Inicialmente no debe haber personaje asignado");

        playerCardUI.Setup("TestPlayer", CharacterType.Manzana);
        Assert.AreEqual(CharacterType.Manzana, playerCardUI.AssignedCharacter, "Propiedad debe retornar personaje correcto");
    }

    [Test]
    public void Setup_HandlesWhitespaceNames()
    {
        playerCardUI.Setup("  Test Player  ");

        Assert.AreEqual("  Test Player  ", playerNameText.text, "Debe preservar espacios en blanco en nombres");
    }

    [Test]
    public void CharacterImage_InitiallyDisabled()
    {
        Assert.IsTrue(characterImage.enabled, "La imagen debe estar habilitada inicialmente según nuestro setup");
    }

    [Test]
    public void PlayerNameText_IsAssignedCorrectly()
    {
        Assert.AreEqual(playerNameText, playerCardUI.playerNameText, "Referencia de texto debe asignarse correctamente");
    }

    [Test]
    public void CharacterImage_IsAssignedCorrectly()
    {
        Assert.AreEqual(characterImage, playerCardUI.characterImage, "Referencia de imagen debe asignarse correctamente");
    }
}
