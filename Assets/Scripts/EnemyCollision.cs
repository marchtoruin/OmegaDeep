using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyCollision : MonoBehaviour
{
    [SerializeField] private int damageAmount = 10;
    [SerializeField] private float knockbackForce = 3f; // Adjusted from 8000f to 3f
    [SerializeField] private bool showDebugInfo = true;
    [SerializeField] private float knockbackDuration = 0.5f; // Longer duration for knockback effect
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
                Debug.LogError("Player found but has no Rigidbody2D! Knockback won't work.", this);
            }
            else if (playerRb.isKinematic)
            {
                Debug.LogWarning("Player Rigidbody2D is set to Kinematic! Forces won't affect it.", this);
            }
            
            if (playerMovement == null)
            {
                Debug.LogWarning("Player doesn't have DiverMovement script. This might affect knockback coordination.", this);
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
                Debug.LogError("Player has no Rigidbody2D component! Cannot apply knockback.", this);
                return;
            }
        }
        
        // Check if we're already in a collision (to prevent multiple knockbacks)
        if (isColliding)
        {
            Debug.Log("Already in collision - ignoring", this);
            return;
        }
        
        // Set collision flag to prevent multiple hits
        isColliding = true;
        
        // Calculate direction from fish to player
        Vector2 knockbackDirection = (other.transform.position - transform.position).normalized;
        
        // Make the Y component stronger to ensure upward movement
        knockbackDirection.y = Mathf.Abs(knockbackDirection.y) + 0.8f;
        knockbackDirection = knockbackDirection.normalized;
        
        // Print very detailed debug info about rigidbody
        Debug.Log($"Player RB properties - Mass: {playerRb.mass}, Drag: {playerRb.drag}, " +
                  $"Constraints: {playerRb.constraints}, Gravity: {playerRb.gravityScale}, " +
                  $"Is Kinematic: {playerRb.isKinematic}, Interpolation: {playerRb.interpolation}", this);
        
        // Apply knockback regardless of invulnerability state
        StartCoroutine(ApplyKnockbackOverTime(playerRb, knockbackDirection));
        
        // Start checking for overlap immediately
        if (!checkingRepulsion)
        {
            StartCoroutine(CheckForPlayerOverlap());
        }
        
        // Only damage if not invulnerable
        if (!playerHealth.IsInvulnerable())
        {
            // Check if this is a boss fish and modify damage accordingly
            int actualDamage = damageAmount;
            badFishHealth fishHealth = GetComponent<badFishHealth>();
            
            if (fishHealth != null && fishHealth.IsBoss())
            {
                // Apply boss damage multiplier
                actualDamage *= fishHealth.GetBossDamageMultiplier();
                Debug.Log($"Boss fish collision! Damage multiplied to {actualDamage}", this);
            }
            
            // Damage the player through their health component
            playerHealth.TakeDamage(actualDamage);
        }
        else
        {
            Debug.Log("Player is invulnerable - applying knockback but no damage", this);
        }
        
        // Reset collision flag after delay
        Invoke("ResetCollision", knockbackDuration + 1.0f);
    }
    
    private IEnumerator ApplyKnockbackOverTime(Rigidbody2D rb, Vector2 direction)
    {
        Vector3 initialPosition = rb.transform.position;
        Debug.Log($"Start knockback from position {initialPosition}", this);
        
        // Save original gravity scale to restore later
        float originalGravityScale = rb.gravityScale;
        
        // Temporarily reduce gravity during knockback to get more distance
        rb.gravityScale = 0.05f;
        
        // Disable player movement by setting knockback state
        if (playerMovement != null)
        {
            playerMovement.SetKnockbackState(true, knockbackDuration + 0.2f);
            Debug.Log("Disabled player movement control during knockback", this);
        }
        else
        {
            Debug.LogWarning("Could not find DiverMovement component to disable movement during knockback", this);
        }
        
        // Apply a stronger initial impulse to get the player moving
        rb.velocity = Vector2.zero; // Reset velocity completely
        rb.AddForce(direction * knockbackForce * 1.5f, ForceMode2D.Impulse);
        Debug.Log($"Initial impulse: {direction * knockbackForce * 1.5f}", this);
        
        yield return new WaitForFixedUpdate();
        
        // Apply force multiple times to ensure it works
        for (int i = 0; i < 5; i++) // Reduced iterations since force is now appropriate
        {
            // Apply impulse force if velocity drops too low
            if (rb.velocity.magnitude < 5f) // Reduced threshold to match new force scale
            {
                rb.AddForce(direction * knockbackForce * 0.75f, ForceMode2D.Impulse);
                
                Debug.Log($"Applied force: {direction * knockbackForce * 0.75f}, Frame: {i}, " +
                         $"Player position: {rb.transform.position}, " +
                         $"Player velocity: {rb.velocity}", this);
            }
            
            // Wait for physics update
            yield return new WaitForFixedUpdate();
        }
        
        // Wait for a brief period to let physics work
        yield return new WaitForSeconds(knockbackDuration);
        
        // Restore original gravity
        rb.gravityScale = originalGravityScale;
        
        // Log movement stats
        Debug.Log($"Knockback complete. Initial position: {initialPosition}, " +
                 $"Final position: {rb.transform.position}, " +
                 $"Total movement: {Vector3.Distance(initialPosition, rb.transform.position)}", this);
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
        
        // Reduced check duration for more frequent repulsion
        float checkDuration = knockbackDuration + 0.5f; // Reduced from 1.0f to 0.5f
        float elapsedTime = 0f;
        
        // Check more frequently with shorter intervals
        float checkInterval = 0.05f; // Check every 0.05 seconds instead of every frame
        
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
                    
                    // If the player movement component exists, make sure it knows we're pushing the player
                    if (playerMovement != null)
                    {
                        playerMovement.SetKnockbackState(true, 0.1f); // Reduced from 0.2f to 0.1f
                    }
                }
            }
            
            elapsedTime += checkInterval;
            yield return new WaitForSeconds(checkInterval); // Use fixed interval instead of physics update
        }
        
        // Allow a new repulsion check sooner
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
} 