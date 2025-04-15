using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private bool showDebugMessages = false;
    
    [Header("UI References")]
    public PlayerHealthBar healthBar; // Made public for easier external assignment
    
    [Header("Enemy Collision")]
    [SerializeField] private string enemyTag = "BadFish"; // Tag of enemy fish
    [SerializeField] private int collisionDamage = 10; // Damage taken when colliding with enemy
    [SerializeField] private float knockbackForce = 2000f; // Increased knockback force
    [SerializeField] private float knockbackDuration = 0.2f; // How long knockback lasts
    [SerializeField] private float invulnerabilityDuration = 1.5f; // Invulnerability time after being hit
    [SerializeField] private bool showFlashOnHit = true; // Visual feedback when hit
    [SerializeField] private bool useDebugKnockback = true; // For debugging - always shows direction
    
    [Header("Death & Respawn")]
    [SerializeField] private GameObject deathUIPanel; // Assign a UI panel with the respawn button
    [SerializeField] private string spawnPointName = "PlayerSpawn"; // Name of the spawn point
    [SerializeField] private float respawnInvulnerabilityTime = 3f; // Short invulnerability after respawn
    [SerializeField] private bool disableControlsOnDeath = true; // Whether to disable player controls on death
    [SerializeField] private float deathSlowMotionScale = 0.15f; // Time scale during death (slow motion - lower value for more dramatic effect)
    [SerializeField] private Color screenDimColor = new Color(0, 0, 0, 0.6f); // Color for screen dimming effect
    
    [Header("Death Sink Settings")]
    [SerializeField] private float deathSinkSpeed = 1.5f; // How fast the player sinks after death
    
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
        
        // Hide death UI at start if assigned
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }
        
        // Find spawn point and store position
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        if (spawnPoint != null)
        {
            respawnPosition = spawnPoint.transform.position;
        }
        else
        {
            // If no spawn point is found, use current position
            respawnPosition = transform.position;
            Debug.LogWarning($"PlayerHealth: No spawn point named '{spawnPointName}' found. Using current position.", this);
        }
        
        // Find and cache references to arm/aim scripts on ArmPivot child
        Transform armPivot = transform.Find("ArmPivot");
        if (armPivot != null)
        {
            diverShooter = armPivot.GetComponent<DiverShooter>();
            armAim = armPivot.GetComponent<ArmAim>();
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
        HandleEnemyHit(collision.gameObject);
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        HandleEnemyHit(collider.gameObject);
    }

    // Centralized enemy hit logic
    private void HandleEnemyHit(GameObject enemy)
    {
        // Check if collided with enemy and player is not invulnerable
        if (!isInvulnerable && enemy.CompareTag(enemyTag))
        {
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
            
            // Apply damage
            TakeDamage(collisionDamage);
            
            // Check if the enemy has its own EnemyCollision component
            // If it does, let it handle the knockback instead
            EnemyCollision enemyCollision = enemy.GetComponent<EnemyCollision>();
            if (enemyCollision == null)
            {
                // Only apply our own knockback if the enemy doesn't have an EnemyCollision component
                ApplyKnockback(knockbackDirection);
                
                if (showDebugMessages)
                {
                    Debug.Log("Using PlayerHealth knockback (enemy has no EnemyCollision component)");
                }
            }
            else
            {
                if (showDebugMessages)
                {
                    Debug.Log("Skipping PlayerHealth knockback as enemy has EnemyCollision component");
                }
            }
            
            // Make player briefly invulnerable
            StartInvulnerability();
        }
    }
    
    /// <summary>
    /// Applies knockback force in specified direction
    /// </summary>
    private void ApplyKnockback(Vector2 direction)
    {
        if (rb != null)
        {
            // Stop any existing knockback
            StopKnockback();
            
            // Apply immediate force
            rb.velocity = Vector2.zero; // Reset velocity first
            rb.AddForce(direction * knockbackForce, ForceMode2D.Impulse);
            
            if (showDebugMessages)
            {
                Debug.Log($"Applied knockback force: {direction * knockbackForce}");
            }
            
            // Start coroutine to clear knockback after duration
            StartCoroutine(ClearKnockbackAfterDelay());
        }
        else
        {
            Debug.LogError("Cannot apply knockback - Player has no Rigidbody2D component!", this);
        }
    }
    
    /// <summary>
    /// Clear knockback force after delay
    /// </summary>
    private IEnumerator ClearKnockbackAfterDelay()
    {
        yield return new WaitForSeconds(knockbackDuration);
        StopKnockback();
    }
    
    /// <summary>
    /// Stops any current knockback momentum
    /// </summary>
    public void StopKnockback()
    {
        if (rb != null)
        {
            // Optional - can either zero the velocity or let physics handle it
            // rb.velocity = Vector2.zero;
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
        // Don't take damage if invulnerable
        if (isInvulnerable) return;
        
        // Validate damage amount
        if (amount <= 0) return;
        
        // Apply damage
        currentHealth -= amount;
        
        // Clamp health to 0
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Update the health bar
        UpdateHealthBar();
        
        if (showDebugMessages)
        {
            Debug.Log($"Player took {amount} damage, current health: {currentHealth}");
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
        
        // Always log death info for debugging
        Debug.Log("Player died! Current health: " + currentHealth);
        
        // Activate slow motion - force it to be very slow for dramatic effect
        Time.timeScale = deathSlowMotionScale;
        Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale; // Adjust fixed timestep to match
        Debug.Log($"Activated slow motion: Time.timeScale = {Time.timeScale}, fixedDeltaTime = {Time.fixedDeltaTime}");
        
        // Activate screen dimming effect
        if (screenDimOverlay != null)
        {
            screenDimOverlay.SetActive(true);
            
            // Fade in the dim effect
            Image dimImage = screenDimOverlay.GetComponentInChildren<Image>();
            if (dimImage != null)
            {
                dimImage.color = screenDimColor;
                Debug.Log("Screen dimming effect activated");
            }
        }
        
        // Disable player controls if set
        if (disableControlsOnDeath && diverMovement != null)
        {
            diverMovement.enabled = false;
            Debug.Log("Disabled DiverMovement controls");
        }
        else if (diverMovement == null)
        {
            Debug.LogError("Could not find DiverMovement component on player!");
        }
        
        // Stop any physics and make the player completely static
        if (rb != null)
        {
            // Zero the velocity first
            rb.velocity = Vector2.zero;
            
            // Set kinematic to prevent any further physics interactions
            rb.isKinematic = true;
            
            Debug.Log("Set player Rigidbody2D to kinematic and zeroed velocity");
        }
        
        // Trigger feeding frenzy for nearby fish
        TriggerFeedingFrenzy();
        
        // Show death UI with more aggressive approach
        if (deathUIPanel != null)
        {
            // First check if it exists in the scene
            Debug.Log($"Death UI Panel found: {deathUIPanel.name}, Currently active: {deathUIPanel.activeSelf}");
            
            // Enable all parent canvases first (work up the hierarchy)
            Transform current = deathUIPanel.transform;
            while (current != null)
            {
                if (current.gameObject.activeSelf == false)
                {
                    current.gameObject.SetActive(true);
                    Debug.Log($"Activated parent object: {current.gameObject.name}");
                }
                
                // Check for Canvas component and make sure it's enabled
                Canvas canvas = current.GetComponent<Canvas>();
                if (canvas != null && !canvas.enabled)
                {
                    canvas.enabled = true;
                    Debug.Log($"Enabled Canvas component on {current.gameObject.name}");
                }
                
                // Move up to parent
                current = current.parent;
            }
            
            // Now activate the panel itself
            deathUIPanel.SetActive(true);
            Debug.Log("Showing death UI panel: " + deathUIPanel.name);
            
            // Force activation of all children
            foreach (Transform child in deathUIPanel.transform)
            {
                child.gameObject.SetActive(true);
                Debug.Log($"Activated child: {child.gameObject.name}");
            }
        }
        else
        {
            Debug.LogError("Death UI Panel not assigned to PlayerHealth component! Assign it in the Inspector.", this);
        }
        
        // Disable arm/aim scripts on death
        if (diverShooter != null) diverShooter.enabled = false;
        if (armAim != null) armAim.enabled = false;

        // --- SINKING EFFECT ---
        // Immediately allow the player to sink
        if (rb != null)
        {
            rb.isKinematic = false; // Allow gravity
            rb.bodyType = RigidbodyType2D.Dynamic; // Ensure dynamic
            rb.gravityScale = 1f; // Ensure gravity is enabled
            rb.velocity = new Vector2(0, -deathSinkSpeed); // Sink straight down
            Debug.Log($"[DeathSink] isKinematic: {rb.isKinematic}, bodyType: {rb.bodyType}, gravityScale: {rb.gravityScale}, velocity: {rb.velocity}");
        }
    }
    
    /// <summary>
    /// Respawn the player (called by the UI button)
    /// </summary>
    public void Respawn()
    {
        if (!isDead) return;
        
        // Reset time scale to normal using the stored original value
        Time.timeScale = 1.0f;
        Time.fixedDeltaTime = originalFixedDeltaTime; // Reset fixed timestep using stored original
        Debug.Log($"Reset time to normal speed: timeScale = {Time.timeScale}, fixedDeltaTime = {Time.fixedDeltaTime}");
        
        // Remove screen dimming effect
        if (screenDimOverlay != null)
        {
            // Fade out the dim effect
            Image dimImage = screenDimOverlay.GetComponentInChildren<Image>();
            if (dimImage != null)
            {
                dimImage.color = Color.clear;
            }
            
            // Disable the overlay
            screenDimOverlay.SetActive(false);
            Debug.Log("Screen dimming effect removed");
        }
        
        // Reset all fish in the scene
        ResetAllFish();
        
        // Reset health
        currentHealth = maxHealth;
        
        // Stop any running coroutines first
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }
        
        if (invulnerabilityCoroutine != null)
        {
            StopCoroutine(invulnerabilityCoroutine);
            invulnerabilityCoroutine = null;
        }
        
        // Reset sprite color to normal
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white;
        }
        
        // Update the health bar
        UpdateHealthBar();
        
        // Move player to spawn position
        transform.position = respawnPosition;
        
        // Reset physics and reactivate Rigidbody2D
        if (rb != null)
        {
            // Re-enable physics
            rb.isKinematic = false;
            
            // Reset velocity
            rb.velocity = Vector2.zero;
            
            Debug.Log("Reset Rigidbody2D to non-kinematic for respawn");
        }
        
        // Hide death UI
        if (deathUIPanel != null)
        {
            deathUIPanel.SetActive(false);
        }
        
        // Re-enable player controls
        if (disableControlsOnDeath && diverMovement != null)
        {
            diverMovement.enabled = true;
        }
        
        // Make player briefly invulnerable
        StartInvulnerability();
        
        // Reset death state
        isDead = false;
        
        // Re-enable arm/aim scripts on respawn
        if (diverShooter != null) diverShooter.enabled = true;
        if (armAim != null) armAim.enabled = true;
        
        if (showDebugMessages)
        {
            Debug.Log("Player respawned at " + respawnPosition);
        }
        
        // Refill oxygen to max
        if (playerOxygen != null)
            playerOxygen.RefillOxygen(playerOxygen.GetMaxOxygen());
    }
    
    /// <summary>
    /// Resets all fish in the scene when player respawns
    /// </summary>
    private void ResetAllFish()
    {
        // Find all bad fish in the scene
        BadFishAI[] allFish = FindObjectsOfType<BadFishAI>();
        int resetCount = 0;
        
        foreach (BadFishAI fish in allFish)
        {
            // Reset each fish to its starting state
            StartCoroutine(RespawnFish(fish));
            resetCount++;
        }
        
        Debug.Log($"Resetting {resetCount} fish in the scene");
    }
    
    /// <summary>
    /// Coroutine to respawn a fish with a slight delay to prevent all fish from respawning at once
    /// </summary>
    private IEnumerator RespawnFish(BadFishAI fish)
    {
        // Only do this if the fish is valid
        if (fish != null)
        {
            // Get the badFishHealth component
            badFishHealth healthComponent = fish.GetComponent<badFishHealth>();
            
            // Respawn with a small random delay
            yield return new WaitForSeconds(Random.Range(0.1f, 0.5f));
            
            // Stop all coroutines on the fish
            fish.StopAllCoroutines();
            
            // Call a method to reset the fish
            fish.SendMessage("ResetToInitialState", SendMessageOptions.DontRequireReceiver);
            
            // Reset the fish's health if available
            if (healthComponent != null)
            {
                healthComponent.SendMessage("ResetHealth", SendMessageOptions.DontRequireReceiver);
            }
        }
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
        ApplyKnockback(randomDirection);
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
} 