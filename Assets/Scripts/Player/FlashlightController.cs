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
    
    [Header("Child References")]
    [Tooltip("Assign the child 'FlashlightPoint' Transform here")]
    [SerializeField] private Transform flashlightPointTransform; // Reference to the child point

    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false; // Default to false to reduce console spam
    
    // Parent (this object) originals
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private bool wasFlipped = false;
    // Child point originals
    private Vector3 pointOriginalLocalPos;
    private Quaternion pointOriginalLocalRot;
    private SpriteRenderer pointSpriteRenderer; // Add reference for the child's renderer

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
        
        // Find and store originals for child point
        if (flashlightPointTransform == null) // Try to find if not assigned
        {
            flashlightPointTransform = transform.Find("FlashlightPoint");
        }
        if (flashlightPointTransform != null)
        {
            pointOriginalLocalPos = flashlightPointTransform.localPosition;
            pointOriginalLocalRot = flashlightPointTransform.localRotation;
            // Also get the renderer
            pointSpriteRenderer = flashlightPointTransform.GetComponent<SpriteRenderer>();
            if(pointSpriteRenderer == null) Debug.LogError("[FlashlightController] FlashlightPoint does not have a SpriteRenderer!", this);
            
            Debug.Log($"[FlashlightController] Stored FP original pos: {pointOriginalLocalPos}, rot: {pointOriginalLocalRot.eulerAngles}", this);
        }
        else
        {
            Debug.LogError("[FlashlightController] FlashlightPoint child transform not found or assigned!", this);
        }
        
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

        // Also toggle the child point's renderer
        if (pointSpriteRenderer != null)
        {
            pointSpriteRenderer.enabled = isOn;
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

    // Public method called by ArmAim to set the correct orientation
    public void UpdateFlipState(bool isFacingRight)
    {
        // Only apply if the flashlight is on
        if (!isOn) return;

        bool shouldFlip = !isFacingRight; // shouldFlip is true if NOT facing right (i.e., facing left)

        if (shouldFlip != wasFlipped)
        {
            wasFlipped = shouldFlip;
            
            // 1. Flip the Parent (this transform)
            if (shouldFlip) // Facing Left
            {
                transform.localPosition = new Vector3(-originalLocalPosition.x, originalLocalPosition.y, originalLocalPosition.z); 
                transform.localRotation = Quaternion.Euler(
                    originalLocalRotation.eulerAngles.x,
                    originalLocalRotation.eulerAngles.y + 180f, 
                    originalLocalRotation.eulerAngles.z
                );
            }
            else // Facing Right
            {
                transform.localPosition = originalLocalPosition;
                transform.localRotation = originalLocalRotation;
            }

            // 2. Explicitly Flip the Child Point Transform (using its own originals)
            if (flashlightPointTransform != null)
            {
                if (shouldFlip) // Facing Left
                {
                    float targetPosX = -pointOriginalLocalPos.x;
                    Quaternion targetRot = Quaternion.Euler(
                        pointOriginalLocalRot.eulerAngles.x,
                        pointOriginalLocalRot.eulerAngles.y + 180f,
                        pointOriginalLocalRot.eulerAngles.z
                    );
                    flashlightPointTransform.localPosition = new Vector3(targetPosX, pointOriginalLocalPos.y, pointOriginalLocalPos.z);
                    flashlightPointTransform.localRotation = targetRot;
                }
                else // Facing Right
                {
                    // Restore child's original local state
                    flashlightPointTransform.localPosition = pointOriginalLocalPos;
                    flashlightPointTransform.localRotation = pointOriginalLocalRot;
                }
            }
        }
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
}
