using UnityEngine;
using System.Collections;
using FMODUnity;
using FMOD.Studio;

public class AirBubble : MonoBehaviour
{
    [Tooltip("How much oxygen to restore (absolute value, e.g. 20 = 20 units)")]
    public float oxygenRestoreAmount = 20f;

    [Header("Respawn Settings")]
    [Tooltip("How long to wait before respawning the bubble (seconds)")]
    public float respawnTime = 5f;
    [Tooltip("Layer to check for map collision before respawning")] 
    public LayerMask collisionLayer;

    [Tooltip("Radius for collision check when respawning (use small value)")]
    public float overlapRadius = 0.05f;

    [Header("FMOD Settings")]
    [Tooltip("FMOD event to play when bubble is collected")] 
    [SerializeField] private EventReference collectFmodEvent;

    private Collider2D bubbleCollider;
    private SpriteRenderer bubbleRenderer;
    private bool hasBeenCollected = false;
    private Vector3 initialPosition; // Store the starting position

    private void Awake()
    {
        bubbleCollider = GetComponent<Collider2D>();
        bubbleRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        initialPosition = transform.position; // Store initial position
        // At scene start, assume collision is present
        // if (bubbleCollider != null) bubbleCollider.enabled = false;
        // if (bubbleRenderer != null) bubbleRenderer.enabled = false;
        // StartCoroutine(WaitForCollisionAndEnable());
        if (bubbleCollider != null) bubbleCollider.enabled = true; // Enable directly
        if (bubbleRenderer != null) bubbleRenderer.enabled = true; // Enable directly
    }

    /* // Coroutine no longer needed if map is always present
    private IEnumerator WaitForCollisionAndEnable()
    {
        bool foundCollision = false;
        while (!foundCollision)
        {
            // Check collision at the initial position using OverlapCircle
            Collider2D hit = Physics2D.OverlapCircle(initialPosition, overlapRadius, collisionLayer);
            if (hit != null)
            {
                foundCollision = true;
            }
            else
            {
                yield return new WaitForSeconds(0.2f);
            }
        }
        if (bubbleCollider != null) bubbleCollider.enabled = true;
        if (bubbleRenderer != null) bubbleRenderer.enabled = true;
    }
    */

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[AirBubble] {gameObject.name} OnTriggerEnter2D with {other.name} (tag: {other.tag}), hasBeenCollected: {hasBeenCollected}, frame: {Time.frameCount}", this);
        if (!other.CompareTag("Player")) return;
        if (hasBeenCollected) return;
        if (other.CompareTag("Player"))
        {
            hasBeenCollected = true;
            // Play FMOD event if assigned
            if (collectFmodEvent.IsNull == false)
            {
                Debug.Log($"[AirBubble] Playing FMOD event for {gameObject.name} collected by {other.name} at frame {Time.frameCount}", this);
                RuntimeManager.PlayOneShot(collectFmodEvent, transform.position);
            }
            PlayerOxygen playerOxygen = other.GetComponent<PlayerOxygen>();
            if (playerOxygen != null)
            {
                playerOxygen.RefillOxygen(oxygenRestoreAmount);
            }
            StartCoroutine(RespawnRoutine());
        }
    }

    private IEnumerator RespawnRoutine()
    {
        // Hide bubble
        if (bubbleCollider != null) bubbleCollider.enabled = false;
        if (bubbleRenderer != null) bubbleRenderer.enabled = false;
        // hasBeenCollected = false; // Keep true until fully respawned
        Debug.Log($"[{gameObject.name}] RespawnRoutine: Hiding bubble. Waiting {respawnTime}s.", this);

        // Wait for respawn time
        yield return new WaitForSeconds(respawnTime);

        Debug.Log($"[{gameObject.name}] RespawnRoutine: Finished waiting. Skipping collision check.", this);
        /* // Collision check loop removed
        bool foundCollision = false;
        int checkCount = 0; // Limit check count for debugging
        while (!foundCollision && checkCount < 100) // Added checkCount limit
        {
            checkCount++;
            // Check for any collider on the collision layer at the INITIAL position using OverlapCircle
            Collider2D hit = Physics2D.OverlapCircle(initialPosition, overlapRadius, collisionLayer);
            if (hit != null)
            {
                Debug.Log($"[{gameObject.name}] RespawnRoutine: Found collision layer ({hit.gameObject.layer}) at initial position {initialPosition}. Respawning.", this);
                foundCollision = true;
            }
            else
            {
                Debug.Log($"[{gameObject.name}] RespawnRoutine: No collision layer found at initial position {initialPosition}. Waiting 0.2s (Check #{checkCount}).", this);
                yield return new WaitForSeconds(0.2f); // Check again in 0.2s
            }
        }

        if (!foundCollision)
        {
            Debug.LogWarning($"[{gameObject.name}] RespawnRoutine: Failed to find collision layer after {checkCount} checks. Aborting respawn.", this);
            yield break; // Exit if collision wasn't found after limit
        }
        */

        // Reset position before reactivating
        transform.position = initialPosition;
        Debug.Log($"[{gameObject.name}] RespawnRoutine: Reset position to {initialPosition}. Reactivating bubble.", this);
        // Reactivate bubble
        if (bubbleCollider != null) bubbleCollider.enabled = true;
        if (bubbleRenderer != null) bubbleRenderer.enabled = true;
        hasBeenCollected = false; // Now safe to allow collection again
    }
} 