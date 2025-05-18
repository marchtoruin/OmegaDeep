using UnityEngine;

public class DebugPauseToggle : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Key to toggle pause")]
    public KeyCode pauseKey = KeyCode.P;
    
    [Tooltip("Key to step forward one frame")]
    public KeyCode stepKey = KeyCode.O;
    
    [Tooltip("Slow motion scale (0-1) when using slow mode")]
    [Range(0.05f, 0.5f)]
    public float slowMotionScale = 0.2f;
    
    [Tooltip("Key to toggle slow motion")]
    public KeyCode slowMotionKey = KeyCode.I;
    
    private bool isPaused = false;
    private bool isSlowMotion = false;
    private float originalTimeScale = 1f;
    private float originalFixedDeltaTime = 0.02f;
    
    void Start()
    {
        // Store original time values
        originalTimeScale = Time.timeScale;
        originalFixedDeltaTime = Time.fixedDeltaTime;
        
        // Log initial state
        Debug.Log($"[DebugPauseToggle] Started. Press {pauseKey} to toggle pause, {stepKey} to step forward when paused, {slowMotionKey} for slow motion.");
    }
    
    void Update()
    {
        // Pause toggle
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
        
        // Frame step (only when paused)
        if (isPaused && Input.GetKeyDown(stepKey))
        {
            StepOneFrame();
        }
        
        // Slow motion toggle
        if (Input.GetKeyDown(slowMotionKey))
        {
            ToggleSlowMotion();
        }
    }
    
    public void TogglePause()
    {
        isPaused = !isPaused;
        
        if (isPaused)
        {
            // Store current time scale if not already paused
            if (Time.timeScale != 0)
            {
                originalTimeScale = Time.timeScale;
                originalFixedDeltaTime = Time.fixedDeltaTime;
            }
            
            // Pause the game
            Time.timeScale = 0;
        }
        else
        {
            // If we're in slow motion, restore to slow motion
            if (isSlowMotion)
            {
                Time.timeScale = slowMotionScale;
                Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionScale;
            }
            else
            {
                // Otherwise restore normal time
                Time.timeScale = originalTimeScale;
                Time.fixedDeltaTime = originalFixedDeltaTime;
            }
        }
        
        Debug.Log($"[DebugPauseToggle] Pause: {isPaused}, TimeScale: {Time.timeScale}");
    }
    
    public void StepOneFrame()
    {
        if (!isPaused) return;
        
        // Temporarily advance one frame by setting timeScale briefly
        Debug.Log("[DebugPauseToggle] Stepping one frame forward");
        
        // Set timeScale very briefly to advance physics & logic
        Time.timeScale = originalTimeScale;
        
        // Immediately re-pause on the next frame
        isPaused = false; // Temporarily unpause
        this.enabled = false; // Disable this component
        
        // Use Invoke to re-enable after one frame completion
        Invoke(nameof(RePauseAfterStep), Time.deltaTime * 2);
    }
    
    private void RePauseAfterStep()
    {
        // Re-pause the game
        Time.timeScale = 0;
        isPaused = true;
        this.enabled = true; // Re-enable component
        Debug.Log("[DebugPauseToggle] Re-paused after step");
    }
    
    public void ToggleSlowMotion()
    {
        if (isPaused) return; // Don't toggle while paused
        
        isSlowMotion = !isSlowMotion;
        
        if (isSlowMotion)
        {
            // Store original time values if not already stored
            if (Mathf.Approximately(Time.timeScale, 1.0f))
            {
                originalTimeScale = Time.timeScale;
                originalFixedDeltaTime = Time.fixedDeltaTime;
            }
            
            // Apply slow motion
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = originalFixedDeltaTime * slowMotionScale;
        }
        else
        {
            // Restore normal time
            Time.timeScale = originalTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime;
        }
        
        Debug.Log($"[DebugPauseToggle] Slow Motion: {isSlowMotion}, TimeScale: {Time.timeScale}");
    }
    
    void OnDestroy()
    {
        // Ensure we restore normal time when the component is destroyed
        if (Time.timeScale != 1.0f)
        {
            Time.timeScale = 1.0f;
            Time.fixedDeltaTime = 0.02f; // Default value
            Debug.Log("[DebugPauseToggle] Destroyed - restored normal time");
        }
    }
}