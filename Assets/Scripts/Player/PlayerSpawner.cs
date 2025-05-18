using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefab & Spawn Point")]
    [Tooltip("Assign the Player prefab from your Project Assets")]
    [SerializeField] private GameObject playerPrefab; 
    [Tooltip("The exact name of the empty GameObject marking the spawn location")]
    [SerializeField] private string spawnPointName = "PlayerSpawn"; 

    void Start()
    {
        // Ensure prefab is assigned in the Inspector
        if (playerPrefab == null)
        {
            Debug.LogError("PlayerSpawner: Player Prefab is not assigned in the Inspector!", this);
            return;
        }

        // Find the spawn point GameObject
        GameObject spawnPoint = GameObject.Find(spawnPointName);

        if (spawnPoint != null)
        {
            // Instantiate the player at the spawn point's position and rotation
            // Use Instantiate<GameObject> for potential type safety if needed, but GameObject works fine.
            GameObject spawnedPlayer = Instantiate(playerPrefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            Debug.Log($"Player '{spawnedPlayer.name}' spawned at '{spawnPointName}' ({spawnPoint.transform.position})");
            
            // Optional: Rename the spawned player instance for clarity in hierarchy
            spawnedPlayer.name = playerPrefab.name; 

            // --- Assign spawned player to CameraFollow target ---
            AssignCameraTarget(spawnedPlayer.transform);
            // --------------------------------------------------
        }
        else
        {
            // Log an error if the spawn point wasn't found
            Debug.LogError($"PlayerSpawner: Spawn point GameObject named '{spawnPointName}' not found in the scene!", this);

            // Fallback: Spawn at origin (0,0,0) as a last resort
            GameObject spawnedPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
            Debug.LogWarning($"PlayerSpawner: Spawning player '{spawnedPlayer.name}' at origin (0,0,0) as fallback.", this);
            
            // Optional: Rename the spawned player instance
            spawnedPlayer.name = playerPrefab.name;

            // --- Assign spawned player to CameraFollow target (Fallback) ---
            AssignCameraTarget(spawnedPlayer.transform);
            // ---------------------------------------------------------
        }

        // Optional: Destroy this spawner object after it has done its job
        // Uncomment the line below if you want the spawner to remove itself
        // Destroy(gameObject); 
    }

    // --- New method to find camera and assign target ---
    private void AssignCameraTarget(Transform playerTransform)
    {
        Camera mainCamera = Camera.main; // Find the main camera
        if (mainCamera != null)
        {
            CameraFollow cameraFollow = mainCamera.GetComponent<CameraFollow>();
            if (cameraFollow != null)
            {
                // Before assigning, ensure the target field is currently null 
                // to prevent accidentally overwriting if it was set elsewhere.
                if (cameraFollow.target == null)
                {
                    cameraFollow.target = playerTransform; // Assign the spawned player's transform
                    Debug.Log($"Assigned '{playerTransform.name}' as the target for CameraFollow.", this);
                }
                else
                {
                    Debug.LogWarning($"PlayerSpawner: CameraFollow target was already assigned to '{cameraFollow.target.name}'. Not overwriting.", this);
                }
            }
            else
            {
                Debug.LogWarning("PlayerSpawner: Could not find CameraFollow script on the main camera.", this);
            }
        }
        else
        {
            Debug.LogWarning("PlayerSpawner: Could not find the main camera in the scene to assign target.", this);
        }
    }
    // -------------------------------------------------
} 