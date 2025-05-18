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
    private bool foundPlayer = false; // Flag for robust finding
    
    // Using OnEnable instead of Awake to handle the panel being activated later
    private void OnEnable() 
    { 
        // Attempt to find player immediately when panel becomes active
        TryFindPlayerAndSetup();
    }

    // Helper method to find player and set up UI
    private void TryFindPlayerAndSetup()
    {
        // Don't run setup again if player already found
        if (foundPlayer) return; 

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {   
            // Check for the required component
            playerHealth = player.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                foundPlayer = true;
                Debug.Log("[DeathUIController] Found valid PlayerHealth.");
                // Setup UI elements now that we have the player reference (or know it exists)
                SetupUIElements(); 
            }
            else
            {
                 Debug.LogError("[DeathUIController] Found Player object, but it lacks PlayerHealth component!", player);
                 // Optionally disable respawn button if player health is essential?
                 // if (respawnButton != null) respawnButton.interactable = false;
            }
        }
        else
        {   
            // Log error but don't disable, OnEnable might run again or player might spawn later?
            Debug.LogError("[DeathUIController] Could not find GameObject tagged 'Player'! Respawn might fail.");
             // Maybe try again later? Or assume PlayerHealth handles respawn differently.
        }
    }

    // Separated UI setup from finding the player
    private void SetupUIElements()
    {
         // Set up UI elements
        if (deathMessageText != null)
        {
            deathMessageText.text = deathMessage;
        }
        
        // Set up button
        if (respawnButton != null)
        {
            // Clear previous listeners to avoid duplicates if OnEnable runs multiple times
            respawnButton.onClick.RemoveAllListeners(); 
            
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
    }
    
    // Awake is called only once when the object is first loaded
    // Use it just to ensure the panel starts hidden.
    private void Awake()
    {        
        // Make sure panel is initially hidden
        // Note: This might hide it again if respawn reloads scene vs just moving player
        gameObject.SetActive(false); 
    }
    
    private void OnRespawnButtonClicked()
    {
        // Ensure we still have the player reference
        if (!foundPlayer || playerHealth == null)
        {
             // Attempt to find player again just in case
             TryFindPlayerAndSetup();
             if (!foundPlayer || playerHealth == null)
             {
                  Debug.LogError("Cannot respawn: Player Health component reference lost or never found!", this);
                  return;
             }
        }

        // Call respawn on player
        playerHealth.Respawn();
        
        // Optionally hide the death panel immediately after clicking respawn
        // gameObject.SetActive(false); // PlayerHealth.Respawn should probably handle this
    }
} 