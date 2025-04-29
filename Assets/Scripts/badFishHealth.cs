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
    
    [Header("Boss Settings")]
    [SerializeField] private bool isBoss = false; // Is this fish a boss?
    [SerializeField] private int bossHealthMultiplier = 3; // Boss has 3x health
    [SerializeField] private Color bossHealthBarColor = new Color(1.0f, 0.2f, 0.2f); // Bright red for boss health
    [SerializeField] private float bossHealthBarScale = 1.5f; // Larger health bar for boss
    [SerializeField] private float bossMaxDamagePercent = 0.2f; // Maximum damage per hit as percentage of max health (0.2 = 20%)
    [SerializeField] private int bossDamageMultiplier = 2; // How much more damage boss fish do to the player
    
    // Public getter for the boss health bar color
    public Color BossHealthBarColor => bossHealthBarColor;
    
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
    
    // Private variables
    private bool isHealthBarVisible = false;
    private float healthBarTimer = 0f;
    private RectTransform healthBarRectTransform;
    private Vector2 originalSizeDelta; // Store original size
    private BadFishAI aiComponent; // Reference to AI component
    private SpriteRenderer spriteRenderer; // Cache for blinking and flipping
    private Rigidbody2D rb; // Cache for sinking
    private bool isDying = false; // Prevent multiple death sequences
    
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
        
        // Apply boss settings if this is a boss
        if (isBoss)
        {
            SetupBossAttributes();
        }
        
        // Initialize with full health
        currentHealth = maxHealth;
        
        // Get AI component if available
        aiComponent = GetComponent<BadFishAI>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        
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
        if (isBoss || showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Taking {amount} damage, current health: {currentHealth}/{maxHealth}, isBoss: {isBoss}");
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
        if (isBoss || showDebugMessages)
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
            if (isBoss)
            {
                Debug.LogWarning($"BOSS FISH DYING: {gameObject.name} reached 0 health after taking {amount} damage (Max Health was {maxHealth})");
            }
            Die();
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
    private void Die()
    {
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Died", this);
        }
        if (!isDying) // Prevent multiple triggers
        {
            isDying = true;
            StartCoroutine(DeathSequence());
        }
    }
    
    private IEnumerator DeathSequence()
    {
        // Disable AI and movement
        if (aiComponent != null) aiComponent.enabled = false;
        var enemyMove = GetComponent<EnemyMovement>();
        if (enemyMove != null) enemyMove.enabled = false;
        // Stop all movement and physics
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.isKinematic = true;
        }
        // Disable all colliders (including children)
        foreach (var col in GetComponentsInChildren<Collider2D>()) col.enabled = false;
        // Flip upside down (rotate 180° Z)
        transform.rotation = Quaternion.Euler(0, 0, 180);
        // Blink red
        if (spriteRenderer != null)
        {
            Color origColor = spriteRenderer.color;
            for (int i = 0; i < 6; i++)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.1f);
                spriteRenderer.color = origColor;
                yield return new WaitForSeconds(0.1f);
            }
        }
        // Sink straight down and fade out
        float timer = 0f;
        float duration = 2f;
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + new Vector3(0, -2f, 0); // Sink 2 units straight down
        float startAlpha = 1f;
        float endAlpha = 0f;
        if (spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            while (timer < duration)
            {
                float t = timer / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                c.a = Mathf.Lerp(startAlpha, endAlpha, t);
                spriteRenderer.color = c;
                timer += Time.deltaTime;
                yield return null;
            }
            // Ensure final state
            transform.position = endPos;
            c.a = endAlpha;
            spriteRenderer.color = c;
        }
        else
        {
            // No sprite renderer, just move
            while (timer < duration)
            {
                float t = timer / duration;
                transform.position = Vector3.Lerp(startPos, endPos, t);
                timer += Time.deltaTime;
                yield return null;
            }
            transform.position = endPos;
        }
        Destroy(gameObject);
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
    /// Sets up boss attributes for health and health bar
    /// </summary>
    private void SetupBossAttributes()
    {
        // Validation for multiplier - ensure it's a reasonable value
        if (bossHealthMultiplier <= 1)
        {
            Debug.LogWarning($"Boss health multiplier is set to {bossHealthMultiplier}, which is too low. Setting to default value of 3.", this);
            bossHealthMultiplier = 3;
        }
        
        // Store original max health first if not already stored
        if (originalMaxHealth <= 0)
        {
            originalMaxHealth = maxHealth;
        }
        
        // Multiply max health for boss
        maxHealth = originalMaxHealth * bossHealthMultiplier;
        
        // Ensure boss has at least 9 health
        if (maxHealth < 9)
        {
            Debug.LogWarning($"Boss health calculated to only {maxHealth}, which is too low. Setting to minimum value of 9.", this);
            maxHealth = 9;
        }
        
        // IMPORTANT: Set current health to the new max health to fix boss dying too quickly
        currentHealth = maxHealth;
        
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Boss mode activated! Max Health increased from {originalMaxHealth} to {maxHealth}, current health set to {currentHealth}");
        }
        
        // Make health bar always visible for boss
        alwaysShowHealthBar = true;
        
        // Use boss color for health bar
        if (healthBarFill != null)
        {
            healthBarFill.color = bossHealthBarColor;
        }
        
        // Scale up health bar if possible
        if (healthBarObject != null)
        {
            // Scale the health bar to be more visible
            Transform healthBarTransform = healthBarObject.transform;
            healthBarTransform.localScale = new Vector3(
                healthBarTransform.localScale.x * bossHealthBarScale,
                healthBarTransform.localScale.y * bossHealthBarScale,
                healthBarTransform.localScale.z
            );
        }
        
        // Update the health bar to show full health
        UpdateHealthBar();
        
        if (showDebugMessages)
        {
            Debug.Log($"{gameObject.name}: Boss mode activated! Health: {maxHealth}, Bar scale: {bossHealthBarScale}");
        }
    }

    /// <summary>
    /// Public method to set boss status from other scripts
    /// </summary>
    public void SetBossStatus(bool status)
    {
        // If no change in status, don't do anything
        if (status == isBoss) return;
        
        isBoss = status;
        
        // If we're changing to a boss, set up boss attributes
        if (isBoss)
        {
            SetupBossAttributes();
            // Note: currentHealth is already set in SetupBossAttributes
            UpdateHealthBar();
            
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Changed to boss, Health set to {currentHealth}/{maxHealth}");
            }
        }
        else
        {
            // Reverting from boss to regular fish
            maxHealth = originalMaxHealth;
            currentHealth = Mathf.Min(currentHealth, maxHealth); // Cap health at new max
            UpdateHealthBar();
            
            if (showDebugMessages)
            {
                Debug.Log($"{gameObject.name}: Reverted from boss, Health adjusted to {currentHealth}/{maxHealth}");
            }
        }
    }

    /// <summary>
    /// Check if this fish is a boss
    /// </summary>
    public bool IsBoss()
    {
        return isBoss;
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
        // Ensure boss health multiplier is reasonable
        if (bossHealthMultiplier < 2)
        {
            Debug.LogWarning($"Boss health multiplier on {gameObject.name} is set to {bossHealthMultiplier}. This seems low for a boss. Consider setting it to at least 3.", this);
            // Don't auto-change as this might interrupt designer work
        }
        
        // Remove the damage cap validation since we're not using it anymore
        
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
            sr.color = isBoss ? bossHealthBarColor : Color.white;
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

        if(showDebugMessages || isBoss) // Log for bosses or if debug is on
        {
            Debug.Log($"[{gameObject.name}] Health Multiplier Applied: {multiplier}x. Base Health: {originalMaxHealth}, New Max Health: {maxHealth}", this);
        }


        // Optional: Update health bar if it exists and needs manual update
        // Make sure UpdateHealthBar can handle the new maxHealth value correctly
        UpdateHealthBar();
    }
}
