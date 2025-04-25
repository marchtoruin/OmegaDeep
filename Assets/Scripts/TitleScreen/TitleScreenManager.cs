using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreenManager : MonoBehaviour
{
    public string gameSceneName = "Logs"; // Change this to your actual gameplay scene name

    public void StartGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Game is exiting... (won’t show in editor)");
    }
}
