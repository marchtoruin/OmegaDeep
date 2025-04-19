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
    
    // Track the player we're colliding with
    private Transform playerTransform;
    private Rigidbody2D playerRb;
    private DiverMovement playerMovement;
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
            Debug.Log($"{gameObject.name}: Tagged as BadFish for collision detection");
        }
    }
    
    private void OnEnable()
    {
        // Try to find the player at startup
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
            playerRb = player.GetComponent<Rigidbody2D>();
            playerMovement = player.GetComponent<DiverMovement>();
            
            if (playerRb == null)
            {
                Debug.LogError("Player found but has no Rigidbody2D! Repulsion won't work.", this);
            }
            else if (playerRb.isKinematic)
            {
                Debug.LogWarning("Player Rigidbody2D is set to Kinematic! Forces won't affect it.", this);
            }
            
            if (playerMovement == null)
            {
                Debug.LogWarning("Player doesn't have DiverMovement script. This might affect repulsion coordination.", this);
            }
        }
        else
        {
            Debug.LogError("No GameObject with tag 'Player' found in scene! Make sure player has this tag.", this);
        }
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
        if (isColliding && !checkingRepulsion)
        {
            StartCoroutine(CheckForPlayerOverlap());
        }
    }
    
    private void HandlePlayerCollision(GameObject other)
    {
        // Check if this is the player by checking for PlayerHealth component
        PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
        if (playerHealth == null)
        {
            return; // Not the player
        }
        
        Debug.Log($"COLLISION DETECTED with {other.name} - has PlayerHealth component", this);
        
        // Notify AI component about player collision
        if (aiComponent != null)
        {
            aiComponent.OnPlayerCollision();
        }
        
        // Get or update the player's rigidbody if needed
        if (playerRb == null)
        {
            playerRb = other.GetComponent<Rigidbody2D>();
            playerMovement = other.GetComponent<DiverMovement>();
            
            if (playerRb == null)
            {
                Debug.LogError("Player has no Rigidbody2D component! Cannot apply repulsion.", this);
                return;
            }
        }
        
        // Check if we're already in a collision (to prevent multiple damages)
        if (isColliding)
        {
            Debug.Log("Already in collision - ignoring", this);
            return;
        }
        
        // Set collision flag to prevent multiple hits
        isColliding = true;
        
        // Calculate direction from fish to player (for PlayerHealth to use)
        Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
        
        // Make the Y component stronger to ensure upward movement
        knockbackDirection.y = Mathf.Abs(knockbackDirection.y) + 0.8f;
        knockbackDirection = knockbackDirection.normalized;
        
        // Print debug info about rigidbody
        if (showDebugInfo)
        {
            Debug.Log($"Player RB properties - Mass: {playerRb.mass}, Drag: {playerRb.drag}, " +
                    $"Constraints: {playerRb.constraints}, Gravity: {playerRb.gravityScale}, " +
                    $"Is Kinematic: {playerRb.isKinematic}, Interpolation: {playerRb.interpolation}", this);
        }
        
        // Start checking for overlap immediately
        if (!checkingRepulsion)
        {
            StartCoroutine(CheckForPlayerOverlap());
        }
        
        // Only damage if not invulnerable
        if (!playerHealth.IsInvulnerable())
        {
            // Get damage from our GetDamageAmount method that accounts for boss status
            int damage = GetDamageAmount();
            
            if (showDebugInfo && GetComponent<badFishHealth>() != null && GetComponent<badFishHealth>().IsBoss())
            {
                Debug.Log($"Boss fish collision! Damage multiplied to {damage}", this);
            }
            
            // Damage the player through their health component
            // PlayerHealth will handle knockback internally
            playerHealth.TakeDamage(damage);
        }
        else
        {
            Debug.Log("Player is invulnerable - ignoring damage", this);
        }
        
        // Reset collision flag after delay
        Invoke("ResetCollision", 1.0f);
    }
    
    private void ResetCollision()
    {
        isColliding = false;
        
        // Start a new repulsion check if the player is still close
        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < repulsionCheckDistance)
        {
            StartCoroutine(CheckForPlayerOverlap());
        }
    }
    
    // Check if player is overlapping with the fish and push them away
    private IEnumerator CheckForPlayerOverlap()
    {
        checkingRepulsion = true;
        
        // Check for a short duration
        float checkDuration = 0.5f;
        float elapsedTime = 0f;
        
        // Check frequently with short intervals
        float checkInterval = 0.05f; // Check every 0.05 seconds
        
        while (elapsedTime < checkDuration)
        {
            // Only check if we have player info
            if (playerTransform != null && playerRb != null)
            {
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
                        playerMovement.SetKnockbackState(true, 0.1f);
                    }
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
        badFishHealth fishHealth = GetComponent<badFishHealth>();
        
        if (fishHealth != null && fishHealth.IsBoss())
        {
            // Apply boss damage multiplier
            actualDamage *= fishHealth.GetBossDamageMultiplier();
        }
        
        return actualDamage;
    }
} 