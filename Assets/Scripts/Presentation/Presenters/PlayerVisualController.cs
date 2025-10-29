using UnityEngine;

public class PlayerVisualController : MonoBehaviour
{
    [SerializeField] private CharacterVisualLibrary visualLibrary;
    [SerializeField] private GameObject playerPrefab;

    private GameObject activeCharacter;

    public void SpawnCharacter(CharacterType type)
    {
        if (visualLibrary == null || playerPrefab == null)
        {
            Debug.LogWarning("[PlayerVisualController] Faltan referencias.");
            return;
        }

        if (activeCharacter != null)
            Destroy(activeCharacter);

        activeCharacter = Instantiate(playerPrefab, transform);
        var spriteRenderer = activeCharacter.GetComponentInChildren<SpriteRenderer>();
        var animator = activeCharacter.GetComponentInChildren<Animator>();

        var config = visualLibrary.GetVisualConfig(type);
        if (config == null) return;

        if (spriteRenderer != null)
            spriteRenderer.sprite = config.defaultSprite;

        if (animator != null)
            animator.runtimeAnimatorController = config.animatorController;
    }
}
