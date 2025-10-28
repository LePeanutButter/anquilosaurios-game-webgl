using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerCardUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text playerNameText;
    public Image characterImage;

    [Header("Character Sprites")]
    public Sprite florSprite;
    public Sprite girasolSprite;
    public Sprite jalapenoSprite;

    private Dictionary<CharacterType, Sprite> characterSpriteMap;

    [HideInInspector]
    public CharacterType? AssignedCharacter { get; private set; }

    private void Awake()
    {
        EnsureCharacterSpriteMap();
        if (characterImage != null) characterImage.enabled = false;
    }

    private void EnsureCharacterSpriteMap()
    {
        if (characterSpriteMap != null) return;

        characterSpriteMap = new Dictionary<CharacterType, Sprite>
        {
            { CharacterType.Flor, florSprite },
            { CharacterType.Girasol, girasolSprite },
            { CharacterType.Jalapeno, jalapenoSprite }
        };
    }

    public void Setup(string playerName, CharacterType? assignedCharacter = null)
    {
        if (playerNameText != null)
            playerNameText.text = string.IsNullOrEmpty(playerName) ? "Jugador desconocido" : playerName;

        AssignedCharacter = assignedCharacter;

        RefreshCharacterSprite();
    }

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

    public void UpdateName(string newName)
    {
        if (playerNameText != null)
            playerNameText.text = string.IsNullOrEmpty(newName) ? "Jugador desconocido" : newName;
    }

    public void UpdateCharacter(CharacterType? newCharacter)
    {
        AssignedCharacter = newCharacter;
        RefreshCharacterSprite();
    }
}
