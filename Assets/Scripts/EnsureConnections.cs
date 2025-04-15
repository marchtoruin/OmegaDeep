using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnsureConnections : MonoBehaviour
{
    [SerializeField] private bool showDebugMessages = false; // Default to false to reduce console spam
    [SerializeField] private bool runTestDamage = false; // New field to control test damage
    
    void Start()
    {
        if (showDebugMessages)
        {
            Debug.Log("EnsureConnections: Setting up connections between health components");
        }
        
        // Find player health
        PlayerHealth playerHealth = FindObjectOfType<PlayerHealth>();
        if (playerHealth == null)
        {
            Debug.LogError("Could not find PlayerHealth component");
            return;
        }
        
        // Find health bar
        PlayerHealthBar healthBar = null;
        
        // Look for health bar under PlayerUI
        Transform playerUI = GameObject.Find("PlayerUI")?.transform;
        if (playerUI != null)
        {
            Transform healthBarTransform = playerUI.Find("HealthBar");
            if (healthBarTransform != null)
            {
                healthBar = healthBarTransform.GetComponent<PlayerHealthBar>();
            }
        }
        
        // If still not found, try to find anywhere in the scene
        if (healthBar == null)
        {
            healthBar = FindObjectOfType<PlayerHealthBar>();
        }
        
        if (healthBar == null)
        {
            Debug.LogError("Could not find PlayerHealthBar component");
            return;
        }
        
        // Forcibly connect them
        if (showDebugMessages)
        {
            Debug.Log("Connecting PlayerHealth to PlayerHealthBar");
        }
        
        // Set the reference
        playerHealth.healthBar = healthBar;
        
        // Try to update the health bar to current health value (with full health)
        healthBar.UpdateHealth(1.0f); // ALWAYS initialize to full health
        
        if (showDebugMessages)
        {
            Debug.Log("Connections established successfully");
        }
        
        // Only run test damage if specifically enabled in inspector
        if (runTestDamage)
        {
            if (showDebugMessages)
            {
                Debug.Log("Running test with 10 damage...");
            }
            playerHealth.TakeDamage(10);
        }
    }
} 