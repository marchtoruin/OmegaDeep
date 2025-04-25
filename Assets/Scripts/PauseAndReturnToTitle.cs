using UnityEngine;
using UnityEngine.SceneManagement;

// Attach this script to your GameManager GameObject
public class PauseAndReturnToTitle : MonoBehaviour
{
    void Update()
    {
        // Listen for Escape key
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Load the TitleScreen scene
            SceneManager.LoadScene("TitleScreen");
        }
    }
} 