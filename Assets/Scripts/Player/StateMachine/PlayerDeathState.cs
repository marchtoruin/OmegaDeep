using UnityEngine;
using System.Collections; // Required for IEnumerator
using UnityEngine.UI; // Required for Image

public class PlayerDeathState : PlayerBaseState
{
    private Coroutine activateScreenCoroutine; // Keep track of the coroutine
    private Color screenDimColor = new Color(0, 0, 0, 0.6f); // Define the dim color here

    public PlayerDeathState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entering Death State");

        // Disable all player actions
        if (stateMachine.DiverMovement != null) 
        {
            stateMachine.DiverMovement.enabled = false;
            stateMachine.DiverMovement.SetKnockbackState(false); // Call the method, don't assign to it
        }
        // Disable the ArmAim component directly
        if (stateMachine.ArmAim != null) stateMachine.ArmAim.enabled = false;
        
        if (stateMachine.DiverShooter != null) stateMachine.DiverShooter.enabled = false; // Disable shooter script
        if (stateMachine.FlashlightController != null) stateMachine.FlashlightController.enabled = false; // Example: Disable flashlight

        // Play death animation
        stateMachine.Animator?.SetTrigger("Die"); // Example trigger

        // Disable physics interactions?
        if (stateMachine.Rb != null)
        {
            stateMachine.Rb.velocity = Vector2.zero;
            stateMachine.Rb.isKinematic = true; // Stop responding to physics
        }
        // Disable colliders?
        // Collider2D[] colliders = stateMachine.GetComponentsInChildren<Collider2D>();
        // foreach(var col in colliders) { col.enabled = false; }

        // Apply Slow Motion
        if (stateMachine.PlayerHealth != null)
        {
            Time.timeScale = stateMachine.PlayerHealth.deathSlowMotionScale;
            Time.fixedDeltaTime = stateMachine.PlayerHealth.GetOriginalFixedDeltaTime() * Time.timeScale;
            Debug.Log($"Activated slow motion: Time.timeScale = {Time.timeScale}, fixedDeltaTime = {Time.fixedDeltaTime}");
        }
        else
        {
            Debug.LogWarning("PlayerDeathState: Cannot apply slow motion, PlayerHealth reference missing on StateMachine.");
            // Default to some slow motion if reference is missing?
            // Time.timeScale = 0.15f;
            // Time.fixedDeltaTime = 0.02f * Time.timeScale; // Assuming default 0.02
        }

        // --- UI Activation ---
        // Start coroutine for the Death Panel & Dimming
        if (activateScreenCoroutine != null) stateMachine.StopCoroutine(activateScreenCoroutine);
        activateScreenCoroutine = stateMachine.StartCoroutineFromState(ActivateUICoroutine());
    }

    // Player stays in Death state, doesn't update or transition automatically
    public override void Update() { }

    public override void Exit()
    {
        // Stop the coroutine if it's still running when exiting (e.g., immediate scene reload)
        if (activateScreenCoroutine != null) 
        { 
            stateMachine.StopCoroutine(activateScreenCoroutine); 
            activateScreenCoroutine = null; 
        }
        // This state usually isn't exited cleanly (scene reload)
        // But if it were, we'd re-enable components here.
        Debug.Log("Exiting Death State (Likely via scene reload)");
    }

    // --- Coroutine to find and activate UI elements with retries ---
    private IEnumerator ActivateUICoroutine() // Renamed Coroutine
    {
        Debug.Log("[ActivateUICoroutine] Starting search for DeathPanel and ScreenDimOverlay...");
        GameObject deathPanelObject = null;
        GameObject screenDimOverlayObject = null; // Added variable for overlay
        string panelName = "DeathPanel";
        string overlayName = "ScreenDimOverlay"; // Added name for overlay
        float searchStartTime = Time.time;
        float searchTimeout = 2.0f; // Try for 2 seconds

        // --- Declare and populate allTransforms ONCE before the loop --- 
        var allTransforms = UnityEngine.Object.FindObjectsOfType<Transform>(true);
        Debug.Log($"[ActivateUICoroutine] Found {allTransforms.Length} Transforms (including inactive) to search through.");

        // --- Search Loop --- 
        while (Time.time < searchStartTime + searchTimeout)
        {
            // --- Try finding Death Panel (if not already found) ---
            if (deathPanelObject == null)
            {
                // Search within the pre-fetched list
                foreach (var t in allTransforms)
                {
                    if (t.name == panelName)
                    {
                        deathPanelObject = t.gameObject;
                        Debug.Log($"[ActivateUICoroutine] Found GameObject named '{panelName}'. Current activeSelf: {deathPanelObject.activeSelf}");
                        break; 
                    }
                }
            }
            
            // --- Try finding Screen Dim Overlay (if not already found) ---
             if (screenDimOverlayObject == null)
            {
                // Search within the same pre-fetched list
                foreach (var t in allTransforms)
                {
                    if (t.name == overlayName)
                    {
                        screenDimOverlayObject = t.gameObject;
                        Debug.Log($"[ActivateUICoroutine] Found GameObject named '{overlayName}' via FindObjectsOfType. Current activeSelf: {screenDimOverlayObject.activeSelf}");
                        break; 
                    }
                }
            }

            // --- Activation Logic (if both found) ---
            if (deathPanelObject != null && screenDimOverlayObject != null)
            {
                // Activate Death Panel
                Debug.Log($"[ActivateUICoroutine] Activating '{deathPanelObject.name}'...");
                deathPanelObject.SetActive(true);
                Debug.Log($"[ActivateUICoroutine] Death screen '{deathPanelObject.name}' activated. Final activeSelf: {deathPanelObject.activeSelf}");

                // Activate Screen Dimming
                 Debug.Log($"[ActivateUICoroutine] Activating '{screenDimOverlayObject.name}' dimming...");
                screenDimOverlayObject.SetActive(true);
                Image dimImage = screenDimOverlayObject.GetComponentInChildren<Image>();
                if (dimImage != null)
                {
                    dimImage.color = screenDimColor;
                    Debug.Log("[ActivateUICoroutine] Screen dimming effect activated.");
                }
                else
                {
                     Debug.LogWarning("[ActivateUICoroutine] Could not find Image component on {overlayName} or its children.");
                }
                
                yield break; // Exit coroutine successfully
            }
            
            // Wait until the next frame before retrying
            yield return null; 
        }

        // --- Timeout Error Logging --- 
        if (deathPanelObject == null)
        {
            Debug.LogError($"[ActivateUICoroutine] Failed to find GameObject named '{panelName}' anywhere in the scene after {searchTimeout} seconds! Ensure it exists and is named correctly.");
        }
         if (screenDimOverlayObject == null)
        {
             Debug.LogError($"[ActivateUICoroutine] Failed to find GameObject named '{overlayName}' anywhere in the scene after {searchTimeout} seconds! Ensure it exists (created by PlayerHealth) and is named correctly.");
        }
    }

    // Helper function to find child recursively, including inactive objects - NO LONGER NEEDED FOR THIS APPROACH
    /*
    private Transform FindDeepChildInactive(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true)) // true includes inactive
        {
            if (child.name == name)
            {
                return child;
            }
        }
        return null; // Not found
    }
    */
}

