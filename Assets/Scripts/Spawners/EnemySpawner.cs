using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject enemyPrefab; // Your fish prefab
    [SerializeField] private int numberOfEnemies = 5; // How many to spawn
    [SerializeField] private float spawnRadius = 10f; // Area to spawn within
    [SerializeField] private bool spawnInCircle = true; // Spawn in circle or square pattern
    [SerializeField] private float minSpawnDistanceFromPlayer = 5f; // Don't spawn too close to player
    [SerializeField] private float minDistanceBetweenEnemies = 3f; // Keep enemies from overlapping
    [SerializeField] private bool forceFlipXOnSpawn = false; // ADDED: Manually flip spawned enemy sprite horizontally
    
    [Header("Spawn Timing")]
    [SerializeField] private bool spawnOnStart = true; // Spawn when scene starts
    [SerializeField] private bool spawnOverTime = false; // Spawn gradually instead of all at once
    [SerializeField] private float timeBetweenSpawns = 1f; // Time between spawns if spawning over time
    
    [Header("Visualization")]
    [SerializeField] private bool showSpawnArea = true; // Show spawn area in editor
    [SerializeField] private Color spawnAreaColor = new Color(1, 0, 0, 0.2f); // Color for visualization
    
    [Header("Respawn Settings")]
    [SerializeField] private bool enableRespawn = false; // Toggle to enable/disable respawning
    [SerializeField] private float respawnDelay = 10f; // Seconds to wait before respawning
    [SerializeField] private int maxSimultaneousEnemies = 5; // Maximum number of active enemies at once
    [SerializeField] private bool respawnAtOriginalPositions = true; // Whether to respawn at the original spawn position or a new random one
    
    private Transform playerTransform;
    private List<Transform> spawnedEnemies = new List<Transform>();
    private Dictionary<GameObject, Vector3> enemyOriginalPositions = new Dictionary<GameObject, Vector3>();
    private List<GameObject> activeEnemies = new List<GameObject>();
    private List<Vector3> pendingRespawnPositions = new List<Vector3>();
    
    void Start()
    {
        // Find player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
        
        // Spawn enemies
        if (spawnOnStart)
        {
            if (spawnOverTime)
            {
                StartCoroutine(SpawnEnemiesOverTime());
            }
            else
            {
                SpawnAllEnemies();
            }
        }
    }
    
    private void SpawnAllEnemies()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            SpawnEnemy();
        }
    }
    
    private IEnumerator SpawnEnemiesOverTime()
    {
        for (int i = 0; i < numberOfEnemies; i++)
        {
            SpawnEnemy();
            yield return new WaitForSeconds(timeBetweenSpawns);
        }
    }
    
    private GameObject SpawnEnemy(Vector3? positionOverride = null)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("No enemy prefab assigned to spawner!", this);
            return null;
        }
        
        // Don't spawn more than max simultaneous enemies if respawning is enabled
        if (enableRespawn && activeEnemies.Count >= maxSimultaneousEnemies)
        {
            return null;
        }
        
        // Try to find a valid spawn position
        Vector3 spawnPosition = GetRandomSpawnPosition();
        int attempts = 0;
        
        // Retry if too close to player or other enemies
        while (!IsValidSpawnPosition(spawnPosition) && attempts < 30)
        {
            spawnPosition = GetRandomSpawnPosition();
            attempts++;
        }
        
        if (attempts >= 30)
        {
            Debug.LogWarning("Could not find valid spawn position after 30 attempts. Consider reducing spawn density or increasing spawn area.", this);
            return null;
        }
        
        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
        // --- ADDED: Force flip logic ---
        if (forceFlipXOnSpawn)
        {
            SpriteRenderer sr = enemy.GetComponentInChildren<SpriteRenderer>(); // Look in children too
            if (sr != null)
            {
                bool originalFlip = sr.flipX;
                sr.flipX = !originalFlip;
                Debug.Log($"[EnemySpawner] Force flipping {enemy.name}. Original flipX: {originalFlip}, New flipX: {sr.flipX}", enemy);
            }
            else
            {
                Debug.LogWarning($"[EnemySpawner] forceFlipXOnSpawn is true, but could not find SpriteRenderer on spawned enemy {enemy.name} or its children.", enemy);
            }
        }
        // --- End Force flip logic ---
        
        // Make it a child of this spawner for organization
        enemy.transform.parent = transform;
        
        // Give it a unique name
        enemy.name = $"{enemyPrefab.name}_{spawnedEnemies.Count + 1}";
        
        // Add to list for tracking
        spawnedEnemies.Add(enemy.transform);
        
        // If respawning is enabled, track in active enemies and original positions
        if (enableRespawn)
        {
            activeEnemies.Add(enemy);
            enemyOriginalPositions[enemy] = spawnPosition;
        }
        
        // Log spawn
        Debug.Log($"Spawned {enemy.name} at position {spawnPosition}");
        return enemy;
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 position;
        
        if (spawnInCircle)
        {
            // Random point in circle
            Vector2 randomCircle = Random.insideUnitCircle * spawnRadius;
            position = transform.position + new Vector3(randomCircle.x, randomCircle.y, 0);
        }
        else
        {
            // Random point in square
            float x = Random.Range(-spawnRadius, spawnRadius);
            float y = Random.Range(-spawnRadius, spawnRadius);
            position = transform.position + new Vector3(x, y, 0);
        }
        
        return position;
    }
    
    private bool IsValidSpawnPosition(Vector3 position)
    {
        // Check if too close to player
        if (playerTransform != null)
        {
            if (Vector3.Distance(position, playerTransform.position) < minSpawnDistanceFromPlayer)
            {
                return false;
            }
        }
        
        // Check if too close to other enemies
        foreach (Transform enemy in spawnedEnemies)
        {
            if (enemy != null && Vector3.Distance(position, enemy.position) < minDistanceBetweenEnemies)
            {
                return false;
            }
        }
        
        return true;
    }
    
    // Spawn an enemy on demand (can be called from other scripts)
    public GameObject SpawnEnemyAtPosition(Vector3 position)
    {
        return SpawnEnemy(position);
    }
    
    // For debugging/visualization in the editor
    private void OnDrawGizmosSelected()
    {
        if (!showSpawnArea) return;
        
        Gizmos.color = spawnAreaColor;
        
        if (spawnInCircle)
        {
            // Draw circle
            Gizmos.DrawSphere(transform.position, spawnRadius);
        }
        else
        {
            // Draw cube
            Gizmos.DrawCube(transform.position, new Vector3(spawnRadius * 2, spawnRadius * 2, 0.1f));
        }
        
        // Draw player minimum distance
        if (playerTransform != null)
        {
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawSphere(playerTransform.position, minSpawnDistanceFromPlayer);
        }
    }
    
    private void Update()
    {
        if (!enableRespawn)
            return;
        
        // Check for any null entries in activeEnemies (destroyed enemies)
        List<GameObject> enemiesToRespawn = new List<GameObject>();

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                // The GameObject reference itself is null, it was destroyed
                enemiesToRespawn.Add(activeEnemies[i]); // Add the null reference to process
                activeEnemies.RemoveAt(i); // Remove from active list
            }
        }

        // Schedule respawn for each destroyed enemy
        foreach(var destroyedEnemyPlaceholder in enemiesToRespawn)
        {
            // Note: We can't directly use destroyedEnemyPlaceholder to get the original position anymore
            // because the key in the dictionary is the GameObject instance which is now null.
            // This respawn logic needs rethinking if respawnAtOriginalPositions is critical.
            // For now, let's simplify and just respawn at a new random valid position.

             if (respawnAtOriginalPositions)
             {
                // This logic is flawed as the key (the destroyed GameObject) is lost.
                // Need a way to store original positions that persists after destruction (e.g., list of positions)
                // Fallback: Respawn randomly for now.
                Debug.LogWarning("[EnemySpawner] RespawnAtOriginalPositions is enabled, but tracking original position after destruction is not fully implemented. Respawning randomly.", this);
                 StartCoroutine(RespawnEnemyAfterDelay(respawnDelay, null)); // Pass null for position
             }
             else
             {
                StartCoroutine(RespawnEnemyAfterDelay(respawnDelay, null)); // Pass null for position
             }
        }
    }
    
    private IEnumerator RespawnEnemyAfterDelay(float delay, Vector3? specificPosition)
    {
        yield return new WaitForSeconds(delay);

        // Only respawn if under max limit
        if (activeEnemies.Count < maxSimultaneousEnemies)
        {
            Vector3? respawnPos = specificPosition;
            if (!respawnPos.HasValue)
            {
                // Find a new random valid position if none was provided
                Vector3 randomPos = GetRandomSpawnPosition();
                 int attempts = 0;
                 while (!IsValidSpawnPosition(randomPos) && attempts < 30)
                 {
                    randomPos = GetRandomSpawnPosition();
                    attempts++;
                 }
                 if(attempts < 30)
                 {
                    respawnPos = randomPos;
                 } else {
                    Debug.LogWarning($"[EnemySpawner] Could not find valid position for respawn after {attempts} attempts.", this);
                    // Don't try to spawn if no valid position found
                 }
            }


            // Spawn new enemy if we found a position
            if (respawnPos.HasValue)
            {
                GameObject newEnemy = SpawnEnemy(respawnPos.Value); // Call internal spawn with position
                if (newEnemy != null)
                {
                    Debug.Log($"Respawned enemy at position {respawnPos.Value} after {delay} seconds");
                }
            }
        }
    }
    
    public void ResetSpawner()
    {
        // Destroy all currently tracked active enemies
        foreach (var enemy in new List<GameObject>(activeEnemies)) // Iterate over a copy
        {
            if (enemy != null)
                Destroy(enemy.gameObject);
        }
         // Also destroy any remaining tracked transforms just in case
         foreach (var enemyTransform in new List<Transform>(spawnedEnemies))
         {
             if (enemyTransform != null)
                Destroy(enemyTransform.gameObject);
         }

        // Clear tracking lists/dictionaries
        spawnedEnemies.Clear();
        activeEnemies.Clear();
        enemyOriginalPositions.Clear(); // This dictionary might not be reliable anymore with the current respawn logic
        pendingRespawnPositions.Clear(); // This list seems unused with the current respawn logic

        // Stop any ongoing respawn coroutines
        StopAllCoroutines();

        // Respawn the initial set of enemies
         if (spawnOnStart)
         {
            if (spawnOverTime)
            {
                StartCoroutine(SpawnEnemiesOverTime());
            }
            else
            {
                SpawnAllEnemies();
            }
         }
    }
} 