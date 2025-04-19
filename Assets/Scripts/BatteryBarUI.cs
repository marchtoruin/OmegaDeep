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

    [Header("Target Flashlight")]
    [Tooltip("Assign the GameObject that has the FlashlightController script (e.g., the Flashlight object)")]
    public FlashlightController flashlightController;

    void Start()
    {
        // Error checking
        if (batteryFillImage == null)
        {
            Debug.LogError("[BatteryBarUI] Battery Fill Image is not assigned!", this);
            enabled = false; // Disable script if setup is wrong
            return;
        }
        if (flashlightController == null)
        {
            Debug.LogError("[BatteryBarUI] Flashlight Controller is not assigned!", this);
            enabled = false;
            return;
        }

        if (batteryPercentText == null)
        {
            // Optional: Find it if not assigned? For now, just warn.
            Debug.LogWarning("[BatteryBarUI] Battery Percent Text is not assigned!", this);
        }

        // Get RectTransform and store original size
        fillRectTransform = batteryFillImage.rectTransform;
        if (fillRectTransform != null)
        {
            originalSize = fillRectTransform.sizeDelta;
            Debug.Log($"[BatteryBarUI] Initialized. Original Size: {originalSize}");
        }
        else
        {
            Debug.LogError("[BatteryBarUI] Could not get RectTransform from Fill Image!", this);
            enabled = false;
            return;
        }

        isInitialized = true;
    }

    void Update()
    {
        if (!isInitialized) return; // Don't update if initialization failed

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
    }
} 