using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the UI representation of a player's card, including player name and character sprite.
/// </summary>
public class PlayerCardUI : MonoBehaviour
{
    #region Public Fields

    [Header("UI References")]
    public TMP_Text playerNameText;
    public Image characterImage;

    [Header("Character Sprites")]
    public Sprite florSprite;
    public Sprite girasolSprite;
    public Sprite jalapenoSprite;
    public Sprite manzanaSprite;

    #endregion

    #region Private Fields

    private Dictionary<CharacterType, Sprite> characterSpriteMap;

    #endregion

    #region Public Properties

    /// <summary>
    /// The currently assigned character for this player card.
    /// Can be null if no character is assigned.
    /// </summary>
    [HideInInspector]
    public CharacterType? AssignedCharacter { get; private set; }

    #endregion

    #region Unity Callbacks

    /// <summary>
    /// Initializes the character sprite map and disables the character image initially.
    /// </summary>
    private void Awake()
    {
        EnsureCharacterSpriteMap();
        if (characterImage != null) characterImage.enabled = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Sets up the player card with the player's name and optional assigned character.
    /// </summary>
    /// <param name="playerName">The name of the player. If empty or null, displays "Unknown Player".</param>
    /// <param name="assignedCharacter">Optional character assigned to this player.</param>
    public void Setup(string playerName, CharacterType? assignedCharacter = null)
    {
        if (playerNameText != null)
            playerNameText.text = string.IsNullOrEmpty(playerName) ? "Unknown Player" : playerName;

        AssignedCharacter = assignedCharacter;

        RefreshCharacterSprite();
    }

    /// <summary>
    /// Updates the character image based on the currently assigned character.
    /// Enables or disables the image if a valid sprite exists.
    /// </summary>
    public void RefreshCharacterSprite()
    {
        if (characterImage == null) return;

        if (AssignedCharacter.HasValue && characterSpriteMap != null && characterSpriteMap.TryGetValue(AssignedCharacter.Value, out Sprite sprite) && sprite != null)
        {
            characterImage.sprite = sprite;
            characterImage.enabled = true;
        }
        else
        {
            characterImage.enabled = false;
        }
    }

    /// <summary>
    /// Updates the player's displayed name.
    /// </summary>
    /// <param name="newName">The new name of the player. If empty or null, displays "Unknown Player".</param>
    public void UpdateName(string newName)
    {
        if (playerNameText != null)
            playerNameText.text = string.IsNullOrEmpty(newName) ? "Unknown Player" : newName;
    }

    /// <summary>
    /// Updates the player's assigned character and refreshes the sprite.
    /// </summary>
    /// <param name="newCharacter">The new character assigned to this player.</param>
    public void UpdateCharacter(CharacterType? newCharacter)
    {
        AssignedCharacter = newCharacter;
        RefreshCharacterSprite();
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Ensures that the character sprite map is initialized.
    /// Maps each CharacterType to its corresponding sprite.
    /// </summary>
    private void EnsureCharacterSpriteMap()
    {
        if (characterSpriteMap != null) return;

        characterSpriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, florSprite },
            { CharacterType.Girasol, girasolSprite },
            { CharacterType.Jalapeno, jalapenoSprite },
            { CharacterType.Manzana, manzanaSprite }
        };
    }

    #endregion
}
