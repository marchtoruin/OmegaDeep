using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements
using FMODUnity;

public class badFishHealth : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private bool showDebugMessages = false; // Enable to show detailed health status logs in console
    
    [Header("Boss Health Bar Settings")] // Renamed Header for clarity
    [SerializeField] private Color bossHealthBarColor = new Color(1.0f, 0.2f, 0.2f); // Bright red for boss health
    [SerializeField] private float bossHealthBarScale = 1.5f; // Larger health bar for boss
    
    // Kept boss damage settings here as they relate to health/damage calculation
    [Header("Boss Damage Settings")]
    [SerializeField] private float bossMaxDamagePercent = 0.2f; // Maximum damage per hit as percentage of max health (0.2 = 20%)
    [SerializeField] private int bossDamageMultiplier = 2; // How much more damage boss fish do to the player
    
    // Public getter for the boss health bar color
    // public Color BossHealthBarColor => bossHealthBarColor;
    
    // Current health tracking
    private int currentHealth;
    private int originalMaxHealth; // Store original max health for reference

    [Header("Health Bar UI")]
    [SerializeField] private Image healthBarFill; // Reference to the fill image of health bar
    [SerializeField] private GameObject healthBarObject; // Reference to the health bar parent
    [SerializeField] private float healthBarVisibilityDuration = 3f; // How long health bar stays visible after damage
    [SerializeField] private bool alwaysShowHealthBar = false; // Should health bar always be visible?
    
    [Header("Effects")]
    // Optional effect to play when damaged
    [SerializeField] private GameObject damageEffectPrefab;
    
    // FMOD impact sound
    public FMODPlayOnTrigger impactSound;
    
    [Header("Respawn Settings")]
    [SerializeField] private bool respawnOnDeath = false;
    [SerializeField] private float respawnDelay = 10.0f;
    
    // Private variables
    private bool isHealthBarVisible = false;
    private float healthBarTimer = 0f;
    private RectTransform healthBarRectTransform;
    private Vector2 originalSizeDelta; // Store original size
    private BadFishAI aiComponent; // Reference to AI component
    private SpriteRenderer spriteRenderer; // Cache for blinking and flipping
    private Rigidbody2D rb; // Cache for sinking
    private bool isDead = false; // Renamed from isDying
    private Vector3 initialPosition; // ADDED: Store initial position
    private Quaternion initialRotation; // ADDED: Store initial rotation
    private Color originalSpriteColor; // ADDED: Store original color
    
    // Initialize health on startup
    void Start()
    {
        // Find ImpactSoundTrigger child and get FMODPlayOnTrigger
        Transform impactSoundChild = transform.Find("ImpactSoundTrigger");
        if (impactSoundChild != null)
        {
            impactSound = impactSoundChild.GetComponent<FMODPlayOnTrigger>();
        }
        // Store original max health
        originalMaxHealth = maxHealth;
        
        // Initialize with full health
        currentHealth = maxHealth;
        
        // Get AI component if available
        aiComponent = GetComponent<BadFishAI>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
        // --- ADDED: Store initial transform and color ---
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        if (spriteRenderer != null) 
        {
             originalSpriteColor = spriteRenderer.color;
        }
        // ----------------------------------------------
        
        // Ensure fish has the proper tag for player collision detection
        if (gameObject.tag != "BadFish")
        {
            gameObject.tag = "BadFish";
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Tag set to 'BadFish' for player collision detection");
            }
        }
        
        // Get the RectTransform if it exists
        if (healthBarFill != null)
        {
            healthBarRectTransform = healthBarFill.rectTransform;
            
            // Store original size instead of modifying pivot
            if (healthBarRectTransform != null)
            {
                originalSizeDelta = healthBarRectTransform.sizeDelta;
                
                // Configure for right-to-left depletion (damage comes from right side)
                // Using right-side pivot and anchors
                healthBarRectTransform.pivot = new Vector2(1, 0.5f);
                healthBarRectTransform.anchorMin = new Vector2(1, 0.5f);
                healthBarRectTransform.anchorMax = new Vector2(1, 0.5f);
                
                // Reset position to align with the right side of background
                healthBarRectTransform.anchoredPosition = Vector2.zero;
            }
        }
        
        // Setup health bar initial state
        UpdateHealthBar();
        
        // Hide health bar initially if not set to always show
        if (!alwaysShowHealthBar && healthBarObject != null)
        {
            healthBarObject.SetActive(false);
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Initialized with {currentHealth} health", this);
            
            // Warn if health bar components are missing
            if (healthBarFill == null && healthBarObject != null)
            {
                Debug.LogWarning($"{gameObject.name}: Health bar object assigned but fill image is missing", this);
            }
        }
    }
    
    void Update()
    {
        // Handle health bar visibility timer
        if (isHealthBarVisible && !alwaysShowHealthBar)
        {
            healthBarTimer -= Time.deltaTime;
            
            if (healthBarTimer <= 0f)
            {
                isHealthBarVisible = false;
                if (healthBarObject != null)
                {
                    healthBarObject.SetActive(false);
                }
            }
        }
    }
    
    /// <summary>
    /// Public method to damage the fish
    /// </summary>
    /// <param name="amount">Amount of damage to apply</param>
    public void TakeDamage(int amount)
    {
        // Validate damage amount
        if (amount <= 0) return;
        
        // Play impact sound if available
        if (impactSound != null && impactSound.emitter != null)
        {
            impactSound.emitter.Play();
        }
        
        // Debug - always log damage for bosses regardless of debug setting
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Taking {amount} damage, current health: {currentHealth}/{maxHealth}");
        }
        
        // Remove damage cap for boss fish - they now take full damage
        // Just apply damage directly
        currentHealth -= amount;
        
        // Update the health bar
        UpdateHealthBar();
        
        // Show health bar when damaged
        ShowHealthBar();
        
        // Notify AI component that fish was attacked
        if (aiComponent != null)
        {
            aiComponent.OnAttacked();
            // If using health-based behavior changes
            aiComponent.OnHealthChanged(currentHealth, maxHealth);
        }
        
        // Always log for bosses, otherwise only if debug is enabled
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: After taking {amount} damage, health is now: {currentHealth}/{maxHealth}");
        }
        
        // Spawn optional damage effect
        if (damageEffectPrefab != null)
        {
            Instantiate(damageEffectPrefab, transform.position, Quaternion.identity);
        }
        
        // Check if should die
        if (currentHealth <= 0)
        {
            if (showDebugMessages)
            {
                Debug.Log($"[{gameObject.name}] Died", this);
            }
            if (!isDead) // Prevent multiple triggers
            {
                isDead = true;
                StartCoroutine(DeathAndRespawnSequence()); // Changed to combined coroutine
            }
        }
    }
    
    /// <summary>
    /// Updates the health bar fill amount
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarFill != null && healthBarRectTransform != null)
        {
            // Calculate fill amount (value between 0 and 1)
            float fillAmount = (float)currentHealth / maxHealth;
            fillAmount = Mathf.Clamp01(fillAmount); // Ensure value is between 0-1
            
            // Adjust width through sizeDelta (width setting) 
            // Since we're using right pivot/anchor, this gives right-to-left depletion
            healthBarRectTransform.sizeDelta = new Vector2(originalSizeDelta.x * fillAmount, originalSizeDelta.y);
            
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Health bar updated - {fillAmount * 100}% remaining", this);
            }
        }
    }
    
    /// <summary>
    /// Shows the health bar and resets visibility timer
    /// </summary>
    private void ShowHealthBar()
    {
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(true);
            isHealthBarVisible = true;
            healthBarTimer = healthBarVisibilityDuration;
        }
    }
    
    /// <summary>
    /// Private method to handle death
    /// </summary>
    private IEnumerator DeathAndRespawnSequence()
    {
        // --- Death Part --- 
        // Disable AI
        if (aiComponent != null) aiComponent.enabled = false;

        // Stop all movement and physics
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }

        // Disable all colliders (including children) - cache them first
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var col in colliders) col.enabled = false;

        // Flip upside down (rotate 180° Z)
        transform.rotation = Quaternion.Euler(initialRotation.eulerAngles.x, initialRotation.eulerAngles.y, initialRotation.eulerAngles.z + 180f);

        // Blink red
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color; // Use current color to preserve alpha if needed
            for (int i = 0; i < 6; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = c; // Restore original color + alpha
                yield return new WaitForSeconds(0.1f);
            }
        }

        // Sink straight down and fade out
        float timer = 0f;
        float duration = 2f;
        Vector3 startPos = transform.position; // Start sinking from current (rotated) position
        Vector3 endPos = startPos + transform.up * -1.5f; // Sink based on current up direction (which is down because it's flipped)
        float startAlpha = spriteRenderer != null ? spriteRenderer.color.a : 1f;
        float endAlpha = 0f;

        while (timer < duration)
        {
            float t = timer / duration;
            transform.position = Vector3.Lerp(startPos, endPos, t);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(startAlpha, endAlpha, t);
                spriteRenderer.color = c;
            }
            timer += Time.deltaTime;
            yield return null;
        }

        // --- Post-Death Action --- 
        if (!respawnOnDeath)
        {
            if(showDebugMessages) Debug.Log($"[{gameObject.name}] Not respawning, destroying object.", this);
            Destroy(gameObject);
            yield break; // Exit coroutine
        }
        else
        {
            // Prepare for respawn: Disable renderer and health bar
            if(showDebugMessages) Debug.Log($"[{gameObject.name}] Preparing for respawn in {respawnDelay} seconds.", this);
            if (spriteRenderer != null) spriteRenderer.enabled = false;
            if (healthBarObject != null) healthBarObject.SetActive(false); // Hide health bar

            // Wait for respawn delay
            yield return new WaitForSeconds(respawnDelay);

            // --- Respawn Part --- 
            if(showDebugMessages) Debug.Log($"[{gameObject.name}] Respawning now!", this);

            // Reset Transform
            transform.position = initialPosition;
            transform.rotation = initialRotation;

            // Reset Visuals
            if (spriteRenderer != null) 
            {
                spriteRenderer.enabled = true;
                spriteRenderer.color = originalSpriteColor; // Restore original color and alpha
            }

            // Re-enable colliders
            foreach (var col in colliders) col.enabled = true;

            // Reset Physics
            if (rb != null)
            {
                rb.isKinematic = false;
            }

            // Reset Health
            ResetHealth(); // Restores health and updates health bar (but bar might be inactive)

            // Reset AI
            aiComponent?.ResetToInitialState(); // Use the existing reset method on AI
            if (aiComponent != null) aiComponent.enabled = true;

            // Reset dead flag
            isDead = false;
        }
    }
    
    /// <summary>
    /// Test method for debugging - call this to manually damage the fish
    /// </summary>
    public void TestDamage()
    {
        TakeDamage(1);
    }

    /// <summary>
    /// Resets the fish's health to maximum when player respawns
    /// </summary>
    public void ResetHealth()
    {
        // Reset health to maximum
        currentHealth = maxHealth;
        
        // Update the health bar
        UpdateHealthBar();
        
        // Hide health bar if it shouldn't always be shown
        if (!alwaysShowHealthBar && healthBarObject != null)
        {
            healthBarObject.SetActive(false);
            isHealthBarVisible = false;
            healthBarTimer = 0f;
        }
        
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Health reset to {currentHealth}/{maxHealth}", this);
        }
    }

    /// <summary>
    /// Updates the visual appearance of the health bar based on boss status.
    /// Called by BadFishAI after setting boss status.
    /// </summary>
    public void UpdateHealthBarAppearance(bool isCurrentlyBoss)
    {
        if (showDebugMessages) 
            Debug.Log($"[{gameObject.name}] Updating Health Bar Appearance. isBoss = {isCurrentlyBoss}", this);

        alwaysShowHealthBar = isCurrentlyBoss; // Boss bar is always visible

        if (healthBarFill != null) 
        {
             healthBarFill.color = isCurrentlyBoss ? bossHealthBarColor : Color.white; // Use boss color or default white
        }

        if (healthBarObject != null)
        {
            Transform healthBarTransform = healthBarObject.transform;
            // Reset scale first to avoid multiplicative scaling if called multiple times
            // Assuming parent object starts at local scale 1,1,1
            // TODO: This might need adjustment if the parent object's scale is not 1 initially.
            // Consider storing original local scale in Start() if needed.
            healthBarTransform.localScale = Vector3.one; 
            
            if(isCurrentlyBoss)
            {
                // Scale the health bar to be more visible
                healthBarTransform.localScale = new Vector3(
                    bossHealthBarScale, // Apply uniform scale based on boss setting
                    bossHealthBarScale,
                    1f // Keep Z scale at 1
                );
            }
            // Ensure bar is active/inactive based on alwaysShow setting
            healthBarObject.SetActive(alwaysShowHealthBar || isHealthBarVisible); 
        }
    }

    /// <summary>
    /// Get the boss damage multiplier for enemy collision
    /// </summary>
    public int GetBossDamageMultiplier()
    {
        return bossDamageMultiplier;
    }

    // Validate inspector fields in OnValidate to catch common setup errors
    private void OnValidate()
    {
        // Check if health bar references are missing when alwaysShowHealthBar is true
        if (alwaysShowHealthBar && (healthBarFill == null || healthBarObject == null))
        {
            Debug.LogError($"Health bar on {gameObject.name} is set to always show, but health bar references are missing!", this);
        }
    }

    // Add this method to reset visuals and stop all coroutines
    public void ResetVisuals()
    {
        StopAllCoroutines();
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.white;
        }
    }

    /// <summary>
    /// Applies a multiplier to the fish's health, used for boss variants.
    /// Recalculates max health and sets current health to the new maximum.
    /// Stores the original base health if not already done.
    /// </summary>
    /// <param name="multiplier">The factor to multiply the base max health by.</param>
    public void ApplyHealthMultiplier(float multiplier)
    {
        if (multiplier <= 1f)
        {
            Debug.LogWarning($"[{gameObject.name}] ApplyHealthMultiplier called with multiplier <= 1 ({multiplier}). No change applied.", this);
            return;
        }

        // Ensure originalMaxHealth is stored correctly before modification
        // If originalMaxHealth hasn't been set or matches current maxHealth, store it now.
        // This handles cases where SetupBossAttributes might be called before originalMaxHealth is properly stored.
        if (originalMaxHealth <= 0 || originalMaxHealth == maxHealth)
        {
             originalMaxHealth = maxHealth; // Store the current maxHealth as the base
             if(showDebugMessages) Debug.Log($"[{gameObject.name}] Storing base max health: {originalMaxHealth}", this);
        }


        // Calculate new max health based on the *original* value
        maxHealth = Mathf.CeilToInt(originalMaxHealth * multiplier);
        currentHealth = maxHealth; // Heal the boss to the new full health

        if(showDebugMessages) // Log for bosses or if debug is on
        {
            Debug.Log($"[{gameObject.name}] Health Multiplier Applied: {multiplier}x. Base Health: {originalMaxHealth}, New Max Health: {maxHealth}", this);
        }


        // Optional: Update health bar if it exists and needs manual update
        // Make sure UpdateHealthBar can handle the new maxHealth value correctly
        UpdateHealthBar();
    }
}
