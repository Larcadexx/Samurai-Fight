using UnityEngine;

public class WinLoseMenu : MonoBehaviour
{
    public void PlayAgain()
    {
        Time.timeScale = 1f;

        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.LoadSceneTransisi("Gameplay");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Gameplay");
        }
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;

        if (TransisiScene.Instance != null)
        {
            TransisiScene.Instance.LoadSceneTransisi("MainMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }
}