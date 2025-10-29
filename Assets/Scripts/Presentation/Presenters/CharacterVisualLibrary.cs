using UnityEngine;

[CreateAssetMenu(menuName = "Game/Character Visual Library")]
public class CharacterVisualLibrary : ScriptableObject
{
    [System.Serializable]
    public class CharacterVisualConfig
    {
        public CharacterType characterType;
        public Sprite defaultSprite;
        public RuntimeAnimatorController animatorController;
    }

    public CharacterVisualConfig[] characters;

    public CharacterVisualConfig GetVisualConfig(CharacterType type)
    {
        foreach (var cfg in characters)
            if (cfg.characterType == type)
                return cfg;
        return null;
    }
}
