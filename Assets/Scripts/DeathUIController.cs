using UnityEngine;
using UnityEngine.UI;
using TMPro; // Add TextMeshPro namespace

public class DeathUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Button respawnButton; // Assign this in the inspector
    [SerializeField] private TextMeshProUGUI deathMessageText; // Changed to TextMeshProUGUI
    
    [Header("Settings")]
    [SerializeField] private string deathMessage = "You died!"; // Message to display
    [SerializeField] private string respawnButtonText = "Respawn"; // Text for the respawn button
    
    private PlayerHealth playerHealth; // Reference to player health component
    
    private void Awake()
    {
        // Find player and get health component
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerHealth = player.GetComponent<PlayerHealth>();
        }
        
        // Set up UI elements
        if (deathMessageText != null)
        {
            deathMessageText.text = deathMessage;
        }
        
        // Set up button
        if (respawnButton != null)
        {
            // Get button text component - updated for TextMeshPro
            TextMeshProUGUI buttonText = respawnButton.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = respawnButtonText;
            }
            
            // Add click listener
            respawnButton.onClick.AddListener(OnRespawnButtonClicked);
        }
        else
        {
            Debug.LogError("Respawn button not assigned to DeathUIController", this);
        }
        
        // Make sure panel is initially hidden
        gameObject.SetActive(false);
    }
    
    private void OnRespawnButtonClicked()
    {
        if (playerHealth != null)
        {
            // Call respawn on player
            playerHealth.Respawn();
        }
        else
        {
            Debug.LogError("Cannot respawn: Player Health component not found", this);
        }
    }
} 