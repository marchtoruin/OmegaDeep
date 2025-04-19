using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BadFishAI : MonoBehaviour
{
    [Header("Detection Settings")]
    [SerializeField] private float playerDetectionRange = 5f;
    [SerializeField] private float aggroRange = 8f; // Range at which the fish will pursue when hit
    [SerializeField] private LayerMask obstacleLayer; // Layer for obstacles the fish should avoid
    [SerializeField] private bool showDebugGizmos = true;
    
    [Header("Movement Settings")]
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float rotationSpeed = 3f;
    [SerializeField] private float minDistanceToWaypoint = 0.5f;
    [SerializeField] private float minDistanceToPlayer = 1f; // Don't get too close to player
    
    [Header("Physics Settings")]
    [SerializeField] private RigidbodyType2D bodyType = RigidbodyType2D.Dynamic;
    [SerializeField] private CollisionDetectionMode2D collisionDetectionMode = CollisionDetectionMode2D.Continuous;
    
    [Header("Patrol Settings")]
    [SerializeField] private Vector2[] patrolPoints; // Manual patrol points if needed
    [SerializeField] private float patrolRadius = 5f; // Automatic patrol radius from spawn
    [SerializeField] private float patrolPointWaitTime = 1.5f; // Time to wait at patrol points
    [SerializeField] private bool useRandomPatrolPoints = true; // Generate random patrol points
    [SerializeField] private int maxRandomPatrolPoints = 4; // Max number of random patrol points
    
    [Header("Appearance Settings")]
    [SerializeField] public bool artworkFacesRight = true; // Set based on which way your sprite artwork naturally faces
    [SerializeField] private bool debugOrientation = false; // Turn this on to debug orientation issues
    
    [Header("Boss Configuration")]
    [SerializeField] private bool isBoss = false; // Is this fish a mini-boss?
    [SerializeField] private float bossSpeedMultiplier = 1.2f; // Boss is faster
    [SerializeField] private float bossAggroRangeMultiplier = 1.5f; // Boss detects player from further away
    [SerializeField] private float bossChaseTimeMultiplier = 2f; // Boss chases for longer
    [SerializeField] private Color bossColor = new Color(1f, 0.5f, 0.5f, 1f); // Reddish tint for boss fish
    [SerializeField] private bool showBossDebugLogs = true; // Show debugging for boss fish
    
    [Header("Collision Avoidance")]
    [SerializeField] private float collisionAvoidanceRadius = 1.5f; // How far ahead to check for collisions
    [SerializeField] private float collisionAvoidanceForce = 5f; // How strongly to avoid collisions
    [SerializeField] private LayerMask collisionLayers; // Layers to avoid (should include floor and other obstacles)
    [SerializeField] private float raycastSpacing = 0.5f; // Space between raycasts for collision detection
    
    // State tracking
    private enum FishState { Patrol, Chase, Flee, Stunned }
    private FishState currentState = FishState.Patrol;
    private Vector2 startPosition;
    private Vector2 currentTarget;
    private int currentPatrolIndex = 0;
    private List<Vector2> randomPatrolPoints = new List<Vector2>();
    private float stateTimer = 0f;
    private bool isWaitingAtPatrolPoint = false;
    private bool isAggro = false;
    private float aggroTimer = 0f;
    private const float MAX_AGGRO_TIME = 8f;
    
    // Components
    private Rigidbody2D rb;
    private badFishHealth healthComponent;
    private SpriteRenderer spriteRenderer;
    private GameObject player;
    
    // Animation support
    private bool isFacingRight = true;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        
        // Log initial Rigidbody2D state
        if (rb != null)
        {
            Debug.Log($"BadFishAI Awake - INITIAL Rigidbody2D state: Type={rb.bodyType}, GameObject={gameObject.name}", this);
        }
        
        healthComponent = GetComponent<badFishHealth>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        
        // Store spawn position for patrol radius
        startPosition = transform.position;
        
        // FORCE Dynamic body type for proper collisions with static objects
        if (rb != null)
        {
            // Override inspector settings to ensure Dynamic mode
            Debug.Log($"BadFishAI Awake - Setting Rigidbody2D from {rb.bodyType} to Dynamic", this);
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = collisionDetectionMode;
            
            // Log what we did
            Debug.Log($"BadFishAI: FORCING Rigidbody2D to Dynamic mode for proper collisions with static objects", this);
            
            // Start watching for bodyType changes
            StartCoroutine(MonitorBodyTypeChanges());
        }
    }
    
    // Coroutine to watch for changes to the Rigidbody2D bodyType
    private IEnumerator MonitorBodyTypeChanges()
    {
        if (rb == null) yield break;
        
        RigidbodyType2D lastType = rb.bodyType;
        
        while (true)
        {
            // Check if bodyType changed
            if (rb.bodyType != lastType)
            {
                Debug.LogWarning($"BadFishAI: Rigidbody2D bodyType CHANGED from {lastType} to {rb.bodyType} on {gameObject.name}", this);
                
                // Capture stack trace to find what's changing it
                string stackTrace = System.Environment.StackTrace;
                Debug.LogWarning($"Stack trace at bodyType change: {stackTrace}", this);
                
                // Update last known type
                lastType = rb.bodyType;
                
                // Force back to Dynamic if needed
                if (rb.bodyType != RigidbodyType2D.Dynamic)
                {
                    Debug.LogWarning($"BadFishAI: Forcing Rigidbody2D back to Dynamic mode", this);
                    rb.bodyType = RigidbodyType2D.Dynamic;
                }
            }
            
            yield return new WaitForSeconds(0.1f); // Check every 10th of a second
        }
    }
    
    void Start()
    {
        // Get components
        // NOTE: We already get the Rigidbody2D in Awake, don't override it again
        // rb = GetComponent<Rigidbody2D>();
        
        // Check for component conflicts
        CheckForConflictingComponents();
        
        // Re-check Rigidbody settings at Start in case something changed them
        if (rb != null && rb.bodyType != RigidbodyType2D.Dynamic)
        {
            Debug.LogWarning($"BadFishAI: Rigidbody2D bodyType changed since Awake. Was: {rb.bodyType}, resetting to Dynamic", this);
            rb.bodyType = RigidbodyType2D.Dynamic;
        }
        
        healthComponent = GetComponent<badFishHealth>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Find player
        player = GameObject.FindGameObjectWithTag("Player");
        
        // Store starting position
        startPosition = transform.position;
        
        // Validate colliders for physics collisions
        ValidateColliders();
        
        // Synchronize boss status with health component
        if (healthComponent != null)
        {
            // Check if the isBoss flags are different between components
            bool healthIsBoss = healthComponent.IsBoss();
            if (healthIsBoss != isBoss)
            {
                Debug.LogWarning($"Boss status mismatch between AI ({isBoss}) and Health ({healthIsBoss}) components on {gameObject.name}. Synchronizing to AI setting.", this);
                healthComponent.SetBossStatus(isBoss);
            }
        }
        
        // Apply boss modifications if this is a boss fish
        if (isBoss)
        {
            SetupBossAttributes();
        }
        
        // Generate patrol points 
        if (useRandomPatrolPoints)
        {
            GenerateRandomPatrolPoints();
        }
        
        // Set initial state
        currentState = FishState.Patrol;
        SetNextPatrolTarget();
        
        // Run an initial orientation check
        CheckAndFixOrientation();
        
        if (debugOrientation)
        {
            Debug.Log($"Fish initialized. Artwork naturally faces: {(artworkFacesRight ? "RIGHT" : "LEFT")}", this);
        }
    }
    
    private void CheckForConflictingComponents()
    {
        // Check for EnemyMovement component which might conflict with this script
        EnemyMovement enemyMovement = GetComponent<EnemyMovement>();
        if (enemyMovement != null)
        {
            Debug.LogError($"CONFLICT DETECTED: {gameObject.name} has both BadFishAI and EnemyMovement components!", this);
            Debug.LogError("EnemyMovement sets Rigidbody2D to Kinematic, which prevents collisions with static objects.", this);
            Debug.LogError("Solution: Remove the EnemyMovement component or disable its 'modifyRigidbodyOnStart' property.", this);
        }
        
        // Check for other scripts that might modify the Rigidbody2D
        MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allComponents)
        {
            // Skip this component and known ones
            if (component == this || component is EnemyMovement) continue;
            
            string componentName = component.GetType().Name.ToLower();
            if (componentName.Contains("movement") || 
                componentName.Contains("physics") || 
                componentName.Contains("rigidbody") || 
                componentName.Contains("motor"))
            {
                Debug.LogWarning($"Potential conflict: {gameObject.name} has {component.GetType().Name} which might also modify Rigidbody2D settings.", this);
            }
        }
    }
    
    void Update()
    {
        // Check and fix orientation issues
        CheckAndFixOrientation();
        
        // Process aggro timers and player detection
        ProcessAggroState();
        
        // State machine behavior
        switch (currentState)
        {
            case FishState.Patrol:
                UpdatePatrolState();
                break;
                
            case FishState.Chase:
                UpdateChaseState();
                break;
                
            case FishState.Flee:
                UpdateFleeState();
                break;
                
            case FishState.Stunned:
                UpdateStunnedState();
                break;
        }
    }
    
    private void ProcessAggroState()
    {
        // If the player exists and we're not already in chase mode or stunned
        if (player != null && currentState != FishState.Chase && currentState != FishState.Stunned)
        {
            // Check distance to player
            float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
            
            // If player is within detection range, become aggro
            if (distanceToPlayer <= playerDetectionRange)
            {
                BeginChasing();
            }
        }
        
        // Handle aggro timer
        if (isAggro)
        {
            aggroTimer -= Time.deltaTime;
            if (aggroTimer <= 0)
            {
                isAggro = false;
                // Only stop chasing if we're not still close to the player
                if (currentState == FishState.Chase && player != null)
                {
                    float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
                    if (distanceToPlayer > playerDetectionRange)
                    {
                        ReturnToPatrol();
                    }
                }
            }
        }
    }
    
    private void UpdatePatrolState()
    {
        if (isWaitingAtPatrolPoint)
        {
            stateTimer -= Time.deltaTime;
            if (stateTimer <= 0)
            {
                isWaitingAtPatrolPoint = false;
                SetNextPatrolTarget();
            }
            return;
        }
        
        // Move towards current patrol point
        MoveTowards(currentTarget, patrolSpeed);
        
        // Check if we reached the patrol point
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget);
        if (distanceToTarget <= minDistanceToWaypoint)
        {
            // Wait at patrol point
            stateTimer = patrolPointWaitTime;
            isWaitingAtPatrolPoint = true;
        }
    }
    
    private void UpdateChaseState()
    {
        if (player == null) 
        {
            ReturnToPatrol();
            return;
        }
        
        // Calculate distance to player
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);
        
        // If we lost the player (too far away), return to patrol
        if (distanceToPlayer > aggroRange && !isAggro)
        {
            ReturnToPatrol();
            return;
        }
        
        // Calculate direction to player
        Vector2 direction = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        
        // If we're too close to the player, don't get closer but still face them
        if (distanceToPlayer <= minDistanceToPlayer)
        {
            // Just face the player but don't move closer
            UpdateFacingDirection(direction.x);
            rb.velocity = Vector2.zero;
        }
        else
        {
            // Make sure we're facing the right way before moving
            bool shouldFaceRight = direction.x > 0;
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
            
            // Move towards player using our enhanced movement method
            MoveTowards(player.transform.position, chaseSpeed);
        }
    }
    
    private void UpdateFleeState()
    {
        // Not implemented yet - could be used if the fish has low health
        ReturnToPatrol();
    }
    
    private void UpdateStunnedState()
    {
        // Stay stunned for the timer duration
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
            // Return to previous state or patrol
            currentState = isAggro ? FishState.Chase : FishState.Patrol;
        }
    }
    
    private void MoveTowards(Vector2 target, float speed)
    {
        // Calculate direction to target
        Vector2 direction = (target - (Vector2)transform.position).normalized;

        // Always face the direction we're moving
        bool shouldFaceRight = direction.x > 0;
        
        // Update orientation before moving
        if (shouldFaceRight != isFacingRight)
        {
            FlipSprite(shouldFaceRight);
        }
        
        // Set velocity
        rb.velocity = direction * speed;
    }
    
    private void UpdateFacingDirection(float xDirection)
    {
        // Only flip if the direction is significant (more than a small threshold)
        if (Mathf.Abs(xDirection) > 0.1f)
        {
            bool shouldFaceRight = xDirection > 0;
            
            // Only flip if we actually need to change direction
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
    }
    
    private void FlipSprite(bool shouldFaceRight)
    {
        // Store current facing direction
        isFacingRight = shouldFaceRight;
        
        if (debugOrientation)
        {
            Debug.Log($"Fish should face: {(shouldFaceRight ? "RIGHT" : "LEFT")}, Artwork naturally faces: {(artworkFacesRight ? "RIGHT" : "LEFT")}", this);
        }
        
        if (spriteRenderer != null)
        {
            // Determine if we need to flip the sprite
            // Only flip if the desired direction doesn't match artwork's natural direction
            bool needToFlip = (shouldFaceRight != artworkFacesRight);
            
            // Apply flip state
            spriteRenderer.flipX = needToFlip;
            
            if (debugOrientation)
            {
                Debug.Log($"Setting spriteRenderer.flipX = {needToFlip}", this);
            }
        }
        else
        {
            // No sprite renderer, use transform scale instead
            Vector3 scale = transform.localScale;
            float xScale = Mathf.Abs(scale.x);
            
            // If we want to face right but artwork faces left naturally (or vice versa)
            if (shouldFaceRight != artworkFacesRight)
            {
                scale.x = -xScale; // Flip
            }
            else
            {
                scale.x = xScale; // No flip
            }
            
            transform.localScale = scale;
            
            if (debugOrientation)
            {
                Debug.Log($"Setting transform.localScale.x = {scale.x}", this);
            }
        }
    }
    
    private void GenerateRandomPatrolPoints()
    {
        randomPatrolPoints.Clear();
        
        // Always include the start position
        randomPatrolPoints.Add(startPosition);
        
        // Generate additional random points
        for (int i = 0; i < maxRandomPatrolPoints - 1; i++)
        {
            Vector2 randomOffset = Random.insideUnitCircle * patrolRadius;
            randomPatrolPoints.Add(startPosition + randomOffset);
        }
        
        // Log the patrol points for debugging
        if (showDebugGizmos)
        {
            string pointList = "";
            foreach (Vector2 point in randomPatrolPoints)
            {
                pointList += point.ToString() + ", ";
            }
            Debug.Log($"BadFishAI: Generated random patrol points: {pointList}");
        }
    }
    
    private void SetNextPatrolTarget()
    {
        if (useRandomPatrolPoints && randomPatrolPoints.Count > 0)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % randomPatrolPoints.Count;
            currentTarget = randomPatrolPoints[currentPatrolIndex];
        }
        else if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
            currentTarget = patrolPoints[currentPatrolIndex];
        }
        else
        {
            // Fallback if no patrol points are defined
            currentTarget = startPosition;
        }
    }
    
    // Public method for other scripts to call when the fish is attacked
    public void OnAttacked()
    {
        BeginChasing();
    }
    
    // Called when something hits the fish (player or projectile)
    private void BeginChasing()
    {
        // Become aggro and chase the player
        isAggro = true;
        aggroTimer = MAX_AGGRO_TIME;
        
        if (currentState != FishState.Stunned)
        {
            currentState = FishState.Chase;
        }
    }
    
    private void ReturnToPatrol()
    {
        currentState = FishState.Patrol;
        SetNextPatrolTarget();
    }
    
    // Call this when the fish takes a hit that should stun it briefly
    public void StunFish(float duration)
    {
        currentState = FishState.Stunned;
        stateTimer = duration;
        rb.velocity = Vector2.zero; // Stop movement during stun
    }
    
    // Called by other scripts when fish health changes
    public void OnHealthChanged(int currentHealth, int maxHealth)
    {
        // Could implement fleeing behavior at low health here
        float healthPercentage = (float)currentHealth / maxHealth;
        
        if (healthPercentage < 0.3f)
        {
            // Optionally switch to flee behavior when below 30% health
            // currentState = FishState.Flee;
        }
    }
    
    // Called when player comes into collision with the fish
    public void OnPlayerCollision()
    {
        BeginChasing();
    }
    
    // Show debug visualization
    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;
        
        // Show detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, playerDetectionRange);
        
        // Show aggro range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        
        // Show patrol radius
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : transform.position, patrolRadius);
        
        // Show patrol points
        Gizmos.color = Color.green;
        
        if (Application.isPlaying)
        {
            // Show actual patrol points being used
            if (useRandomPatrolPoints && randomPatrolPoints.Count > 0)
            {
                foreach (Vector2 point in randomPatrolPoints)
                {
                    Gizmos.DrawSphere(point, 0.2f);
                }
                
                // Show current target
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentTarget, 0.3f);
            }
            else if (patrolPoints != null && patrolPoints.Length > 0)
            {
                foreach (Vector2 point in patrolPoints)
                {
                    Gizmos.DrawSphere(point, 0.2f);
                }
                
                // Show current target
                Gizmos.color = Color.blue;
                Gizmos.DrawSphere(currentTarget, 0.3f);
            }
        }
        else
        {
            // Show configured patrol points in editor
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                foreach (Vector2 point in patrolPoints)
                {
                    Gizmos.DrawSphere(point, 0.2f);
                }
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the object has a tag before comparing
        if (other.tag != null && other.tag != "Untagged")
        {
            // Safe way to check for player projectile
            if (other.CompareTag("PlayerProjectile"))
            {
                // Become aggro if hit by a player projectile
                BeginChasing();
            }
        }
    }
    
    // Add this after the Start method to fix orientation issues
    private void CheckAndFixOrientation()
    {
        // Determine current world position
        Vector3 currentPos = transform.position;
        
        // Find target to check orientation
        Vector3 targetPos;
        
        // Use patrol point or player position to check orientation
        if (player != null && currentState == FishState.Chase)
        {
            targetPos = player.transform.position;
        }
        else if (randomPatrolPoints.Count > 0 && currentPatrolIndex < randomPatrolPoints.Count)
        {
            targetPos = randomPatrolPoints[currentPatrolIndex];
        }
        else if (patrolPoints != null && patrolPoints.Length > 0 && currentPatrolIndex < patrolPoints.Length)
        {
            targetPos = patrolPoints[currentPatrolIndex];
        }
        else
        {
            // Default: just use a point to the right
            targetPos = transform.position + Vector3.right;
        }
        
        // Calculate movement direction
        Vector2 moveDirection = (targetPos - currentPos).normalized;
        
        // If significant horizontal movement
        if (Mathf.Abs(moveDirection.x) > 0.1f)
        {
            // Should the fish face right?
            bool shouldFaceRight = moveDirection.x > 0;
            
            // Force the sprite to face the correct direction
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
    }
    
    // Add a debug method to help users fix orientation issues
    [ContextMenu("Toggle Orientation Debug Mode")]
    public void ToggleOrientationDebug()
    {
        debugOrientation = !debugOrientation;
        Debug.Log($"Fish orientation debug mode: {(debugOrientation ? "ENABLED" : "DISABLED")}", this);
        
        if (debugOrientation)
        {
            Debug.Log($"ORIENTATION INFO - Artwork faces: {(artworkFacesRight ? "RIGHT" : "LEFT")}, " +
                      $"Currently facing: {(isFacingRight ? "RIGHT" : "LEFT")}", this);
        }
    }
    
    // Called when the player dies - triggers feeding frenzy behavior
    public void OnPlayerDeath(Vector3 playerDeathPosition)
    {
        // Reset any existing coroutines or states
        StopAllCoroutines();

        // Reset sprite color to normal (prevents flicker)
        if (spriteRenderer != null)
        {
            spriteRenderer.color = isBoss ? bossColor : Color.white;
        }
        // Also reset any flash/invulnerability effects in badFishHealth
        if (healthComponent != null)
        {
            healthComponent.ResetVisuals();
        }

        // Set fish to flee away from the player's death position
        StartCoroutine(FleeFromPlayerDeath(playerDeathPosition, 3f)); // Flee for 3 seconds
    }

    // Coroutine to handle fleeing behavior after player death
    private IEnumerator FleeFromPlayerDeath(Vector3 playerDeathPosition, float fleeDuration)
    {
        currentState = FishState.Flee;
        float startTime = Time.time;
        Vector2 fleeDirection = ((Vector2)transform.position - (Vector2)playerDeathPosition).normalized;
        if (fleeDirection == Vector2.zero) fleeDirection = Vector2.right; // Default if overlapping
        float fleeSpeed = chaseSpeed * 1.2f; // Slightly faster than chase

        while (Time.time - startTime < fleeDuration)
        {
            rb.velocity = fleeDirection * fleeSpeed;
            UpdateFacingDirection(fleeDirection.x);
            yield return null;
        }

        // After fleeing, return to patrol
        ReturnToPatrol();
    }
    
    /// <summary>
    /// Resets the fish to its initial state when player respawns
    /// </summary>
    public void ResetToInitialState()
    {
        // Stop any running coroutines
        StopAllCoroutines();
        
        // Reset the fish's state
        currentState = FishState.Patrol;
        isAggro = false;
        aggroTimer = 0f;
        
        // Reset the fish's speed
        chaseSpeed = patrolSpeed * 2f; // Reset to default (assuming patrol speed is half of chase)
        
        // Reset orientation
        CheckAndFixOrientation();
        
        // Reset physics
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // Regenerate patrol points (optional - only if you want to change patrol paths)
        if (useRandomPatrolPoints)
        {
            GenerateRandomPatrolPoints();
        }
        
        // Set next patrol target
        SetNextPatrolTarget();
        
        Debug.Log($"{gameObject.name} reset to initial state", this);
    }
    
    /// <summary>
    /// Applies boss attributes if this fish is marked as a boss
    /// </summary>
    private void SetupBossAttributes()
    {
        // Apply visual changes
        if (spriteRenderer != null)
        {
            spriteRenderer.color = bossColor;
            
            // Make boss fish slightly larger
            Transform spriteTransform = spriteRenderer.transform;
            spriteTransform.localScale = new Vector3(
                spriteTransform.localScale.x * 1.3f,
                spriteTransform.localScale.y * 1.3f,
                spriteTransform.localScale.z
            );
        }
        
        // Apply stat boosts - store original values first
        float originalPatrolSpeed = patrolSpeed;
        float originalChaseSpeed = chaseSpeed;
        float originalDetectionRange = playerDetectionRange;
        float originalAggroRange = aggroRange;
        
        // Apply speed multipliers
        patrolSpeed *= bossSpeedMultiplier;
        chaseSpeed *= bossSpeedMultiplier;
        playerDetectionRange *= bossAggroRangeMultiplier;
        aggroRange *= bossAggroRangeMultiplier;
        
        // Ensure we have a significant boost in speed
        if (patrolSpeed < originalPatrolSpeed * 1.2f)
        {
            patrolSpeed = originalPatrolSpeed * 1.5f;
        }
        
        if (chaseSpeed < originalChaseSpeed * 1.2f)
        {
            chaseSpeed = originalChaseSpeed * 1.5f;
        }
        
        // Log the changes for verification
        if (showBossDebugLogs)
        {
            Debug.Log($"BOSS FISH: {gameObject.name} - Speed increased from {originalPatrolSpeed}/{originalChaseSpeed} to {patrolSpeed}/{chaseSpeed}");
            Debug.Log($"BOSS FISH: {gameObject.name} - Detection/Aggro range increased from {originalDetectionRange}/{originalAggroRange} to {playerDetectionRange}/{aggroRange}");
        }
        
        // Check for and disable any components that might be destroying this fish
        MonoBehaviour[] allComponents = GetComponents<MonoBehaviour>();
        foreach (MonoBehaviour component in allComponents)
        {
            // Skip this component itself
            if (component == this) continue;
            
            string componentName = component.GetType().Name.ToLower();
            if (componentName.Contains("destroy") || 
                componentName.Contains("despawn") || 
                componentName.Contains("pool") || 
                componentName.Contains("offscreen") || 
                componentName.Contains("lifecycle") ||
                componentName.Contains("respawn") ||
                componentName.Contains("cull"))
            {
                // This might be a component that destroys the fish
                component.enabled = false;
                Debug.Log($"Boss fish: Disabled '{component.GetType().Name}' to prevent despawning", this);
            }
        }
        
        // If there's a Collider2D, make sure it stays active for interaction
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        
        // Notify health component if it exists
        if (healthComponent != null)
        {
            healthComponent.SendMessage("SetBossStatus", true, SendMessageOptions.DontRequireReceiver);
        }
        
        // Log that this is a boss fish
        Debug.Log($"{gameObject.name} initialized as a BOSS fish with enhanced attributes!", this);
    }
    
    // Add a context menu option to help with debugging
    [ContextMenu("Debug Boss Status")]
    public void DebugBossStatus()
    {
        Debug.Log($"BOSS STATUS FOR: {gameObject.name}", this);
        Debug.Log($"- Is Boss: {isBoss}", this);
        Debug.Log($"- Current Position: {transform.position}", this);
        Debug.Log($"- Start Position: {startPosition}", this);
        Debug.Log($"- Current State: {currentState}", this);
        Debug.Log($"- Is Aggro: {isAggro}, Time Left: {aggroTimer:F1}s", this);
        Debug.Log($"- Is Facing Right: {isFacingRight}", this);
        
        // Check for viewport position
        if (Camera.main != null)
        {
            Vector3 viewportPos = Camera.main.WorldToViewportPoint(transform.position);
            bool isVisible = (viewportPos.x > 0 && viewportPos.x < 1 && viewportPos.y > 0 && viewportPos.y < 1);
            Debug.Log($"- Viewport Position: {viewportPos}, Is Visible: {isVisible}", this);
        }
        
        // Check components
        Debug.Log($"- Has SpriteRenderer: {spriteRenderer != null}", this);
        Debug.Log($"- Has Rigidbody2D: {rb != null}", this);
        Debug.Log($"- Has Health Component: {healthComponent != null}", this);
    }
    
    // Called when this gameObject is destroyed - helps us debug why boss fish are being destroyed
    private void OnDestroy()
    {
        // Only log for boss fish
        if (isBoss)
        {
            Debug.LogWarning($"BOSS FISH DESTROYED: {gameObject.name} at position {transform.position}", this);
            
            // Print a stack trace to help identify what's destroying the boss
            string stackTrace = System.Environment.StackTrace;
            Debug.LogWarning($"Destruction stack trace: {stackTrace}");
        }
    }
    
    // Public method to set boss status from other scripts
    public void SetBossStatus(bool status)
    {
        if (status != isBoss)
        {
            isBoss = status;
            
            // Apply boss attributes if becoming a boss
            if (isBoss)
            {
                SetupBossAttributes();
            }
            else
            {
                // Reset to default values if no longer a boss
                // This would need to store original values - not implemented here
            }
            
            // Sync with health component
            if (healthComponent != null)
            {
                healthComponent.SetBossStatus(isBoss);
            }
        }
    }

    private void FixedUpdate()
    {
        // Ensure body type remains Dynamic (in case some other script changes it)
        if (rb != null && rb.bodyType != RigidbodyType2D.Dynamic)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            Debug.LogWarning($"BadFishAI: Something changed the Rigidbody2D type to {rb.bodyType}. Restoring to Dynamic.", this);
        }
        
        // Handle collision avoidance
        if (rb != null && rb.velocity.sqrMagnitude > 0)
        {
            AvoidCollisions();
        }
    }

    /// <summary>
    /// Checks for and avoids collisions with obstacles
    /// </summary>
    private void AvoidCollisions()
    {
        // Get current movement direction
        Vector2 moveDirection = rb.velocity.normalized;
        
        // Cast multiple rays in a fan pattern to detect obstacles
        float angleStep = 45f; // Degrees between each ray
        int rayCount = 5; // Number of rays to cast (including center)
        Vector2 avoidanceForce = Vector2.zero;
        
        for (int i = 0; i < rayCount; i++)
        {
            // Calculate ray direction
            float angle = (i - (rayCount - 1) / 2) * angleStep;
            Vector2 rayDirection = Quaternion.Euler(0, 0, angle) * moveDirection;
            
            // Cast ray
            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                rayDirection,
                collisionAvoidanceRadius,
                collisionLayers
            );
            
            // Debug visualization
            if (showDebugGizmos)
            {
                Debug.DrawRay(
                    transform.position,
                    rayDirection * collisionAvoidanceRadius,
                    hit.collider != null ? Color.red : Color.green
                );
            }
            
            // If we hit something, calculate avoidance force
            if (hit.collider != null)
            {
                // Calculate force away from the collision
                Vector2 avoidDirection = -hit.normal;
                float distanceFactor = 1f - (hit.distance / collisionAvoidanceRadius);
                avoidanceForce += avoidDirection * collisionAvoidanceForce * distanceFactor;
            }
        }
        
        // Apply avoidance force if needed
        if (avoidanceForce != Vector2.zero)
        {
            // Normalize and apply the force
            avoidanceForce.Normalize();
            rb.AddForce(avoidanceForce * collisionAvoidanceForce, ForceMode2D.Force);
            
            // Update facing direction based on new movement
            UpdateFacingDirection(rb.velocity.x);
        }
    }

    private void OnDrawGizmos()
    {
        if (showDebugGizmos)
        {
            // Draw collision avoidance radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, collisionAvoidanceRadius);
            
            // Draw current velocity direction
            if (rb != null && rb.velocity.sqrMagnitude > 0)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(transform.position, rb.velocity.normalized * collisionAvoidanceRadius);
            }
        }
    }
    
    // Log all physical collisions with the fish
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Log detailed collision information
        string layerName = LayerMask.LayerToName(collision.gameObject.layer);
        Debug.Log($"BadFishAI: PHYSICAL COLLISION with {collision.gameObject.name} on layer {layerName}", this);
        
        // Additional details about the collision
        if (collision.contactCount > 0)
        {
            ContactPoint2D contact = collision.GetContact(0);
            Debug.Log($"  - Contact point: {contact.point}, Normal: {contact.normal}", this);
            Debug.Log($"  - Relative velocity: {collision.relativeVelocity}, Rigidbody type: {(rb != null ? rb.bodyType.ToString() : "No Rigidbody")}", this);
        }
    }

    private void ValidateColliders()
    {
        // Get all colliders on this GameObject and its children
        Collider2D[] allColliders = GetComponentsInChildren<Collider2D>();
        
        // Check if we have any colliders at all
        if (allColliders.Length == 0)
        {
            Debug.LogError($"BadFishAI on {gameObject.name}: NO COLLIDERS FOUND! Fish will pass through obstacles.", this);
            return;
        }
        
        // Check if we have at least one non-trigger collider for physics collisions
        bool hasNonTriggerCollider = false;
        foreach (Collider2D collider in allColliders)
        {
            if (!collider.isTrigger)
            {
                hasNonTriggerCollider = true;
                Debug.Log($"BadFishAI: Found non-trigger collider {collider.GetType().Name} on {collider.gameObject.name} for physics collisions", this);
            }
            else
            {
                Debug.Log($"BadFishAI: Found trigger collider {collider.GetType().Name} on {collider.gameObject.name} (trigger colliders don't block movement)", this);
            }
        }
        
        if (!hasNonTriggerCollider)
        {
            Debug.LogError($"BadFishAI on {gameObject.name}: All colliders are TRIGGERS! Fish will pass through obstacles. Add at least one non-trigger collider.", this);
        }
        
        // Check rigidbody settings
        if (rb != null)
        {
            Debug.Log($"BadFishAI: Rigidbody2D settings - Type: {rb.bodyType}, CollisionDetection: {rb.collisionDetectionMode}, " +
                      $"Interpolation: {rb.interpolation}, Gravity: {rb.gravityScale}", this);
            
            // Check if using 'Discrete' collision detection
            if (rb.collisionDetectionMode == CollisionDetectionMode2D.Discrete)
            {
                Debug.LogWarning($"BadFishAI on {gameObject.name}: Using DISCRETE collision detection. Fast-moving fish may pass through thin colliders.", this);
            }
            
            // Check if body type isn't Dynamic
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                Debug.LogWarning($"BadFishAI on {gameObject.name}: Body type is {rb.bodyType} instead of Dynamic. Fish may not collide properly with static obstacles.", this);
            }
        }
        
        // Check layer collisions in Physics2D settings
        int thisLayer = gameObject.layer;
        string thisLayerName = LayerMask.LayerToName(thisLayer);
        Debug.Log($"BadFishAI: This fish is on layer '{thisLayerName}' (#{thisLayer}).", this);
        
        // Check Physics2D layer collision matrix
        Debug.Log("BadFishAI: Checking Physics2D layer collision matrix:", this);
        
        // Try to find floor/ground and fishblocker layers
        for (int otherLayer = 0; otherLayer < 32; otherLayer++)
        {
            string otherLayerName = LayerMask.LayerToName(otherLayer);
            if (string.IsNullOrEmpty(otherLayerName)) continue;
            
            // Check if this is a layer we care about
            bool isFloorLayer = otherLayerName.ToLower().Contains("floor") || 
                                otherLayerName.ToLower().Contains("ground");
            bool isFishBlockerLayer = otherLayerName.ToLower().Contains("fishblocker") || 
                                      (otherLayerName.ToLower().Contains("fish") && otherLayerName.ToLower().Contains("block"));
            
            if (isFloorLayer || isFishBlockerLayer)
            {
                bool canCollide = AreLayersColliding(thisLayer, otherLayer);
                Debug.Log($"  - This layer ({thisLayerName}) {(canCollide ? "CAN" : "CANNOT")} collide with {otherLayerName}", this);
                
                if (!canCollide)
                {
                    Debug.LogError($"PHYSICS2D LAYER ISSUE: {thisLayerName} and {otherLayerName} are NOT set to collide in Physics2D settings!", this);
                    Debug.LogError("Open Edit > Project Settings > Physics 2D and check the Layer Collision Matrix", this);
                }
            }
        }
        
        // Check collisionLayers property for avoidance
        if (collisionLayers.value == 0)
        {
            Debug.LogError($"BadFishAI on {gameObject.name}: collisionLayers is set to NOTHING (0). Fish won't detect obstacles to avoid!", this);
        }
        else
        {
            // Log which layers are included in collision avoidance
            Debug.Log($"BadFishAI: Collision avoidance is set up for these layers:", this);
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & collisionLayers.value) != 0)
                {
                    string layerName = LayerMask.LayerToName(i);
                    Debug.Log($"  - Layer {i}: {(string.IsNullOrEmpty(layerName) ? "Unnamed" : layerName)}", this);
                }
            }
            
            // Try to determine common layer names for floors and walls
            bool containsFloor = false;
            bool containsWall = false;
            bool containsFishBlocker = false;
            
            for (int i = 0; i < 32; i++)
            {
                if (((1 << i) & collisionLayers.value) != 0)
                {
                    string layerName = LayerMask.LayerToName(i).ToLower();
                    if (layerName.Contains("floor") || layerName.Contains("ground") || layerName.Contains("terrain"))
                    {
                        containsFloor = true;
                    }
                    if (layerName.Contains("wall") || layerName.Contains("obstacle") || layerName.Contains("block"))
                    {
                        containsWall = true;
                    }
                    if (layerName.Contains("fish") && (layerName.Contains("block") || layerName.Contains("barrier")))
                    {
                        containsFishBlocker = true;
                    }
                }
            }
            
            if (!containsFloor)
            {
                Debug.LogWarning($"BadFishAI: No 'Floor' or 'Ground' layers detected in collisionLayers. Fish may not avoid floors!", this);
            }
            if (!containsWall)
            {
                Debug.LogWarning($"BadFishAI: No 'Wall' or 'Obstacle' layers detected in collisionLayers. Fish may not avoid walls!", this);
            }
            if (!containsFishBlocker)
            {
                Debug.LogWarning($"BadFishAI: No 'FishBlocker' layer detected in collisionLayers. Fish may not avoid fish blockers!", this);
            }
        }
    }
    
    // Helper method to check if two layers are set to collide in Physics2D settings
    private bool AreLayersColliding(int layer1, int layer2)
    {
        // Get the value directly from Physics2D
        return Physics2D.GetIgnoreLayerCollision(layer1, layer2) == false;
    }

    void OnEnable()
    {
        // Check rigidbody state when fish is enabled/activated
        if (rb != null)
        {
            // Log current state
            Debug.Log($"BadFishAI OnEnable - Rigidbody2D state: Type={rb.bodyType}, GameObject={gameObject.name}", this);
            
            // Force to Dynamic if needed
            if (rb.bodyType != RigidbodyType2D.Dynamic)
            {
                Debug.LogWarning($"BadFishAI OnEnable - Found Rigidbody2D as {rb.bodyType}, forcing to Dynamic", this);
                rb.bodyType = RigidbodyType2D.Dynamic;
            }
        }
    }
} 