// Assets/Scripts/WarpingScripts/TeleportTrigger.cs
using UnityEngine;
using UnityEngine.SceneManagement;

public class TeleportTrigger : MonoBehaviour
{
    [Tooltip("Exact scene name as in Build Settings")]
    public string sceneToLoad;

    [Tooltip("Name of the SpawnPoint GameObject in the target scene")]
    public string spawnPointNameOnNextScene;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[TeleportTrigger] Triggered on '{gameObject.name}', saving spawn '{spawnPointNameOnNextScene}'");
            SceneTransitionData.nextSpawnPointName = spawnPointNameOnNextScene;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
