using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool showDebugMessages = false;
    
    [Header("UI References")]
    public PlayerHealthBar healthBar; // Made public for easier external assignment
    
    [Header("Enemy Collision")]
    [SerializeField] private float knockbackForce = 2000f; // Increased knockback force
    [SerializeField] private float knockbackDuration = 0.2f; // How long knockback lasts
    [SerializeField] private float invulnerabilityDuration = 1.5f; // Invulnerability time after being hit
    [SerializeField] private bool showFlashOnHit = true; // Visual feedback when hit
    [SerializeField] private bool useDebugKnockback = true; // For debugging - always shows direction
    
    [Header("Death & Respawn")]
    [SerializeField] private string spawnPointName = "PlayerSpawn"; // Name of the spawn point
    [SerializeField] private float respawnInvulnerabilityTime = 3f; // Short invulnerability after respawn
    [SerializeField] private bool disableControlsOnDeath = true; // Whether to disable player controls on death
    [SerializeField] public float deathSlowMotionScale = 0.15f; // Time scale during death (slow motion - lower value for more dramatic effect)
    [SerializeField] private Color screenDimColor = new Color(0, 0, 0, 0.6f); // Color for screen dimming effect
    
    [Header("Death Sink Settings")]
    [SerializeField] private float deathSinkSpeed = 1.5f; // How fast the player sinks after death
    
    [Header("Effects")]
    [SerializeField] private ParticleSystem bleedingEffectParticles; // Assign Particle System component from Player's child
    [SerializeField] private Animator bloodSplatAnimator; // Assign Animator component from bloodSpatter_1 child
    
    // Current health tracking
    private int currentHealth;
    private bool isInvulnerable = false;
    private Rigidbody2D rb; // Reference to player's rigidbody
    private SpriteRenderer spriteRenderer; // For visual feedback when hit
    private Coroutine flashCoroutine;
    private Coroutine invulnerabilityCoroutine;
    private bool isDead = false;
    private Vector3 respawnPosition;
    private DiverMovement diverMovement; // Player movement script
    
    // Reference to screen dim overlay
    private GameObject screenDimOverlay;
    
    // Store original fixed delta time for proper reset when respawning
    private float originalFixedDeltaTime;
    
    // Add these fields
    private DiverShooter diverShooter;
    private ArmAim armAim;
    private PlayerOxygen playerOxygen;

    // State Machine reference
    private PlayerStateMachine stateMachine;
    
    void Awake()
    {
        // Get required components
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        diverMovement = GetComponent<DiverMovement>();
        playerOxygen = GetComponent<PlayerOxygen>();
        
        if (rb == null)
        {
            Debug.LogError("PlayerHealth: No Rigidbody2D found on player", this);
        }
        
        // Create screen dim overlay that will be used when player dies
        CreateScreenDimOverlay();
        
        // Find spawn point and store position for respawn (do NOT move the player here)
        string effectiveSpawnPointName = spawnPointName;
        if (!string.IsNullOrEmpty(SceneTransitionData.nextSpawnPointName))
        {
            var overrideSpawn = GameObject.Find(SceneTransitionData.nextSpawnPointName);
            if (overrideSpawn != null)
            {
                Debug.Log($"[PlayerHealth] Using teleport override spawn point: {SceneTransitionData.nextSpawnPointName}", this);
                effectiveSpawnPointName = SceneTransitionData.nextSpawnPointName;
            }
            else
            {
                Debug.LogWarning($"[PlayerHealth] Teleport override spawn point '{SceneTransitionData.nextSpawnPointName}' not found, falling back to default.", this);
            }
            // Do NOT clear SceneTransitionData.nextSpawnPointName here!
        }
        GameObject spawnPoint = GameObject.Find(effectiveSpawnPointName);
        if (spawnPoint != null)
        {
            respawnPosition = spawnPoint.transform.position;
        }
        else
        {
            // If no spawn point is found, use current position
            respawnPosition = transform.position;
            Debug.LogWarning($"PlayerHealth: No spawn point named '{effectiveSpawnPointName}' found. Using current position.", this);
        }
        
        // Find and cache references to arm/aim scripts on ArmPivot child
        Transform armPivot = transform.Find("ArmPivot");
        if (armPivot != null)
        {
            diverShooter = armPivot.GetComponent<DiverShooter>();
            armAim = armPivot.GetComponent<ArmAim>();
        }

        // Get State Machine
        stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine == null)
        {
            Debug.LogError("PlayerHealth: Could not find PlayerStateMachine component!", this);
        }
    }
    
    // Initialize health on startup
    void Start()
    {
        // Store original fixed delta time
        originalFixedDeltaTime = Time.fixedDeltaTime;
        
        // Explicitly set to full health
        currentHealth = maxHealth;
        
        // Try to find health bar if not assigned
        FindHealthBar();
        
        // Make sure the health bar starts at full health
        if (healthBar != null)
        {
            // Force initialize the health bar first
            healthBar.ForceInitialize();
            
            // THEN update it to full health
            UpdateHealthBar();
            
            // Log confirmation
            Debug.Log("Initial health bar setup complete with full health.");
        }
        
        // Add a delayed second refresh to handle any timing issues
        Invoke("ForceRefreshHealthBar", 0.2f);
        
        if (showDebugMessages)
        {
            Debug.Log($"Player initialized with {currentHealth} health");
        }
    }
    
    // Handle both regular collisions and triggers
    void OnCollisionEnter2D(Collision2D collision)
    {
        GameObject otherObject = collision.gameObject;

        // --- Check for Red Orb Collection ---
        if (otherObject.CompareTag("airBubble")) // Check if it's tagged like our RedOrb
        {
            RedOrbCollect orbCollect = otherObject.GetComponent<RedOrbCollect>();
            if (orbCollect != null)
            {
                // --- Add check: Only collect if health is not full ---
                if (currentHealth < maxHealth)
                {
                    int healAmount = orbCollect.GetHealAmount();
                    Debug.Log($"[PlayerHealth] Collided with Red Orb. Healing for {healAmount}.", this);
                    TakeDamage(-healAmount); // Apply healing (negative damage)
                    Destroy(otherObject); // Destroy the orb
                }
                else
                {
                    Debug.Log($"[PlayerHealth] Collided with Red Orb, but health is full. Orb not collected.", this);
                    // Optionally, play a "cannot collect" sound or effect here?
                }
                return; // Exit early, don't process as enemy hit
            }
        }

        // --- Existing Enemy Hit Logic ---
        HandleEnemyHit(otherObject);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        // Only handle enemy fish or jellyfish collisions
        if (collider.CompareTag("BadFish") || collider.GetComponent<EnemyCollision>() != null || collider.GetComponent<JellyfishHealth>() != null)
        {
            HandleEnemyHit(collider.gameObject);
        }
        // Otherwise, ignore (do not trigger hurt logic for WorldBounds or other triggers)
    }

    // Centralized enemy hit logic with optional damage parameter
    private void HandleEnemyHit(GameObject enemy, int customDamage = 0)
    {
        // NOTE: Red Orb logic is now handled in OnCollisionEnter2D above
        //       This function now only handles actual damaging enemies.
        Debug.Log($"[PlayerHealth] HandleEnemyHit check for {enemy.name}", enemy);
        
        // Check if this is an enemy by looking for either EnemyCollision or JellyfishHealth component
        bool isEnemy = enemy.GetComponent<EnemyCollision>() != null || 
                       enemy.GetComponent<JellyfishHealth>() != null;
        
        if (!isInvulnerable && isEnemy)
        {
            Debug.Log("[PlayerHealth] Player not invulnerable, applying damage and effects.");
            // Get direction from enemy to player for knockback
            Vector2 knockbackDirection = (transform.position - enemy.transform.position).normalized;
            
            // Make the Y component stronger to ensure upward movement
            knockbackDirection.y = Mathf.Abs(knockbackDirection.y) + 0.5f;
            knockbackDirection = knockbackDirection.normalized;
            
            if (showDebugMessages || useDebugKnockback)
            {
                Debug.Log($"Player collided with {enemy.name} - Knockback direction: {knockbackDirection}", this);
                // Draw a debug ray showing knockback direction
                Debug.DrawRay(transform.position, knockbackDirection * 3f, Color.red, 2f);
            }
            
            // Get damage amount from the enemy if it has EnemyCollision component
            int damageAmount = customDamage;
            if (damageAmount <= 0)
            {
                EnemyCollision enemyCollision = enemy.GetComponent<EnemyCollision>();
                if (enemyCollision != null)
                {
                    // Get damage amount from the enemy component
                    damageAmount = enemyCollision.GetDamageAmount();
                }
                else
                {
                    // Default damage if enemy doesn't specify
                    damageAmount = 10;
                    Debug.LogWarning($"Enemy {enemy.name} has no EnemyCollision component - using default damage of 10", this);
                }
            }
            
            // Apply damage
            TakeDamage(damageAmount);
            
            // --- Trigger State Machine for Damage Effects ---
            // Instead of calling ApplyKnockback directly, trigger the state
            stateMachine?.TriggerDamageState(knockbackDuration);
            // Note: Bleeding/Splat effects might need to move to TakeDamageState.Enter()
            // if they shouldn't play if the damage doesn't trigger the state (e.g., if already dead)

            // Start the bleeding particle effect
            if (bleedingEffectParticles != null)
            {
                Debug.Log("[PlayerHealth] Playing Bleeding Effect Particles.", bleedingEffectParticles);
                bleedingEffectParticles.Play();
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] Bleeding Effect Particles system is not assigned in the Inspector!", this);
            }
            
            // Trigger the blood splat animation
            if (bloodSplatAnimator != null)
            {
                // Use the exact name of the trigger parameter in your Animator Controller
                bloodSplatAnimator.SetTrigger("Splat"); 
                Debug.Log("[PlayerHealth] Triggering Blood Splat Animation.");
            }
            else
            {
                Debug.LogWarning("[PlayerHealth] Blood Splat Animator is not assigned!", this);
            }
            
            // Make player briefly invulnerable
            StartInvulnerability();
        }
        else if (isInvulnerable && isEnemy)
        {
            Debug.Log("[PlayerHealth] Player is invulnerable, ignoring enemy hit.");
        }
    }
    
    /// <summary>
    /// Makes player invulnerable for a short duration
    /// </summary>
    private void StartInvulnerability()
    {
        // Stop any existing invulnerability
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
        }
        
        // Start new invulnerability period
        invulnerabilityCoroutine = StartCoroutine(InvulnerabilityCoroutine());
        
        // Visual feedback
        if (showFlashOnHit && spriteRenderer != null)
        {
            // Stop existing flash if any
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }
            
            // Start new flash effect
            flashCoroutine = StartCoroutine(FlashSpriteCoroutine());
        }
    }
    
    /// <summary>
    /// Coroutine for invulnerability period
    /// </summary>
    private IEnumerator InvulnerabilityCoroutine()
    {
        isInvulnerable = true;
        
        yield return new WaitForSeconds(invulnerabilityDuration);
        
        isInvulnerable = false;
        invulnerabilityCoroutine = null;

        // Stop the bleeding particle effect when invulnerability ends
        if (bleedingEffectParticles != null)
        {
            Debug.Log("[PlayerHealth] Stopping Bleeding Effect Particles.");
            bleedingEffectParticles.Stop(); // Stops emission, allows existing particles to fade
        }
    }
    
    /// <summary>
    /// Coroutine for visual feedback when hit
    /// </summary>
    private IEnumerator FlashSpriteCoroutine()
    {
        if (spriteRenderer == null) yield break;
        
        Color originalColor = spriteRenderer.color;
        Color flashColor = Color.red;
        
        // Flash between colors
        float elapsedTime = 0;
        while (elapsedTime < invulnerabilityDuration)
        {
            float t = Mathf.PingPong(elapsedTime * 10, 1f);
            spriteRenderer.color = Color.Lerp(flashColor, originalColor, t);
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        // Ensure original color is restored
        spriteRenderer.color = originalColor;
        flashCoroutine = null;
    }
    
    // Find the health bar in the scene
    private void FindHealthBar()
    {
        if (healthBar == null)
        {
            // First look for PlayerUI/HealthBar specifically
            Transform playerUI = GameObject.Find("PlayerUI")?.transform;
            if (playerUI != null)
            {
                Transform healthBarTransform = playerUI.Find("HealthBar");
                if (healthBarTransform != null)
                {
                    healthBar = healthBarTransform.GetComponent<PlayerHealthBar>();
                    if (healthBar != null && showDebugMessages)
                    {
                        Debug.Log("PlayerHealth: Found health bar on PlayerUI/HealthBar", this);
                    }
                }
            }
            
            // If still not found, try general search
            if (healthBar == null)
            {
                healthBar = FindObjectOfType<PlayerHealthBar>();
                if (healthBar != null && showDebugMessages)
                {
                    Debug.Log("PlayerHealth: Found health bar in scene", this);
                }
                else if (showDebugMessages)
                {
                    Debug.LogWarning("PlayerHealth: No health bar assigned or found in the scene", this);
                }
            }
        }
    }
    
    /// <summary>
    /// Public method to damage the player
    /// </summary>
    /// <param name="amount">Amount of damage to apply</param>
    public void TakeDamage(int amount)
    {
        // --- ADDED: Log damage source --- 
        // Use Debug.LogWarning for visibility. Include stack trace to see who called this.
        Debug.LogWarning($"[{gameObject.name} - TakeDamage] Called with amount: {amount}. Current Health: {currentHealth}. Invulnerable: {isInvulnerable}", this);
        // Include stack trace for more context:
        // Debug.Log(System.Environment.StackTrace);
        // --- END LOG ---

        // Don't take damage if invulnerable and amount is positive (damage)
        if (isInvulnerable && amount > 0) return;
        // Ignore zero
        if (amount == 0) return;
        // Apply damage or healing
        currentHealth -= amount;
        // Clamp health between 0 and maxHealth
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        // Update the health bar
        UpdateHealthBar();
        if (showDebugMessages)
        {
            if (amount > 0)
                Debug.Log($"Player took {amount} damage, current health: {currentHealth}");
            else
                Debug.Log($"Player healed {-amount}, current health: {currentHealth}");
        }
        // Check if should die
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// Returns whether player is currently invulnerable
    /// </summary>
    public bool IsInvulnerable()
    {
        return isInvulnerable;
    }
    
    /// <summary>
    /// Updates the health bar with current health percentage
    /// </summary>
    private void UpdateHealthBar()
    {
        // Try to find health bar if not assigned
        if (healthBar == null)
        {
            FindHealthBar();
        }
        
        if (healthBar != null)
        {
            float healthPercent = (float)currentHealth / maxHealth;
            healthBar.UpdateHealth(healthPercent);
            
            if (showDebugMessages)
            {
                Debug.Log($"Updated health bar to {healthPercent * 100}%");
            }
            
            // Ensure the health bar is visible
            healthBar.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Cannot update health bar - none assigned or found");
        }
    }
    
    /// <summary>
    /// Force refresh the health bar - helps with initialization timing issues
    /// </summary>
    private void ForceRefreshHealthBar()
    {
        if (healthBar != null)
        {
            // Always ensure we're setting to the exact current health percentage
            float healthPercent = (float)currentHealth / maxHealth;
            
            // Reinitialize the health bar completely
            healthBar.ForceInitialize();
            
            // Explicitly set to full size
            healthBar.UpdateHealth(1.0f);
            
            // Then update with actual health (which should be full at start)
            healthBar.UpdateHealth(healthPercent);
            
            Debug.Log($"Forced health bar refresh with health percent: {healthPercent}");
        }
    }
    
    /// <summary>
    /// Creates a screen-wide overlay that will dim the screen when player dies
    /// </summary>
    private void CreateScreenDimOverlay()
    {
        // See if we already have a ScreenDimOverlay in the scene
        screenDimOverlay = GameObject.Find("ScreenDimOverlay");
        
        if (screenDimOverlay == null)
        {
            // Create the overlay
            screenDimOverlay = new GameObject("ScreenDimOverlay");
            
            // Add a Canvas component (sorting order above the game but below UI)
            Canvas overlayCanvas = screenDimOverlay.AddComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.sortingOrder = 90; // Just below UI (assuming UI is 100)
            
            // Add a Canvas Scaler for proper sizing
            CanvasScaler scaler = screenDimOverlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            // Add Image that covers the whole screen
            GameObject imageObj = new GameObject("DimImage");
            imageObj.transform.SetParent(screenDimOverlay.transform, false);
            Image dimImage = imageObj.AddComponent<Image>();
            dimImage.color = Color.clear; // Start transparent
            
            // Set the image to cover the whole screen
            RectTransform rectTransform = dimImage.rectTransform;
            rectTransform.anchorMin = Vector2.zero; // Bottom left
            rectTransform.anchorMax = Vector2.one;  // Top right
            rectTransform.sizeDelta = Vector2.zero; // Fill the entire parent
            rectTransform.anchoredPosition = Vector2.zero;
        }
        
        // Make sure it's disabled at start
        screenDimOverlay.SetActive(false);
        
        Debug.Log("Screen dim overlay created and ready");
    }
    
    /// <summary>
    /// Handle player death
    /// </summary>
    private void Die()
    {
        if (isDead) return; // Prevent multiple deaths
        
        isDead = true;
        
        // --- Trigger State Machine --- 
        stateMachine?.TriggerDeathState();
        // --------------------------- 

        // Always log death info for debugging
        Debug.Log("Player died! Current health: " + currentHealth);
        
        // --- Logic Moved to PlayerDeathState --- 
        // Activate slow motion - force it to be very slow for dramatic effect
        // Time.timeScale = deathSlowMotionScale;
        // Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale; // Adjust fixed timestep to match
        // Debug.Log($"Activated slow motion: Time.timeScale = {Time.timeScale}, fixedDeltaTime = {Time.fixedDeltaTime}");
        
        // Activate screen dimming effect
        // if (screenDimOverlay != null)
        // {
        //     screenDimOverlay.SetActive(true);
        //     Image dimImage = screenDimOverlay.GetComponentInChildren<Image>();
        //     if (dimImage != null)
        //     {
        //         dimImage.color = screenDimColor;
        //         Debug.Log("Screen dimming effect activated");
        //     }
        // }
        
        // Disable player controls if set
        // if (disableControlsOnDeath && diverMovement != null)
        // {
        //     diverMovement.enabled = false;
        //     Debug.Log("Disabled DiverMovement controls");
        // }
        // else if (diverMovement == null)
        // {
        //     Debug.LogError("Could not find DiverMovement component on player!");
        // }
        
        // Stop any physics and make the player completely static
        // if (rb != null)
        // {
        //     rb.velocity = Vector2.zero;
        //     rb.isKinematic = true;
        //     Debug.Log("Set player Rigidbody2D to kinematic and zeroed velocity");
        // }
        
        // Disable arm/aim scripts on death
        // if (diverShooter != null) diverShooter.enabled = false;
        // if (armAim != null) armAim.enabled = false;
        // -------------------------------------- 

        // Trigger feeding frenzy for nearby fish
        TriggerFeedingFrenzy();
    }
    
    /// <summary>
    /// Respawn the player (called by the UI button)
    /// </summary>
    public void Respawn()
    {
        if (!isDead) return;
        
        // Reset time scale and fixed delta time to defaults before reloading scene
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = originalFixedDeltaTime; // Use the stored original value
        Debug.Log($"Respawning: Restored Time.timeScale to 1.0 and Time.fixedDeltaTime to {originalFixedDeltaTime}");

        // Reload the current scene for a full reset
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    
    /// <summary>
    /// Test method for debugging - call this to manually damage the player
    /// </summary>
    public void TestDamage(int amount = 10)
    {
        TakeDamage(amount);
    }
    
    /// <summary>
    /// Returns the current health value
    /// </summary>
    public int GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// Returns the maximum health value
    /// </summary>
    public int GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// Returns the original fixedDeltaTime stored at the start.
    /// </summary>
    public float GetOriginalFixedDeltaTime()
    {
        return originalFixedDeltaTime;
    }
    
    /// <summary>
    /// Debug method to check player health state
    /// </summary>
    [ContextMenu("Test Health Bar")]
    public void TestHealthBar()
    {
        if (healthBar == null)
        {
            Debug.LogError("No health bar assigned");
            return;
        }
        
        // Test with 75% health
        currentHealth = Mathf.RoundToInt(maxHealth * 0.75f);
        UpdateHealthBar();
        Debug.Log($"Set health to {currentHealth}/{maxHealth} for testing");
    }
    
    /// <summary>
    /// Debug method to test knockback
    /// </summary>
    [ContextMenu("Test Knockback")]
    public void TestKnockback()
    {
        // Knockback in a random direction
        Vector2 randomDirection = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
        HandleEnemyHit(null, 10); // Use HandleEnemyHit with a custom damage
        Debug.Log($"Applied test knockback in direction: {randomDirection}");
    }
    
    [ContextMenu("Test Death UI")]
    public void TestDeathUI()
    {
        Debug.Log("Testing Death UI - Triggering Player Death");
        // Set health to 0 and call Die() directly
        currentHealth = 0;
        Die();
    }
    
    /// <summary>
    /// Triggers nearby fish to enter a feeding frenzy mode
    /// </summary>
    private void TriggerFeedingFrenzy()
    {
        // Find all bad fish in a radius around the player
        float feedingFrenzyRadius = 15f; // Adjust this range as needed
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(transform.position, feedingFrenzyRadius);
        int frenziedFishCount = 0;
        
        foreach (Collider2D collider in nearbyColliders)
        {
            if (collider.CompareTag("BadFish"))
            {
                BadFishAI fishAI = collider.GetComponent<BadFishAI>();
                if (fishAI != null)
                {
                    // Call the OnPlayerDeath method if it exists, otherwise fallback to OnAttacked
                    // We'll add the OnPlayerDeath method to BadFishAI next
                    if (fishAI.GetType().GetMethod("OnPlayerDeath") != null)
                    {
                        fishAI.SendMessage("OnPlayerDeath", transform.position);
                    }
                    else
                    {
                        fishAI.OnAttacked(); // Fallback to existing aggro behavior
                    }
                    frenziedFishCount++;
                }
            }
        }
        
        Debug.Log($"Triggered feeding frenzy for {frenziedFishCount} fish near the player");
    }

    public void ForceDie()
    {
        currentHealth = 0;
        // Call the main Die method which now handles the state transition
        Die(); 
        // stateMachine?.TriggerDeathState(); // This call is now inside Die()
    }
} 