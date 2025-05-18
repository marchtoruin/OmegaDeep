using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SetupHealthConnections : MonoBehaviour
{
    [Header("Manual Connections")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerHealthBar healthBar;
    [SerializeField] private Image healthBarFillImage;
    
    void Start()
    {
        Debug.Log("Setting up health connections...");
        
        // Find components if not assigned
        if (playerHealth == null)
            playerHealth = FindObjectOfType<PlayerHealth>();
            
        if (healthBar == null)
            healthBar = FindObjectOfType<PlayerHealthBar>();
            
        if (healthBarFillImage == null)
        {
            // Try to find it in the hierarchy
            Transform playerUI = GameObject.Find("PlayerUI")?.transform;
            if (playerUI != null)
            {
                Transform healthBarTransform = playerUI.Find("HealthBar");
                if (healthBarTransform != null)
                {
                    Transform fillTransform = healthBarTransform.Find("Fill");
                    if (fillTransform != null)
                    {
                        healthBarFillImage = fillTransform.GetComponent<Image>();
                    }
                }
            }
        }
        
        // Log what we found
        Debug.Log($"Found PlayerHealth: {playerHealth != null}");
        Debug.Log($"Found PlayerHealthBar: {healthBar != null}");
        Debug.Log($"Found Fill Image: {healthBarFillImage != null}");
        
        // Verify the setup
        if (healthBar != null)
        {
            // See if the health bar already has the fill image assigned
            if (healthBar.GetComponent<Image>() == null && healthBarFillImage != null)
            {
                Debug.Log("The PlayerHealthBar doesn't have an image component. Adding components to the correct objects.");
                
                // Make sure PlayerHealthBar script is on the parent HealthBar object
                Transform playerUI = GameObject.Find("PlayerUI")?.transform;
                if (playerUI != null)
                {
                    Transform healthBarTransform = playerUI.Find("HealthBar");
                    if (healthBarTransform != null)
                    {
                        // If health bar script is on wrong object, move it
                        if (healthBar.gameObject != healthBarTransform.gameObject)
                        {
                            Debug.LogWarning("PlayerHealthBar script is on wrong object. Destroying and adding to correct object.");
                            Destroy(healthBar);
                            healthBar = healthBarTransform.gameObject.AddComponent<PlayerHealthBar>();
                            Debug.Log("Added PlayerHealthBar to correct object: " + healthBarTransform.name);
                        }
                    }
                }
            }
        }
        
        // Force a health update to test connections
        if (playerHealth != null)
        {
            playerHealth.TestDamage(0);
            Debug.Log("Tested health connections with zero damage");
        }
    }
} 