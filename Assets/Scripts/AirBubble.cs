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

    [Header("FMOD Settings")]
    [Tooltip("FMOD event to play when bubble is collected")] 
    [SerializeField] private EventReference collectFmodEvent;

    private Collider2D bubbleCollider;
    private SpriteRenderer bubbleRenderer;
    private bool hasBeenCollected = false;

    private void Awake()
    {
        bubbleCollider = GetComponent<Collider2D>();
        bubbleRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // At scene start, only enable when collision is present
        if (bubbleCollider != null) bubbleCollider.enabled = false;
        if (bubbleRenderer != null) bubbleRenderer.enabled = false;
        StartCoroutine(WaitForCollisionAndEnable());
    }

    private IEnumerator WaitForCollisionAndEnable()
    {
        bool foundCollision = false;
        while (!foundCollision)
        {
            Collider2D hit = Physics2D.OverlapPoint(transform.position, collisionLayer);
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
        hasBeenCollected = false;

        // Wait for respawn time
        yield return new WaitForSeconds(respawnTime);

        // Wait until map collision is loaded at this position
        bool foundCollision = false;
        while (!foundCollision)
        {
            // Check for any collider on the collision layer at this position
            Collider2D hit = Physics2D.OverlapPoint(transform.position, collisionLayer);
            if (hit != null)
            {
                foundCollision = true;
            }
            else
            {
                yield return new WaitForSeconds(0.2f); // Check again in 0.2s
            }
        }

        // Reactivate bubble
        if (bubbleCollider != null) bubbleCollider.enabled = true;
        if (bubbleRenderer != null) bubbleRenderer.enabled = true;
    }
} 