using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BatteryBarUI : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Assign the UI Image component used for the battery fill")]
    public Image batteryFillImage;

    [Tooltip("Assign the TextMeshProUGUI component for the percentage display")]
    public TextMeshProUGUI batteryPercentText;

    private RectTransform fillRectTransform;
    private Vector2 originalSize;
    private bool isInitialized = false;
    private bool foundController = false; // Flag to track if we found the controller

    [Header("Target Flashlight")]
    [Tooltip("Assign the GameObject that has the FlashlightController script (e.g., the Flashlight object). Can be found automatically if left empty.")]
    public FlashlightController flashlightController; // Keep public for optional assignment

    void Start()
    {
        // Initial setup for UI elements
        if (batteryFillImage == null)
        {
            Debug.LogError("[BatteryBarUI] Battery Fill Image is not assigned!", this);
            enabled = false; // Disable script if essential UI is missing
            return;
        }

        if (batteryPercentText == null)
        {
            Debug.LogWarning("[BatteryBarUI] Battery Percent Text is not assigned!", this);
        }

        fillRectTransform = batteryFillImage.rectTransform;
        if (fillRectTransform != null)
        {
            originalSize = fillRectTransform.sizeDelta;
            Debug.Log($"[BatteryBarUI] Initialized UI elements. Original Size: {originalSize}");
            isInitialized = true;
        }
        else
        {
            Debug.LogError("[BatteryBarUI] Could not get RectTransform from Fill Image!", this);
            enabled = false;
            return;
        }

        // --- Attempt to find FlashlightController immediately ---
        TryFindFlashlightController();
    }

    void Update()
    {
        // If UI isn't set up, do nothing
        if (!isInitialized) return;

        // If we haven't found the controller yet, keep trying
        if (!foundController)
        {
            TryFindFlashlightController();
            // If still not found after trying again, exit Update for this frame
            if (!foundController) return; 
        }

        // --- At this point, we should have a valid controller ---

        // Update the fill amount based on the flashlight's current battery level
        if (flashlightController != null && fillRectTransform != null) 
        {
            float normalizedValue = flashlightController.GetBatteryNormalized();

            // Set width based on normalized value and original size
            fillRectTransform.sizeDelta = new Vector2(originalSize.x * normalizedValue, originalSize.y);

            // Update percentage text if assigned
            if (batteryPercentText != null)
            {
                batteryPercentText.text = Mathf.RoundToInt(normalizedValue * 100) + "%";
            }
        }
        // Optional: Handle case where controller might become null later?
        // else if (foundController) 
        // {
        //     Debug.LogWarning("[BatteryBarUI] FlashlightController reference lost!", this);
        //     foundController = false; // Start searching again
        // }
    }

    // Helper method to find the controller
    void TryFindFlashlightController()
    {
        // If already assigned in inspector, use that and mark as found
        if (flashlightController != null)
        {
            if (!foundController) Debug.Log("[BatteryBarUI] Using pre-assigned Flashlight Controller.", this);
            foundController = true;
            return;
        }
        
        // Attempt to find it in the scene
        flashlightController = FindObjectOfType<FlashlightController>();

        if (flashlightController != null)
        {
            Debug.Log("[BatteryBarUI] Flashlight Controller found automatically.", this);
            foundController = true;
        }
        // No error log here, as Update will keep trying
    }
} 