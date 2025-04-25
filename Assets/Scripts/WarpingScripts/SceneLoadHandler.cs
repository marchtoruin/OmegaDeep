// Assets/Scripts/WarpingScripts/SceneLoadHandler.cs
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoadHandler : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(DoDelayedSpawn());
    }

    private IEnumerator DoDelayedSpawn()
    {
        // wait one frame so any default Start() logic finishes
        yield return null;

        // Only check if override is set, but do not move player or clear it here
        if (string.IsNullOrEmpty(SceneTransitionData.nextSpawnPointName))
            yield break;
        // (No further action; DiverMovement will handle spawn and clearing)
    }
}
