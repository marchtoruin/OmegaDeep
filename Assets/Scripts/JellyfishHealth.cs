using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Required for UI elements

public class JellyfishHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 30;
    private int currentHealth;

    [Header("Death Settings")]
    [Tooltip("Prefab of the Jelly Charge to drop on death")]
    public GameObject jellyChargePrefab;
    [Tooltip("Optional particle effect to play on death")]
    public GameObject deathEffectPrefab;

    [Header("Health Bar UI")]
    [SerializeField] private Image healthBarFill; // Reference to the fill image of health bar
    [SerializeField] private GameObject healthBarObject; // Reference to the health bar parent
    [SerializeField] private float healthBarVisibilityDuration = 3f; // How long health bar stays visible after damage
    [SerializeField] private bool alwaysShowHealthBar = false; // Should health bar always be visible?

    // Mirror badFishHealth: cache rect for resizing
    private RectTransform healthBarRectTransform;
    private Vector2 originalSizeDelta;

    // Private health bar variables
    private bool isHealthBarVisible = false;
    private float healthBarTimer = 0f;

    // Reference to AI for potential disabling
    private JellyfishAI aiScript;

    void Awake()
    {
        currentHealth = maxHealth;
        aiScript = GetComponent<JellyfishAI>();

        // Ensure health bar visibility matches alwaysShowHealthBar
        if (healthBarObject != null)
        {
            healthBarObject.SetActive(alwaysShowHealthBar);
        }
        else
        {
            Debug.LogError($"{gameObject.name}: healthBarObject reference is missing!", this);
        }
        if (healthBarFill == null)
        {
            Debug.LogError($"{gameObject.name}: healthBarFill reference is missing!", this);
        }
    }

    void Start()
    {
        // Mirror badFishHealth setup: cache rect and pivot/anchor
        if (healthBarFill != null)
        {
            healthBarRectTransform = healthBarFill.rectTransform;
            if (healthBarRectTransform != null)
            {
                originalSizeDelta = healthBarRectTransform.sizeDelta;
                healthBarRectTransform.pivot = new Vector2(0, 0.5f);
                healthBarRectTransform.anchorMin = new Vector2(0, 0.5f);
                healthBarRectTransform.anchorMax = new Vector2(0, 0.5f);
                healthBarRectTransform.anchoredPosition = Vector2.zero;
            }
        }

        // Setup health bar initial state
        UpdateHealthBar();

        // Hide health bar initially if not always shown
        if (!alwaysShowHealthBar && healthBarObject != null)
        {
            healthBarObject.SetActive(false);
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

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return; // Already dead

        currentHealth -= amount;
        Debug.Log($"{gameObject.name} took {amount} damage, health: {currentHealth}/{maxHealth}", this);

        // Update and show health bar
        UpdateHealthBar();
        ShowHealthBar();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    /// <summary>
    /// Updates the health bar width by resizing the fill rect
    /// </summary>
    private void UpdateHealthBar()
    {
        if (healthBarRectTransform != null)
        {
            float percent = (float)currentHealth / maxHealth;
            percent = Mathf.Clamp01(percent);
            healthBarRectTransform.sizeDelta = new Vector2(originalSizeDelta.x * percent, originalSizeDelta.y);
        }
    }

    /// <summary>
    /// Shows the health bar and resets visibility timer
    /// </summary>
    private void ShowHealthBar()
    {
        if (healthBarObject != null)
        {
            Debug.Log($"{gameObject.name}: Showing health bar.", this);
            healthBarObject.SetActive(true);
            isHealthBarVisible = true;
            healthBarTimer = healthBarVisibilityDuration;
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name} died.", this);

        // Disable AI and Collider
        if (aiScript != null) aiScript.enabled = false;
        Collider2D col = GetComponent<Collider2D>(); if (col != null) col.enabled = false;
        Rigidbody2D rb = GetComponent<Rigidbody2D>(); if (rb != null) rb.velocity = Vector2.zero;

        // Spawn Jelly Charge
        if (jellyChargePrefab != null)
        {
            Instantiate(jellyChargePrefab, transform.position, Quaternion.identity);
            Debug.Log("Spawned Jelly Charge.", this);
        }
        else
        {
            Debug.LogWarning("Jelly Charge Prefab not assigned to JellyfishHealth!", this);
        }

        // Spawn Death Effect (optional)
        if (deathEffectPrefab != null)
        {
            Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }

    // Example placeholder for hurt feedback
    /*
    IEnumerator FlashColor()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            Color originalColor = sr.color;
            sr.color = Color.red; 
            yield return new WaitForSeconds(0.1f);
            sr.color = originalColor;
        }
    }
    */
} 