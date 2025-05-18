// Assets/Scripts/WarpingScripts/SceneTransition.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransition : MonoBehaviour
{
    [Tooltip("Exact scene name as in Build Settings")]
    public string sceneToLoad = "Main";

    /// <summary>
    /// Call this from your UI Button OnClick or other trigger
    /// </summary>
    public void LoadScene()
    {
        SceneManager.LoadScene(sceneToLoad);
    }
}
