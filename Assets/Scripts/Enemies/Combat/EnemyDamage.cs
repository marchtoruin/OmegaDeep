using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyDamage : MonoBehaviour
{
    [Header("Damage Settings")]
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float damageInterval = 1.0f; // Time between damage applications
    [SerializeField] private bool showDebugMessages = false;
    
    // Track when damage was last applied to prevent rapid damage
    private float lastDamageTime;
    
    // Reference to player's health component
    private PlayerHealth playerHealth;
    private bool isInContactWithPlayer = false;
    
    private void Start()
    {
        lastDamageTime = -damageInterval; // Allow immediate damage on first contact
        
        // Check for collider and suggest using trigger
        Collider2D col = GetComponent<Collider2D>();
        if (col != null && !col.isTrigger)
        {
            Debug.Log("Consider setting the collider to trigger mode to prevent pushing the enemy fish", this);
        }
    }
    
    private void Update()
    {
        // --- ADDED: Defensive Checks --- 
        if (isInContactWithPlayer && (playerHealth == null || (playerHealth != null && !playerHealth.gameObject.activeInHierarchy)))
        {
            // If player reference is lost or player is inactive, force contact off.
            Debug.LogWarning($"[{gameObject.name} - Update] Player reference lost or player inactive while isInContact=true. Forcing contact off.", this);
            isInContactWithPlayer = false;
        }
        // --- END Defensive Checks ---

        // Apply damage over time while in contact
        if (isInContactWithPlayer && playerHealth != null)
        {
            // Check if enough time has passed to apply damage again
            if (Time.time >= lastDamageTime + damageInterval)
            {
                ApplyDamageToPlayer();
                lastDamageTime = Time.time;
            }
        }
    }
    
    // Note: These collision functions will only be called if using non-trigger colliders
    // Using these may result in the fish being pushed by the player
    private void OnCollisionEnter2D(Collision2D collision)
    {
        CheckForPlayer(collision.gameObject);
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Ensure player reference is maintained
        if (!isInContactWithPlayer)
        {
            CheckForPlayer(collision.gameObject);
        }
    }
    
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            // ADDED: Log Exit
            Debug.Log($"[{gameObject.name} - OnCollisionExit2D] Player collision exit detected.", this);
            isInContactWithPlayer = false;
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name} stopped contact with player", this);
            }
        }
    }
    
    // These trigger functions are recommended for enemy damage
    // since they don't involve physics pushing
    private void OnTriggerEnter2D(Collider2D collider)
    {
        CheckForPlayer(collider.gameObject);
    }
    
    private void OnTriggerStay2D(Collider2D collider)
    {
        // Ensure player reference is maintained
        if (!isInContactWithPlayer)
        {
            CheckForPlayer(collider.gameObject);
        }
    }
    
    private void OnTriggerExit2D(Collider2D collider)
    {
        if (collider.gameObject.CompareTag("Player"))
        {
            // ADDED: Log Exit
            Debug.Log($"[{gameObject.name} - OnTriggerExit2D] Player trigger exit detected.", this);
            isInContactWithPlayer = false;
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name} stopped contact with player", this);
            }
        }
    }
    
    private void CheckForPlayer(GameObject contactObject)
    {
        // Check if this is the player
        if (contactObject.CompareTag("Player"))
        {
            // Get the player health component if we don't have it already
            if (playerHealth == null)
            {
                playerHealth = contactObject.GetComponent<PlayerHealth>();
            }
            
            isInContactWithPlayer = true;
            
            // Apply damage immediately on first contact
            if (Time.time >= lastDamageTime + damageInterval)
            {
                ApplyDamageToPlayer();
                lastDamageTime = Time.time;
            }
            
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name} made contact with player", this);
            }
        }
    }
    
    private void ApplyDamageToPlayer()
    {
        if (playerHealth != null)
        {
            Debug.LogWarning($"Applying damage via EnemyDamage script on GameObject: {gameObject.name}", this);
            
            playerHealth.TakeDamage(damageAmount);
            
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name} damaged player for {damageAmount} health", this);
            }
        }
    }

    private void OnDisable()
    {
        // If this component or its GameObject is disabled, it should no longer be considered in contact.
        // This helps clean up state if the enemy is, for example, pooled and reused,
        // or destroyed/disabled while the player was in its trigger.
        if (isInContactWithPlayer)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[{gameObject.name} - OnDisable] Component disabled. Resetting isInContactWithPlayer to false.", this);
            }
            isInContactWithPlayer = false;
        }
        // Optionally, clear the playerHealth reference too, though Update() already checks for its validity.
        // playerHealth = null;
    }
} 