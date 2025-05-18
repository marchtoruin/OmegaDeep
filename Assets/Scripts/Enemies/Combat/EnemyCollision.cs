using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private float repulsionCheckDistance = 1.5f; // Distance to check for overlapping player
    [SerializeField] private float repulsionForce = 5f; // Force to apply to push player away when overlapped
    [SerializeField] private LayerMask playerLayerMask; // Layer mask for the player
    
    // Cached player references - found on first contact
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private DiverMovement playerMovement;
    private PlayerHealth playerHealthComponent; // Cache health component too
    
    private bool isColliding = false;
    private BadFishAI aiComponent;
    private bool checkingRepulsion = false;
    
    private void Awake()
    {
        // Get the AI component if available
        aiComponent = GetComponent<BadFishAI>();
        
        // Ensure this object has the right tag
        if (gameObject.tag != "BadFish")
        {
            gameObject.tag = "BadFish";
            if (showDebugInfo) Debug.Log($"{gameObject.name}: Tagged as BadFish for collision detection", this);
        }
    }
    
    private void OnEnable()
    {
        // --- REMOVED PLAYER FINDING LOGIC --- 
        // Player might not exist yet. We will find it on first collision.
        isColliding = false; // Reset collision state when enabled
        checkingRepulsion = false; // Reset repulsion state
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision.gameObject);
    }
    
    private void OnTriggerEnter2D(Collider2D collider)
    {
        HandlePlayerCollision(collider.gameObject);
    }
    
    private void Update()
    {
        // Check for player stuck inside fish and push them out
        // Only run if we have successfully found the player components
        if (isColliding && !checkingRepulsion && playerTransform != null && playerRb != null)
        {
            StartCoroutine(CheckForPlayerOverlap());
        }
    }
    
    private void HandlePlayerCollision(GameObject other)
    {
        // --- FIND PLAYER COMPONENTS ON FIRST CONTACT ---
        if (playerHealthComponent == null)
        {
            playerHealthComponent = other.GetComponent<PlayerHealth>();
            // If it doesn't have PlayerHealth, it's not the player we care about
            if (playerHealthComponent == null) return; 

            // If it *is* the player, cache other components
            playerTransform = other.transform;
            playerRb = other.GetComponent<Rigidbody2D>();
            playerMovement = other.GetComponent<DiverMovement>();
            
            // Log warnings if components are missing (only log once)
            if (playerRb == null)
            {
                Debug.LogError("Player found but has no Rigidbody2D! Repulsion won't work.", other);
            }
            else if (playerRb.isKinematic)
            {
                Debug.LogWarning("Player Rigidbody2D is set to Kinematic! Forces won't affect it.", other);
            }
            if (playerMovement == null)
            {
                Debug.LogWarning("Player doesn't have DiverMovement script. This might affect repulsion coordination.", other);
            }
        }
        // If the object colliding isn't the cached player, ignore it
        else if (other != playerTransform.gameObject) 
        {
             return; 
        }
        // --- END FIND PLAYER COMPONENTS ---
        
        if (showDebugInfo) Debug.Log($"COLLISION DETECTED with {other.name}", this);
        
        // Notify AI component about player collision
        if (aiComponent != null)
        {
            aiComponent.OnAttacked();
        }
        
        // Check if we're already in a collision (to prevent multiple damages)
        if (isColliding)
        {
            if (showDebugInfo) Debug.Log("Already in collision - ignoring", this);
            return;
        }
        
        // Set collision flag to prevent multiple hits
        isColliding = true;
        
        // Calculate direction from fish to player (for PlayerHealth to use)
        Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
        
        // Make the Y component stronger to ensure upward movement
        // Removed this - let PlayerHealth handle knockback direction entirely
        // knockbackDirection.y = Mathf.Abs(knockbackDirection.y) + 0.8f;
        // knockbackDirection = knockbackDirection.normalized;
        
        // Print debug info about rigidbody if available
        if (showDebugInfo && playerRb != null)
        {
            Debug.Log($"Player RB properties - Mass: {playerRb.mass}, Drag: {playerRb.drag}, " +
                    $"Constraints: {playerRb.constraints}, Gravity: {playerRb.gravityScale}, " +
                    $"Is Kinematic: {playerRb.isKinematic}, Interpolation: {playerRb.interpolation}", this);
        }
        
        // Start checking for overlap immediately if components found
        if (!checkingRepulsion && playerTransform != null && playerRb != null)
        {
            StartCoroutine(CheckForPlayerOverlap());
        }
        
        // Only damage if not invulnerable
        if (!playerHealthComponent.IsInvulnerable())
        {
            // Get damage from our GetDamageAmount method that accounts for boss status
            int damage = GetDamageAmount();
            
            if (showDebugInfo && aiComponent != null && aiComponent.isBoss)
            {
                Debug.Log($"Boss fish collision! Damage multiplied to {damage}", this);
            }
            
            // Damage the player through their health component
            // PlayerHealth will handle knockback internally
            playerHealthComponent.TakeDamage(damage);
        }
        else
        {
            if (showDebugInfo) Debug.Log("Player is invulnerable - ignoring damage", this);
        }
        
        // Reset collision flag after delay
        Invoke("ResetCollision", 1.0f); // Consider making delay configurable
    }
    
    private void ResetCollision()
    {
        isColliding = false;
        
        // Start a new repulsion check if the player is still close and components are valid
        if (playerTransform != null && playerRb != null && Vector2.Distance(transform.position, playerTransform.position) < repulsionCheckDistance)
        {
            if (!checkingRepulsion) StartCoroutine(CheckForPlayerOverlap());
        }
    }
    
    // Check if player is overlapping with the fish and push them away
    private IEnumerator CheckForPlayerOverlap()
    {
        // Ensure we have valid references before starting
        if (playerTransform == null || playerRb == null) 
        { 
            checkingRepulsion = false; 
            yield break; 
        }

        checkingRepulsion = true;
        
        // Check for a short duration
        float checkDuration = 0.5f; // Consider making configurable
        float elapsedTime = 0f;
        
        // Check frequently with short intervals
        float checkInterval = 0.05f; // Check every 0.05 seconds
        
        while (elapsedTime < checkDuration)
        {
            // Added null check just in case player gets destroyed during check
            if (playerTransform == null || playerRb == null) 
            {
                 checkingRepulsion = false; 
                 yield break;
            }

            float distance = Vector2.Distance(transform.position, playerTransform.position);
            
            // If player is too close (might be stuck)
            if (distance < repulsionCheckDistance)
            {
                Vector2 pushDirection = ((Vector2)playerTransform.position - (Vector2)transform.position).normalized;
                
                // Make sure the Y component is positive to push player upward
                if (pushDirection.y < 0.2f)
                {
                    pushDirection.y = 0.2f;
                    pushDirection = pushDirection.normalized;
                }
                
                // Apply an additional push force - apply more force when very close
                float forceFactor = 1.0f + (repulsionCheckDistance - distance) / repulsionCheckDistance;
                playerRb.AddForce(pushDirection * repulsionForce * forceFactor, ForceMode2D.Impulse);
                
                if (showDebugInfo)
                {
                    Debug.Log($"Applied repulsion force: {pushDirection * repulsionForce * forceFactor} to prevent sticking", this);
                }
                
                // Inform player movement of this brief repulsion
                if (playerMovement != null)
                {
                    playerMovement.SetKnockbackState(true, 0.1f); // Consider matching duration to checkInterval?
                }
            }
            
            elapsedTime += checkInterval;
            yield return new WaitForSeconds(checkInterval);
        }
        
        // Allow a new repulsion check
        checkingRepulsion = false;
    }
    
    // For debugging in editor
    private void OnDrawGizmos()
    {
        // Check playerTransform instead of playerRb as RB might be missing
        if (playerTransform != null && showDebugInfo)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }
    
    /// <summary>
    /// Returns the damage amount this enemy deals
    /// </summary>
    public int GetDamageAmount()
    {
        // Check if this is a boss fish and modify damage accordingly
        int actualDamage = damageAmount;
        // Use cached AI component
        // badFishHealth fishHealth = GetComponent<badFishHealth>(); 
        BadFishAI localAI = aiComponent ?? GetComponent<BadFishAI>(); // Get if null
        
        // Assuming badFishHealth has IsBoss() and GetBossDamageMultiplier()
        // We need to check if aiComponent is valid and if it's a boss
        if (localAI != null && localAI.isBoss) 
        { 
            // How boss damage is handled depends on BadFishAI/badFishHealth
            // Let's assume BadFishAI has a property or method for this
            // If using charge multiplier:
            if (localAI.IsCurrentlyCharging)
            {
                 actualDamage = Mathf.RoundToInt(actualDamage * localAI.ChargeDamageMultiplier);
            }
            // Add other boss damage multipliers if needed
        }
        
        return actualDamage;
    }
} 