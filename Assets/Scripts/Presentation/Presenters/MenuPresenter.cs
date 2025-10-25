using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPresenter : MonoBehaviour
{
    void StartGame()
    {
        SceneManager.LoadScene("GameScene");
    }
}
