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
    
    private void SpawnEnemy()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("No enemy prefab assigned to spawner!", this);
            return;
        }
        
        // Don't spawn more than max simultaneous enemies if respawning is enabled
        if (enableRespawn && activeEnemies.Count >= maxSimultaneousEnemies)
        {
            return;
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
            return;
        }
        
        // Spawn the enemy
        GameObject enemy = Instantiate(enemyPrefab, spawnPosition, Quaternion.identity);
        
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
        
        GameObject enemy = Instantiate(enemyPrefab, position, Quaternion.identity);
        enemy.transform.parent = transform;
        enemy.name = $"{enemyPrefab.name}_{spawnedEnemies.Count + 1}";
        spawnedEnemies.Add(enemy.transform);
        
        // If respawning is enabled, track in active enemies and original positions
        if (enableRespawn)
        {
            activeEnemies.Add(enemy);
            enemyOriginalPositions[enemy] = position;
        }
        
        return enemy;
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
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] == null)
            {
                // If we have original positions and want to respawn there
                if (respawnAtOriginalPositions && enemyOriginalPositions.ContainsKey(activeEnemies[i]))
                {
                    Vector3 originalPos = enemyOriginalPositions[activeEnemies[i]];
                    pendingRespawnPositions.Add(originalPos);
                    
                    // Remove from tracking dictionaries
                    enemyOriginalPositions.Remove(activeEnemies[i]);
                }
                else
                {
                    // Schedule a new random position respawn
                    pendingRespawnPositions.Add(GetRandomSpawnPosition());
                }
                
                // Remove from active enemies
                activeEnemies.RemoveAt(i);
                
                // Start respawn coroutine if not already at max enemies
                if (activeEnemies.Count < maxSimultaneousEnemies)
                {
                    StartCoroutine(RespawnEnemyAfterDelay(respawnDelay));
                }
            }
        }
    }
    
    private IEnumerator RespawnEnemyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Only respawn if we have pending positions and are under max limit
        if (pendingRespawnPositions.Count > 0 && activeEnemies.Count < maxSimultaneousEnemies)
        {
            // Get the position
            Vector3 respawnPos = pendingRespawnPositions[0];
            pendingRespawnPositions.RemoveAt(0);
            
            // Spawn new enemy
            GameObject newEnemy = SpawnEnemyAtPosition(respawnPos);
            if (newEnemy != null)
            {
                Debug.Log($"Respawned enemy at position {respawnPos} after {delay} seconds");
            }
        }
    }
} 