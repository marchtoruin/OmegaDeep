using UnityEngine;
using System.Collections;

public class AirBubbleSpawner : MonoBehaviour
{
    [Header("Air Bubble Prefabs")]
    [Tooltip("List of AirBubble prefabs to choose from (randomly)")]
    public GameObject[] airBubblePrefabs;

    [Header("Spawn Settings")]
    [Tooltip("How many air bubbles to spawn when ready")]
    public int spawnCount = 1;
    [Tooltip("Radius around this spawner to randomly place bubbles")]
    public float spawnRadius = 0f;

    [Header("Spawner Behavior")]
    [Tooltip("Destroy this spawner after spawning bubbles?")]
    public bool destroyAfterSpawn = true;

    [Header("Player Proximity Spawn")]
    [Tooltip("Distance from player at which to spawn bubbles (in world units)")]
    public float spawnDistance = 20f;
    [Tooltip("Cooldown before respawning a bubble after the player leaves and returns (seconds)")]
    public float respawnCooldown = 3f;

    private bool hasSpawned = false;
    private Transform playerTransform;
    private GameObject currentBubble = null;
    private float respawnTimer = 0f;

    private void Start()
    {
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("AirBubbleSpawner: No GameObject with tag 'Player' found in scene!", this);
        }
    }

    private void Update()
    {
        if (playerTransform == null) return;
        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool playerInRange = dist <= spawnDistance;

        // Handle respawn cooldown
        if (respawnTimer > 0f)
        {
            respawnTimer -= Time.deltaTime;
        }

        if (playerInRange)
        {
            if (currentBubble == null && respawnTimer <= 0f)
            {
                SpawnBubbles();
            }
        }
        else
        {
            if (currentBubble != null)
            {
                Destroy(currentBubble);
                currentBubble = null;
                respawnTimer = respawnCooldown;
            }
        }
    }

    private void SpawnBubbles()
    {
        if (airBubblePrefabs == null || airBubblePrefabs.Length == 0)
        {
            Debug.LogError("AirBubbleSpawner: No AirBubble prefabs assigned!", this);
            return;
        }
        // Only spawn one bubble for this logic
        GameObject prefab = airBubblePrefabs[Random.Range(0, airBubblePrefabs.Length)];
        Vector2 offset = spawnRadius > 0f ? (Vector2)Random.insideUnitCircle * spawnRadius : Vector2.zero;
        Vector3 spawnPos = transform.position + (Vector3)offset;
        currentBubble = Instantiate(prefab, spawnPos, Quaternion.identity);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, spawnDistance);
    }
} 