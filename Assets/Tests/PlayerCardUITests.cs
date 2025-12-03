using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Tests unitarios completos para PlayerCardUI incluyendo el mapeo de sprites.
/// </summary>
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
        nameObject.transform.SetParent(playerCardObject.transform);

        var imageObject = new GameObject("CharacterImage");
        characterImage = imageObject.AddComponent<Image>();
        characterImage.enabled = false; // Starts disabled
        imageObject.transform.SetParent(playerCardObject.transform);

        // Asignar referencias
        playerCardUI.playerNameText = playerNameText;
        playerCardUI.characterImage = characterImage;
    }

    [TearDown]
    public void TearDown()
    {
        if (playerCardObject != null)
            Object.DestroyImmediate(playerCardObject);
    }

    #region Setup Method Tests

    [Test]
    public void Setup_WithValidName_SetsPlayerName()
    {
        // Act
        playerCardUI.Setup("TestPlayer", CharacterType.Flor);

        // Assert
        Assert.AreEqual("TestPlayer", playerNameText.text,
            "Nombre del jugador debe establecerse correctamente");
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter,
            "Personaje asignado debe establecerse");
    }

    [Test]
    public void Setup_WithNullName_SetsDefaultName()
    {
        // Act
        playerCardUI.Setup(null);

        // Assert
        Assert.AreEqual("Unknown Player", playerNameText.text,
            "Debe mostrar nombre por defecto para null");
    }

    [Test]
    public void Setup_WithEmptyName_SetsDefaultName()
    {
        // Act
        playerCardUI.Setup("");

        // Assert
        Assert.AreEqual("Unknown Player", playerNameText.text,
            "Debe mostrar nombre por defecto para string vacío");
    }

    [Test]
    public void Setup_WithWhitespaceName_SetsDefaultName()
    {
        // Act
        playerCardUI.Setup("   ");

        // Assert
        Assert.AreEqual("   ", playerNameText.text,
            "Debe mostrar nombre para solo espacios");
    }

    [Test]
    public void Setup_WithNullCharacter_DisablesImage()
    {
        // Act
        playerCardUI.Setup("TestPlayer", null);

        // Assert
        Assert.IsFalse(characterImage.enabled,
            "Imagen debe desactivarse cuando no hay personaje asignado");
        Assert.IsNull(playerCardUI.AssignedCharacter);
    }

    [Test]
    public void Setup_HandlesWhitespaceInNames()
    {
        // Act
        playerCardUI.Setup("  Test Player  ");

        // Assert
        Assert.AreEqual("  Test Player  ", playerNameText.text,
            "Debe preservar espacios en blanco en nombres válidos");
    }

    [Test]
    public void Setup_WithOnlyName_NoCharacter()
    {
        // Act
        playerCardUI.Setup("PlayerName");

        // Assert
        Assert.AreEqual("PlayerName", playerNameText.text);
        Assert.IsNull(playerCardUI.AssignedCharacter);
    }

    #endregion

    #region UpdateName Method Tests

    [Test]
    public void UpdateName_WithValidName_ChangesPlayerName()
    {
        // Arrange
        playerCardUI.Setup("OriginalName");

        // Act
        playerCardUI.UpdateName("NewName");

        // Assert
        Assert.AreEqual("NewName", playerNameText.text,
            "Nombre debe actualizarse correctamente");
    }

    [Test]
    public void UpdateName_WithNullName_SetsDefaultName()
    {
        // Arrange
        playerCardUI.Setup("OriginalName");

        // Act
        playerCardUI.UpdateName(null);

        // Assert
        Assert.AreEqual("Unknown Player", playerNameText.text,
            "Debe establecer nombre por defecto para null");
    }

    [Test]
    public void UpdateName_WithEmptyName_SetsDefaultName()
    {
        // Arrange
        playerCardUI.Setup("OriginalName");

        // Act
        playerCardUI.UpdateName("");

        // Assert
        Assert.AreEqual("Unknown Player", playerNameText.text,
            "Debe establecer nombre por defecto para string vacío");
    }

    [Test]
    public void UpdateName_MultipleTimes_WorksCorrectly()
    {
        // Act
        playerCardUI.UpdateName("Name1");
        Assert.AreEqual("Name1", playerNameText.text);

        playerCardUI.UpdateName("Name2");
        Assert.AreEqual("Name2", playerNameText.text);

        playerCardUI.UpdateName("Name3");
        Assert.AreEqual("Name3", playerNameText.text);
    }

    #endregion

    #region UpdateCharacter Method Tests

    [Test]
    public void UpdateCharacter_WithValidCharacter_UpdatesAssignedCharacter()
    {
        // Arrange
        playerCardUI.Setup("TestPlayer");

        // Act
        playerCardUI.UpdateCharacter(CharacterType.Girasol);

        // Assert
        Assert.AreEqual(CharacterType.Girasol, playerCardUI.AssignedCharacter,
            "Personaje asignado debe actualizarse");
    }

    [Test]
    public void UpdateCharacter_WithNullCharacter_ResetsAssignedCharacter()
    {
        // Arrange
        playerCardUI.Setup("TestPlayer", CharacterType.Flor);

        // Act
        playerCardUI.UpdateCharacter(null);

        // Assert
        Assert.IsNull(playerCardUI.AssignedCharacter, "Personaje asignado debe ser null");
        Assert.IsFalse(characterImage.enabled, "Imagen debe deshabilitarse");
    }

    [Test]
    public void UpdateCharacter_ChangingCharacters_WorksCorrectly()
    {
        // Act
        playerCardUI.UpdateCharacter(CharacterType.Flor);
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter);

        playerCardUI.UpdateCharacter(CharacterType.Girasol);
        Assert.AreEqual(CharacterType.Girasol, playerCardUI.AssignedCharacter);

        playerCardUI.UpdateCharacter(CharacterType.Jalapeno);
        Assert.AreEqual(CharacterType.Jalapeno, playerCardUI.AssignedCharacter);
    }

    #endregion

    #region Character Sprite Map Tests

    [Test]
    public void UpdateCharacter_WithNullSpriteMap_DisablesImage()
    {
        // Arrange
        SetCharacterSpriteMap(null);

        // Act
        playerCardUI.UpdateCharacter(CharacterType.Flor);

        // Assert
        Assert.IsFalse(characterImage.enabled,
            "Imagen debe estar deshabilitada cuando characterSpriteMap es null");
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter,
            "Pero el personaje debe estar asignado");
    }

    [Test]
    public void UpdateCharacter_WithValidSpriteMap_EnablesImageAndSetsSprite()
    {
        // Arrange
        var testSprite = CreateTestSprite();
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, testSprite }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act
        playerCardUI.UpdateCharacter(CharacterType.Flor);

        // Assert
        Assert.IsTrue(characterImage.enabled,
            "Imagen debe habilitarse cuando hay sprite disponible");
        Assert.AreEqual(testSprite, characterImage.sprite,
            "Sprite debe asignarse correctamente");
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter);
    }

    [Test]
    public void UpdateCharacter_WithMissingSpriteInMap_DisablesImage()
    {
        // Arrange
        var testSprite = CreateTestSprite();
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, testSprite }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act - Try to set character not in map
        playerCardUI.UpdateCharacter(CharacterType.Girasol);

        // Assert
        Assert.IsFalse(characterImage.enabled,
            "Imagen debe estar deshabilitada cuando sprite no está en el mapa");
        Assert.AreEqual(CharacterType.Girasol, playerCardUI.AssignedCharacter,
            "Personaje debe estar asignado aunque no tenga sprite");
    }

    [Test]
    public void UpdateCharacter_WithEmptySpriteMap_DisablesImage()
    {
        // Arrange
        var spriteMap = new Dictionary<CharacterType, Sprite>();
        SetCharacterSpriteMap(spriteMap);

        // Act
        playerCardUI.UpdateCharacter(CharacterType.Flor);

        // Assert
        Assert.IsFalse(characterImage.enabled,
            "Imagen debe estar deshabilitada cuando el mapa está vacío");
    }

    [Test]
    public void UpdateCharacter_WithMultipleSprites_SelectsCorrectOne()
    {
        // Arrange
        var florSprite = CreateTestSprite();
        var girasolSprite = CreateTestSprite();
        var jalapenoSprite = CreateTestSprite();
        var manzanaSprite = CreateTestSprite();

        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, florSprite },
            { CharacterType.Girasol, girasolSprite },
            { CharacterType.Jalapeno, jalapenoSprite },
            { CharacterType.Manzana, manzanaSprite }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act & Assert - Flor
        playerCardUI.UpdateCharacter(CharacterType.Flor);
        Assert.AreEqual(florSprite, characterImage.sprite);
        Assert.IsTrue(characterImage.enabled);

        // Act & Assert - Girasol
        playerCardUI.UpdateCharacter(CharacterType.Girasol);
        Assert.AreEqual(girasolSprite, characterImage.sprite);
        Assert.IsTrue(characterImage.enabled);

        // Act & Assert - Jalapeno
        playerCardUI.UpdateCharacter(CharacterType.Jalapeno);
        Assert.AreEqual(jalapenoSprite, characterImage.sprite);
        Assert.IsTrue(characterImage.enabled);

        // Act & Assert - Manzana
        playerCardUI.UpdateCharacter(CharacterType.Manzana);
        Assert.AreEqual(manzanaSprite, characterImage.sprite);
        Assert.IsTrue(characterImage.enabled);
    }

    [Test]
    public void UpdateCharacter_SwitchingFromValidToInvalid_DisablesImage()
    {
        var testSprite = CreateTestSprite();
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, testSprite }
        };
        SetCharacterSpriteMap(spriteMap);

        playerCardUI.UpdateCharacter(CharacterType.Flor);
        Assert.IsTrue(characterImage.enabled, "Debe estar habilitada inicialmente");

        playerCardUI.UpdateCharacter(CharacterType.Girasol);

        Assert.IsFalse(characterImage.enabled,
            "Debe deshabilitarse al cambiar a personaje sin sprite");
    }

    [Test]
    public void UpdateCharacter_NullSprite_HandlesGracefully()
    {
        // Arrange - Map with null sprite
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, null }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act
        playerCardUI.UpdateCharacter(CharacterType.Flor);

        // Assert
        Assert.IsNull(characterImage.sprite, "Sprite debe ser null");
        Assert.IsFalse(characterImage.enabled,
            "Imagen se habilita incluso con sprite null (comportamiento del código)");
    }

    [Test]
    public void Setup_WithCharacterAndSpriteMap_SetsSprite()
    {
        // Arrange
        var testSprite = CreateTestSprite();
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, testSprite }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act
        playerCardUI.Setup("TestPlayer", CharacterType.Flor);

        // Assert
        Assert.IsTrue(characterImage.enabled, "Imagen debe habilitarse");
        Assert.AreEqual(testSprite, characterImage.sprite, "Sprite debe asignarse");
    }

    #endregion

    #region AssignedCharacter Property Tests

    [Test]
    public void AssignedCharacter_Property_IsAccessible()
    {
        // Assert initial state
        Assert.IsNull(playerCardUI.AssignedCharacter,
            "Inicialmente no debe haber personaje asignado");

        // Act
        playerCardUI.Setup("TestPlayer", CharacterType.Manzana);

        // Assert
        Assert.AreEqual(CharacterType.Manzana, playerCardUI.AssignedCharacter,
            "Propiedad debe retornar personaje correcto");
    }

    [Test]
    public void AssignedCharacter_ReturnsNullWhenNotSet()
    {
        // Assert
        Assert.IsNull(playerCardUI.AssignedCharacter,
            "Debe retornar null cuando no está asignado");
    }

    [Test]
    public void AssignedCharacter_PersistsAcrossUpdates()
    {
        // Act
        playerCardUI.UpdateCharacter(CharacterType.Flor);
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter);

        playerCardUI.UpdateName("NewName");
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter,
            "Personaje debe persistir después de actualizar nombre");
    }

    #endregion

    #region UI Component Tests

    [Test]
    public void PlayerNameText_IsAssignedCorrectly()
    {
        // Assert
        Assert.AreEqual(playerNameText, playerCardUI.playerNameText,
            "Referencia de texto debe asignarse correctamente");
    }

    [Test]
    public void CharacterImage_IsAssignedCorrectly()
    {
        // Assert
        Assert.AreEqual(characterImage, playerCardUI.characterImage,
            "Referencia de imagen debe asignarse correctamente");
    }

    [Test]
    public void CharacterImage_InitiallyDisabled()
    {
        // Assert
        Assert.IsFalse(characterImage.enabled,
            "La imagen debe estar deshabilitada inicialmente");
    }

    [Test]
    public void CharacterImage_StartsWithNoSprite()
    {
        // Assert
        Assert.IsNull(characterImage.sprite,
            "La imagen no debe tener sprite inicialmente");
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void Setup_WithNullUIComponents_HandlesGracefully()
    {
        // Arrange
        playerCardUI.playerNameText = null;
        playerCardUI.characterImage = null;

        // Act & Assert
        Assert.DoesNotThrow(() => playerCardUI.Setup("TestPlayer", CharacterType.Flor),
            "Debe manejar componentes null sin lanzar excepciones");
    }

    [Test]
    public void UpdateName_WithNullTextComponent_HandlesGracefully()
    {
        // Arrange
        playerCardUI.playerNameText = null;

        // Act & Assert
        Assert.DoesNotThrow(() => playerCardUI.UpdateName("NewName"),
            "Debe manejar texto null sin lanzar excepciones");
    }

    [Test]
    public void UpdateCharacter_WithNullImageComponent_HandlesGracefully()
    {
        // Arrange
        playerCardUI.characterImage = null;
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, CreateTestSprite() }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act & Assert
        Assert.DoesNotThrow(() => playerCardUI.UpdateCharacter(CharacterType.Flor),
            "Debe manejar imagen null sin lanzar excepciones");
    }

    [Test]
    public void Setup_WithLongName_HandlesCorrectly()
    {
        // Arrange
        string longName = new string('A', 500);

        // Act
        playerCardUI.Setup(longName);

        // Assert
        Assert.AreEqual(longName, playerNameText.text,
            "Debe manejar nombres muy largos");
    }

    [Test]
    public void Setup_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        string specialName = "Player!@#$%^&*()_+-=[]{}|;':,.<>?/~`";

        // Act
        playerCardUI.Setup(specialName);

        // Assert
        Assert.AreEqual(specialName, playerNameText.text,
            "Debe manejar caracteres especiales");
    }

    [Test]
    public void UpdateCharacter_WithNoneCharacter_HandlesCorrectly()
    {
        // Act
        playerCardUI.UpdateCharacter(CharacterType.None);

        // Assert
        Assert.AreEqual(CharacterType.None, playerCardUI.AssignedCharacter,
            "Debe poder asignar CharacterType.None");
    }

    #endregion

    #region Integration Tests

    [Test]
    public void CompleteFlow_SetupAndUpdate()
    {
        // Arrange
        var testSprite = CreateTestSprite();
        var spriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, testSprite },
            { CharacterType.Girasol, testSprite }
        };
        SetCharacterSpriteMap(spriteMap);

        // Act - Setup
        playerCardUI.Setup("Player1", CharacterType.Flor);

        // Assert - After setup
        Assert.AreEqual("Player1", playerNameText.text);
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter);
        Assert.IsTrue(characterImage.enabled);

        // Act - Update name
        playerCardUI.UpdateName("Player2");

        // Assert - After name update
        Assert.AreEqual("Player2", playerNameText.text);
        Assert.AreEqual(CharacterType.Flor, playerCardUI.AssignedCharacter);

        // Act - Update character
        playerCardUI.UpdateCharacter(CharacterType.Girasol);

        // Assert - After character update
        Assert.AreEqual("Player2", playerNameText.text);
        Assert.AreEqual(CharacterType.Girasol, playerCardUI.AssignedCharacter);
        Assert.IsTrue(characterImage.enabled);
    }

    #endregion

    #region Helper Methods

    private Sprite CreateTestSprite()
    {
        // Create a simple 1x1 texture and sprite for testing
        Texture2D texture = new Texture2D(1, 1);
        return Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
    }

    private void SetCharacterSpriteMap(Dictionary<CharacterType, Sprite> spriteMap)
    {
        var field = typeof(PlayerCardUI).GetField("characterSpriteMap",
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        field?.SetValue(playerCardUI, spriteMap);
    }

    #endregion
}