using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnsureConnections : MonoBehaviour
{
    [SerializeField] private bool showDebugMessages = false; // Default to false to reduce console spam
    [SerializeField] private bool runTestDamage = false; // New field to control test damage

    private PlayerHealthBar healthBar; // Cache health bar reference
    private bool playerConnected = false; // Flag to prevent repeated connections
    
    void Start()
    {
        if (showDebugMessages)
        {
            Debug.Log("EnsureConnections: Searching for components...", this);
        }
        
        // --- Find Health Bar (Likely exists at start) ---
        healthBar = null;
        Transform playerUI = GameObject.Find("PlayerUI")?.transform;
        if (playerUI != null)
        {
            Transform healthBarTransform = playerUI.Find("HealthBar");
            if (healthBarTransform != null)
            {
                healthBar = healthBarTransform.GetComponent<PlayerHealthBar>();
            }
        }
        if (healthBar == null)
        {
            healthBar = FindObjectOfType<PlayerHealthBar>();
        }
        if (healthBar == null)
        {   
            // Log error but continue, maybe player health exists standalone
            Debug.LogError("Could not find PlayerHealthBar component", this);
        }
        // --- End Find Health Bar ---

        // --- Attempt Initial Player Health Connection ---
        TryConnectPlayer();
    }

    void TryConnectPlayer()
    {
        // If already connected, do nothing
        if (playerConnected) return;

        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        
        if (playerHealth != null)
        {
            // Found the player health component
            playerConnected = true; // Mark as connected
            
            if (showDebugMessages)
            {
                Debug.Log($"Found PlayerHealth on {playerHealth.gameObject.name}. Attempting to connect...", this);
            }

            // Only connect if health bar was also found
            if (healthBar != null)
            {
                if (showDebugMessages) Debug.Log("Connecting PlayerHealth to PlayerHealthBar", this);
                playerHealth.healthBar = healthBar;
                healthBar.UpdateHealth(1.0f); // Initialize to full health
            }
            else
            {
                if (showDebugMessages) Debug.Log("PlayerHealth found, but no HealthBar to connect to.", this);
            }
            
            if (showDebugMessages) Debug.Log("Player connection established successfully", this);
            
            // Run test damage if enabled
            if (runTestDamage)
            {
                if (showDebugMessages) Debug.Log("Running test with 10 damage...", this);
                playerHealth.TakeDamage(10);
            }
        }
        else
        {
            // PlayerHealth not found yet, start the coroutine to keep trying
            if (showDebugMessages) Debug.Log("PlayerHealth not found yet. Starting search coroutine...", this);
            StartCoroutine(FindPlayerHealthCoroutine());
        }
    }

    // Coroutine to repeatedly search for PlayerHealth
    private IEnumerator FindPlayerHealthCoroutine()
    {
        while (!playerConnected)
        {
            // Wait a moment before retrying
            yield return new WaitForSeconds(0.5f); 

            if (showDebugMessages) Debug.Log("Retrying PlayerHealth search...", this);
            TryConnectPlayer(); // Attempt connection again
            
            // If TryConnectPlayer succeeds, playerConnected will become true, ending the loop
        }
    }
} 