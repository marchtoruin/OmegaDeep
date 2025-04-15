using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHealthBar : MonoBehaviour
{
    [Header("Health Bar UI")]
    public Image healthBarFill; // Direct reference to the fill image component
    public Image healthBarBackground; // Optional reference to the background
    public TextMeshProUGUI healthPercentText; // Reference to the percentage label
    
    [Header("Settings")]
    [SerializeField] private bool showDebugMessages = true; // Enabled for debugging
    [SerializeField] private Color fillColor = Color.red;
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.7f);
    
    // Added a field to explicitly set/override width if needed
    [SerializeField] private float explicitWidth = 353f; // Your exact editor width
    [SerializeField] private bool useExplicitWidth = true; // Turn this on to force the width
    
    [Header("Hierarchy Debug")]
    [SerializeField] private RectTransform parentRectTransform; // Reference to parent rect transform
    [SerializeField] private Vector2 targetSizeDelta; // What we want the size delta to be
    [SerializeField] private Vector2 actualSizeDelta; // What it actually is
    
    [Header("Debug (Read-only)")]
    [SerializeField] private float currentFillAmount = 1.0f;
    private Vector2 originalSize; // Will be initialized from actual rect size
    
    // References for health bar manipulation
    private RectTransform fillRectTransform;
    private RectTransform backgroundRectTransform;
    private bool isInitialized = false;
    
    // This is called when the script is loaded or a value changes in the Inspector
    void OnValidate()
    {
        // Update explicit width from editor
        if (useExplicitWidth && Application.isEditor)
        {
            RectTransform rt = GetComponent<RectTransform>();
            if (rt != null && rt.rect.width > 10)
            {
                explicitWidth = rt.rect.width;
            }
        }
    }
    
    void Awake()
    {
        // Make sure we're visible from the start
        gameObject.SetActive(true);
        
        // Attempt to get parent rect transform for debugging
        if (transform.parent != null)
        {
            parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }
    }
    
    void Start()
    {
        // Always assign parent reference first for debugging
        if (transform.parent != null)
        {
            parentRectTransform = transform.parent.GetComponent<RectTransform>();
        }
        
        // Get exact canvas health bar width before any other operations
        RectTransform myRect = GetComponent<RectTransform>();
        if (myRect != null && myRect.rect.width > 0)
        {
            // Store the EXACT inspector-shown width, not a modified runtime width
            explicitWidth = myRect.rect.width;
            Debug.Log($"Start: Capturing exact health bar width from editor: {explicitWidth}");
        }
        
        // Then continue with initialization
        CaptureOriginalSize();
        InitializeHealthBar();
        
        // Force full health - ALWAYS set to the explicit width directly
        if (fillRectTransform != null)
        {
            fillRectTransform.sizeDelta = new Vector2(explicitWidth, fillRectTransform.sizeDelta.y);
            Debug.Log($"Force set exact width: {explicitWidth}");
        }
        
        // Log confirmation and show debug details
        PrintDetailedDebugInfo();
    }
    
    // Capture the correct original size - following badFishHealth pattern
    private void CaptureOriginalSize()
    {
        // Always use the explicit width since we know that's what we want
        originalSize = new Vector2(explicitWidth, 20);
        Debug.Log($"Set original size to explicit width: {explicitWidth}");
            
        // Store for debug display
        targetSizeDelta = originalSize;
    }
    
    // Full component hierarchy initialization
    public void InitializeHealthBar()
    {
        Debug.Log("PlayerHealthBar: Initializing...");
        
        // Find or ensure components exist
        CreateOrSetupBackground();
        SetupFillComponent();
        
        // Set all parent objects to be active
        Transform current = transform;
        while (current != null)
        {
            current.gameObject.SetActive(true);
            current = current.parent;
        }
        
        // Based on badFishHealth script success, make sure the fill has exactly the right anchoring
        if (fillRectTransform != null)
        {
            // Removed runtime anchor/pivot/position settings. User will set these in the Editor.
            fillRectTransform.sizeDelta = new Vector2(explicitWidth, fillRectTransform.sizeDelta.y);
        }
        
        isInitialized = true;
        
        // Cache actual size for debugging
        if (fillRectTransform != null)
        {
            actualSizeDelta = fillRectTransform.sizeDelta;
        }
    }
    
    // Create or setup the background
    private void CreateOrSetupBackground()
    {
        if (healthBarBackground == null)
        {
            // Look for existing background
            Transform bgTransform = transform.Find("Background");
            if (bgTransform != null)
            {
                healthBarBackground = bgTransform.GetComponent<Image>();
                Debug.Log("PlayerHealthBar: Found Background image");
            }
            
            // If still not found, create it
            if (healthBarBackground == null)
            {
                GameObject bgObj = new GameObject("Background");
                bgObj.transform.SetParent(transform, false);
                
                // Set as first child for proper layering
                bgObj.transform.SetSiblingIndex(0);
                
                // Add image component
                healthBarBackground = bgObj.AddComponent<Image>();
                healthBarBackground.color = backgroundColor;
                
                // Get the RectTransform
                backgroundRectTransform = bgObj.GetComponent<RectTransform>();
                
                Debug.Log("PlayerHealthBar: Created Background image");
            }
            else
            {
                backgroundRectTransform = healthBarBackground.GetComponent<RectTransform>();
            }
        }
        else if (backgroundRectTransform == null)
        {
            backgroundRectTransform = healthBarBackground.GetComponent<RectTransform>();
        }
        
        // Configure background to match the container exactly
        if (backgroundRectTransform != null)
        {
            // Full stretch anchors that match the container
            backgroundRectTransform.anchorMin = Vector2.zero;  // Bottom left
            backgroundRectTransform.anchorMax = Vector2.one;   // Top right
            backgroundRectTransform.pivot = new Vector2(0.5f, 0.5f); // Center pivot
            backgroundRectTransform.anchoredPosition = Vector2.zero;
            backgroundRectTransform.sizeDelta = Vector2.zero;  // Fill entire container
            
            // Ensure the parent container is the right size
            RectTransform containerRect = GetComponent<RectTransform>();
            if (containerRect != null && containerRect.rect.width != explicitWidth)
            {
                // This ensures the container itself is the right size
                containerRect.sizeDelta = new Vector2(explicitWidth, containerRect.sizeDelta.y);
                Debug.Log($"Fixed container size to match explicit width: {explicitWidth}");
            }
        }
        
        // Ensure background is active
        if (healthBarBackground != null)
        {
            healthBarBackground.gameObject.SetActive(true);
        }
    }
    
    // Setup the fill component
    private void SetupFillComponent()
    {
        if (healthBarFill == null)
        {
            // First look for a child named "Fill"
            Transform fillTransform = transform.Find("Fill");
            if (fillTransform != null)
            {
                healthBarFill = fillTransform.GetComponent<Image>();
                Debug.Log("PlayerHealthBar: Found Fill image");
            }
            else
            {
                // Create new fill image
                GameObject fillObj = new GameObject("Fill");
                fillObj.transform.SetParent(transform, false);
                fillObj.transform.SetSiblingIndex(1); // Above background
                
                healthBarFill = fillObj.AddComponent<Image>();
                healthBarFill.color = fillColor;
                
                Debug.Log("PlayerHealthBar: Created Fill image");
            }
        }
        
        // Get the fill's RectTransform
        fillRectTransform = healthBarFill.rectTransform;
        
        // Configure the fill to exactly match badFishHealth
        if (fillRectTransform != null)
        {
            // Removed runtime anchor/pivot/position settings. User will set these in the Editor.
            fillRectTransform.sizeDelta = new Vector2(explicitWidth, fillRectTransform.sizeDelta.y);
        }
        
        // Save the original size from the actual measured size
        if (fillRectTransform != null)
        {
            originalSize = fillRectTransform.sizeDelta;
            Debug.Log($"Original size set to actual fill size: {originalSize}");
        }
        
        // Ensure fill is visible
        if (healthBarFill != null)
        {
            healthBarFill.gameObject.SetActive(true);
        }
    }
    
    /// <summary>
    /// Updates the health bar fill amount based on percentage - matches badFishHealth
    /// </summary>
    /// <param name="percent">Health percentage (0-1)</param>
    public void UpdateHealth(float percent)
    {
        // Initialize if not yet done
        if (!isInitialized)
        {
            InitializeHealthBar();
        }
        
        // Save the value for debugging
        currentFillAmount = Mathf.Clamp01(percent);
        
        if (healthBarFill == null || fillRectTransform == null)
        {
            Debug.LogWarning("Failed to update health bar - components missing.");
            return;
        }
        
        // EXACT SAME as badFishHealth update logic - copy/pasted to ensure similarity
        float fillAmount = currentFillAmount;
        fillAmount = Mathf.Clamp01(fillAmount); // Ensure value is between 0-1
            
        // Adjust width through sizeDelta (width setting) 
        // Since we're using left pivot/anchor, this gives right-to-left depletion
        fillRectTransform.sizeDelta = new Vector2(originalSize.x * fillAmount, fillRectTransform.sizeDelta.y);
        
        // Update debug info
        actualSizeDelta = fillRectTransform.sizeDelta;
        
        Debug.Log($"Updated health bar to {fillAmount * 100}% - Width: {fillRectTransform.sizeDelta.x}/{originalSize.x}");
        
        // Update percentage text if assigned
        if (healthPercentText != null)
            healthPercentText.text = Mathf.RoundToInt(fillAmount * 100) + "%";
    }
    
    // Detailed debug output
    public void PrintDetailedDebugInfo()
    {
        string output = "HEALTH BAR DETAILED DEBUG:\n";
        
        // Component references
        output += $"Fill component: {(healthBarFill != null ? "FOUND" : "MISSING")}\n";
        output += $"Background component: {(healthBarBackground != null ? "FOUND" : "MISSING")}\n";
        
        // Transform hierarchy
        output += "Hierarchy: ";
        Transform current = transform;
        while (current != null)
        {
            output += current.name + " > ";
            current = current.parent;
        }
        output += "null\n";
        
        // Fill details
        if (fillRectTransform != null)
        {
            output += $"Fill anchors: min={fillRectTransform.anchorMin}, max={fillRectTransform.anchorMax}\n";
            output += $"Fill pivot: {fillRectTransform.pivot}\n";
            output += $"Fill position: {fillRectTransform.anchoredPosition}\n";
            output += $"Fill size: {fillRectTransform.sizeDelta}\n";
            output += $"Target width: {explicitWidth}, Actual width: {fillRectTransform.sizeDelta.x}\n";
        }
        
        // Canvas details
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            output += $"Canvas scaling: {canvas.scaleFactor}, render mode: {canvas.renderMode}\n";
        }
        
        Debug.Log(output);
    }
    
    // Force complete reinitialize
    [ContextMenu("Force Initialize")]
    public void ForceInitialize()
    {
        // Capture container size first
        RectTransform rt = GetComponent<RectTransform>();
        if (rt != null && rt.rect.width > 10)
        {
            explicitWidth = rt.rect.width;
            Debug.Log($"ForceInitialize: Updated explicit width from RectTransform: {explicitWidth}");
        }
        
        // Then do initialization
        CaptureOriginalSize();
        isInitialized = false;
        InitializeHealthBar();
        
        // Explicitly ensure fill matches the exact width
        if (fillRectTransform != null)
        {
            fillRectTransform.sizeDelta = new Vector2(explicitWidth, fillRectTransform.sizeDelta.y);
        }
        
        // Set to full health after ensuring width
        UpdateHealth(1.0f);
        
        // Debug info
        PrintDetailedDebugInfo();
    }
    
    // Test methods for the health bar
    [ContextMenu("Test Half Health")]
    public void TestHalfHealth()
    {
        UpdateHealth(0.5f);
    }
    
    [ContextMenu("Test Full Health")]
    public void TestFullHealth()
    {
        UpdateHealth(1.0f);
    }
    
    [ContextMenu("Test Low Health")]
    public void TestLowHealth()
    {
        UpdateHealth(0.2f);
    }
    
    // Debugging tools with added width measurements
    [ContextMenu("Show Debug Info")]
    public void ShowDebugInfo()
    {
        // Get current measurements
        if (fillRectTransform != null)
        {
            actualSizeDelta = fillRectTransform.sizeDelta;
        }
        
        // Get container width
        RectTransform containerRect = GetComponent<RectTransform>();
        float containerWidth = containerRect != null ? containerRect.rect.width : 0;
        
        // Add explicit measurements to debug output
        Debug.Log($"Container width: {containerWidth}, Target width: {explicitWidth}, Fill width: {actualSizeDelta.x}");
        
        // Print full debug info
        PrintDetailedDebugInfo();
    }
} 