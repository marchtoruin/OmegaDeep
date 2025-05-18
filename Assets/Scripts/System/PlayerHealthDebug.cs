using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealthDebug : MonoBehaviour
{
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerHealthBar healthBar;
    [SerializeField] private KeyCode testDamageKey = KeyCode.T;
    [SerializeField] private int testDamageAmount = 10;
    
    // Debug properties that show current state
    [Header("Debug Info (Read Only)")]
    [SerializeField] private int currentHealth;
    [SerializeField] private float healthPercent;
    [SerializeField] private bool isConnected = false;
    
    private void Start()
    {
        // Auto-find components if not assigned
        if (playerHealth == null)
        {
            playerHealth = GetComponent<PlayerHealth>();
            if (playerHealth == null)
            {
                playerHealth = FindObjectOfType<PlayerHealth>();
            }
        }
        
        if (healthBar == null)
        {
            healthBar = FindObjectOfType<PlayerHealthBar>();
        }
        
        // Check if connections are made
        isConnected = (playerHealth != null && healthBar != null);
        
        if (isConnected)
        {
            Debug.Log("PlayerHealthDebug: Connected to PlayerHealth and PlayerHealthBar");
            // Let's manually update the health bar to make sure it works
            StartCoroutine(DelayedHealthUpdate());
        }
        else
        {
            if (playerHealth == null)
                Debug.LogError("PlayerHealthDebug: Could not find PlayerHealth component");
            if (healthBar == null)
                Debug.LogError("PlayerHealthDebug: Could not find PlayerHealthBar component");
        }
    }
    
    private IEnumerator DelayedHealthUpdate()
    {
        // Wait a frame to ensure everything is initialized
        yield return null;
        
        if (playerHealth != null && healthBar != null)
        {
            // Force update health bar
            currentHealth = playerHealth.GetCurrentHealth();
            int maxHealth = 100; // This should match the PlayerHealth.maxHealth value
            healthPercent = (float)currentHealth / maxHealth;
            healthBar.UpdateHealth(healthPercent);
            Debug.Log($"PlayerHealthDebug: Initial health bar update to {healthPercent * 100}%");
        }
    }
    
    private void Update()
    {
        // Update debug display
        if (playerHealth != null)
        {
            currentHealth = playerHealth.GetCurrentHealth();
            int maxHealth = 100; // This should match the PlayerHealth.maxHealth value
            healthPercent = (float)currentHealth / maxHealth;
        }
        
        // Test damage on key press
        if (Input.GetKeyDown(testDamageKey) && playerHealth != null)
        {
            Debug.Log($"PlayerHealthDebug: Applying test damage of {testDamageAmount}");
            playerHealth.TakeDamage(testDamageAmount);
            
            // After damage is applied, ensure health bar updates
            if (healthBar != null)
            {
                // Force update again directly
                healthBar.UpdateHealth(healthPercent);
                Debug.Log($"PlayerHealthDebug: Directly updated health bar to {healthPercent * 100}%");
            }
        }
    }
    
    // Manual method for Unity button or public API
    public void ForceUpdateHealthBar()
    {
        if (healthBar != null && playerHealth != null)
        {
            currentHealth = playerHealth.GetCurrentHealth();
            int maxHealth = 100; // This should match the PlayerHealth.maxHealth value
            healthPercent = (float)currentHealth / maxHealth;
            healthBar.UpdateHealth(healthPercent);
            Debug.Log($"PlayerHealthDebug: Manually forced health bar update to {healthPercent * 100}%");
        }
    }
    
    // Manual method to apply damage - can be called from Inspector
    public void ApplyTestDamage()
    {
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(testDamageAmount);
            Debug.Log($"PlayerHealthDebug: Applied test damage of {testDamageAmount}");
        }
    }
} 