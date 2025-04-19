using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashlightController : MonoBehaviour
{
    public SpriteRenderer playerSprite; // Reference to the player's sprite renderer for flipping
    public ArmAim armAimScript; // Reference to the ArmAim script to check facing direction

    [Header("Flashlight Settings")]
    public KeyCode toggleKey = KeyCode.F; // Key to toggle flashlight on/off
    public bool startOn = true; // Whether the flashlight starts on or off
    
    [Header("Battery Settings")]
    public float maxBattery = 100f;
    public float drainRate = 5f; // Units per second when ON
    public float rechargeRate = 2f; // Units per second when OFF
    
    // Public for UI or other scripts to read
    [HideInInspector] public float currentBattery; 
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false; // Default to false to reduce console spam
    
    private Vector3 originalLocalPosition; // Store the original local position
    private Quaternion originalLocalRotation; // Store the original local rotation
    private bool wasFlipped = false; // Track last flip state
    private bool isOn; // Current state of the flashlight
    private Light flashlightLight; // Reference to the Light component

    // Start is called before the first frame update
    void Start()
    {
        // Get the Light component (could be Light2D for 2D games)
        flashlightLight = GetComponent<Light>();
        if (flashlightLight == null)
        {
            // For 2D lights, try to find UnityEngine.Rendering.Universal.Light2D
            var light2D = GetComponent(typeof(UnityEngine.Rendering.Universal.Light2D));
            if (light2D != null)
            {
                // We found a Light2D component but can't directly reference the type
                // (we'll use reflection to toggle it)
                flashlightLight = light2D as Light;
            }
        }
        
        // Initialize battery
        currentBattery = maxBattery;
        
        // Initialize light state
        isOn = startOn;
        // If starting on but battery is somehow 0, start off
        if (currentBattery <= 0) 
        {
            isOn = false;
        }
        UpdateLightState();
        
        // Store the initial local position and rotation of the flashlight
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        
        // Auto-find references if not set
        if (playerSprite == null)
        {
            // First look for player sprite in the parent hierarchy
            Transform playerTransform = transform.root.Find("Player");
            if (playerTransform != null)
            {
                playerSprite = playerTransform.GetComponent<SpriteRenderer>();
                if (playerSprite == null)
                {
                    // If not found directly on Player, try to find it on a child
                    playerSprite = playerTransform.GetComponentInChildren<SpriteRenderer>();
                }
            }
            
            // If still not found, try a broader search
            if (playerSprite == null)
            {
                Debug.LogWarning("Player sprite not found in hierarchy - attempting broader search");
                playerSprite = FindObjectOfType<SpriteRenderer>();
            }
            
            // Comment out debug log
            //if (playerSprite != null && showDebugInfo)
            //{
            //    Debug.Log($"Auto-found player sprite: {playerSprite.name}");
            //}
        }
        
        // Find ArmAim script if not assigned
        if (armAimScript == null)
        {
            armAimScript = GetComponentInParent<ArmAim>();
            if (armAimScript == null)
            {
                armAimScript = FindObjectOfType<ArmAim>();
            }
            // Comment out debug log
            //if (armAimScript != null && showDebugInfo)
            //{
            //    Debug.Log($"Auto-found ArmAim script on: {armAimScript.name}");
            //}
        }
        
        // Comment out debug logs
        //if (showDebugInfo)
        //{
        //    // Log initial state
        //    Debug.Log($"FlashlightController initialized. Original position: {originalLocalPosition}, " +
        //              $"Original rotation: {originalLocalRotation.eulerAngles}");
        //    if (playerSprite != null)
        //        Debug.Log($"Using playerSprite: {playerSprite.name}, Initial flipX: {playerSprite.flipX}");
        //    if (armAimScript != null)
        //        Debug.Log($"Using armAimScript: {armAimScript.name}, Initial IsFacingRight: {armAimScript.IsFacingRight}");
        //}
    }

    void Update()
    {
        // --- Battery Drain/Recharge --- 
        if (isOn)
        {
            if (currentBattery > 0)
            {
                currentBattery -= drainRate * Time.deltaTime;
                currentBattery = Mathf.Max(currentBattery, 0f); // Clamp to 0
            }
            
            // Auto-turn off if battery runs out
            if (currentBattery <= 0)
            {
                if (showDebugInfo) Debug.Log("Battery depleted, turning flashlight off.");
                isOn = false;
                UpdateLightState();
            }
        }
        else // Flashlight is OFF
        {
            if (currentBattery < maxBattery)
            {
                currentBattery += rechargeRate * Time.deltaTime;
                currentBattery = Mathf.Min(currentBattery, maxBattery); // Clamp to max
            }
        }
        // --- End Battery --- 

        // Check for flashlight toggle input
        if (Input.GetKeyDown(toggleKey))
        {
            // Only allow turning ON if there is battery
            if (!isOn && currentBattery > 0)
            {
                ToggleFlashlight();
            }
            // Always allow turning OFF
            else if (isOn)
            {
                ToggleFlashlight();
            }
        }
    }

    void LateUpdate()
    {
        // Only update position if the flashlight is on
        if (!isOn) return;
        
        bool shouldFlip = false;
        
        // First try to determine flip state from the sprite renderer
        if (playerSprite != null)
        {
            shouldFlip = playerSprite.flipX;
        }
        // Fallback to ArmAim script if sprite reference is missing or not working
        else if (armAimScript != null)
        {
            shouldFlip = !armAimScript.IsFacingRight;
        }
        
        // Only update if the flip state has changed to avoid constant reassignment
        if (shouldFlip != wasFlipped)
        {
            wasFlipped = shouldFlip;
            
            if (shouldFlip)
            {
                // Flip the flashlight position and adjust rotation if needed
                transform.localPosition = new Vector3(-originalLocalPosition.x, originalLocalPosition.y, originalLocalPosition.z);
                
                // Option: also flip rotation if needed (adjust angle as needed for your specific setup)
                transform.localRotation = Quaternion.Euler(
                    originalLocalRotation.eulerAngles.x,
                    originalLocalRotation.eulerAngles.y + 180f, 
                    originalLocalRotation.eulerAngles.z);
                
                // Comment out debug log
                //if (showDebugInfo)
                //{
                //    Debug.Log($"Flipping flashlight to LEFT. Position: {transform.localPosition}");
                //}
            }
            else
            {
                // Restore original position and rotation
                transform.localPosition = originalLocalPosition;
                transform.localRotation = originalLocalRotation;
                
                // Comment out debug log
                //if (showDebugInfo)
                //{
                //    Debug.Log($"Flipping flashlight to RIGHT. Position: {transform.localPosition}");
                //}
            }
        }
        
        // REMOVED: This was the main source of the console spam
        // if (showDebugInfo && Time.frameCount % 60 == 0)
        // {
        //     string directionStr = shouldFlip ? "LEFT" : "RIGHT";
        //     Debug.Log($"Current direction: {directionStr}, FlipX: {(playerSprite != null ? playerSprite.flipX.ToString() : "N/A")}, " +
        //               $"IsFacingRight: {(armAimScript != null ? armAimScript.IsFacingRight.ToString() : "N/A")}, " +
        //               $"Current position: {transform.localPosition}");
        // }
    }
    
    // Toggle the flashlight on or off
    public void ToggleFlashlight()
    {
        isOn = !isOn;
        UpdateLightState();
        
        // Comment out debug log
        //if (showDebugInfo)
        //{
        //    Debug.Log($"Flashlight toggled: {(isOn ? "ON" : "OFF")}");
        //}
    }
    
    // Update the light component based on current state
    private void UpdateLightState()
    {
        // Try to enable/disable the light component
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isOn;
        }
        else
        {
            // If we couldn't get the light component directly, try using reflection
            // for Unity's 2D light which might be from UnityEngine.Rendering.Universal
            var light2D = GetComponent(typeof(UnityEngine.Rendering.Universal.Light2D));
            if (light2D != null)
            {
                // Use reflection to access the 'enabled' property
                var enabledProperty = light2D.GetType().GetProperty("enabled");
                if (enabledProperty != null)
                {
                    enabledProperty.SetValue(light2D, isOn);
                }
            }
            
            // If we still don't have a valid light reference, just enable/disable the GameObject
            if (light2D == null)
            {
                // Fallback: enable/disable the entire GameObject
                // This works, but will hide the flashlight sprite too if there is one
                gameObject.SetActive(isOn);
                
                // Comment out debug log
                //if (showDebugInfo)
                //{
                //    Debug.LogWarning("No Light component found. Toggling entire GameObject.");
                //}
            }
        }
    }

    // Method for pickups to fully recharge the battery
    public void RechargeFully()
    {
        currentBattery = maxBattery;
        if (showDebugInfo) Debug.Log("Battery fully recharged!");
    }

    // Method for UI to get the battery level (0.0 to 1.0)
    public float GetBatteryNormalized()
    {
        return currentBattery / maxBattery;
    }
}
