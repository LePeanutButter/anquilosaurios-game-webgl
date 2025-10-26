using UnityEngine;

public class MenuGameManager : MonoBehaviour
{
    public void OnStartButtonPressed()
    {
        if (GameplayLoader.Instance == null)
        {
            Debug.LogError("MenuGameManager: GameplayLoader no existe en la escena de menu. Crea el GameObject GameplayLoader con el script GameplayLoader.");
            return;
        }

        GameplayLoader.Instance.LoadGameplay();
    }
}
