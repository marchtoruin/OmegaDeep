using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;
    public float smoothSpeed = 0.125f;
    
    [Header("Background Bounds")]
    public SpriteRenderer backgroundSprite; // Reference to the background sprite renderer
    public bool clampToBackground = true; // Toggle clamping to background
    public float backgroundBuffer = 0f; // Optional buffer around background edges (can be negative)
    
    [Header("World Bounds (Chunked World Support)")]
    public BoxCollider2D worldBoundsCollider; // Reference to the world bounds collider
    
    [Header("Dynamic Offset Settings")]
    [SerializeField] private bool useDynamicOffset = true; // Can be disabled entirely here if needed
    [SerializeField] private float aheadOffset = 6f; // How far ahead to look in the facing direction
    [SerializeField] private float offsetLerpSpeed = 6f; // How quickly the offset transitions
    [SerializeField] private SpriteRenderer playerSprite; // Reference to the player's SpriteRenderer
    private float currentDynamicOffsetX = 0f; // Smoothed offset value
    
    // --- New field to control dynamic offset behavior ---
    private bool dynamicOffsetActive = true;
    // ----------------------------------------------------

    // --- New Zoom Settings ---
    [Header("Zoom Settings")]
    [Tooltip("How quickly the camera zooms in/out")]
    public float zoomSmoothSpeed = 2f;
    private float currentTargetOrthographicSize;
    private float previousOrthographicSize; // Store the size before SetTargetZoom

    private Camera cam;
    private float camHalfHeight;
    private float camHalfWidth;
    private bool hasBounds;
    private Bounds backgroundBounds;
    private bool hasWorldBounds = false;
    private Bounds worldBounds;
    
    void Start()
    {
        // Get the camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraFollow script requires a Camera component!");
            return;
        }
        
        // Try to find WorldBounds collider if not set
        if (worldBoundsCollider == null)
        {
            GameObject wbObj = GameObject.Find("WorldBounds");
            if (wbObj != null)
            {
                worldBoundsCollider = wbObj.GetComponent<BoxCollider2D>();
                if (worldBoundsCollider != null)
                {
                    Debug.Log("Auto-found WorldBounds BoxCollider2D.");
                }
            }
        }
        
        // Auto-find background if not set (fallback)
        if (backgroundSprite == null)
        {
            GameObject bgObject = GameObject.Find("background");
            if (bgObject != null)
            {
                backgroundSprite = bgObject.GetComponent<SpriteRenderer>();
                if (backgroundSprite != null)
                {
                    Debug.Log("Auto-found background sprite.");
                }
            }
        }
        
        UpdateCameraMetrics();

        // Initialize dynamic offset state based on inspector setting
        dynamicOffsetActive = useDynamicOffset;
        currentDynamicOffsetX = offset.x; // Start centered or with base offset

        // Initialize target and previous zoom
        currentTargetOrthographicSize = cam.orthographicSize;
        previousOrthographicSize = currentTargetOrthographicSize; // Start with same values
    }
    
    void UpdateCameraMetrics()
    {
        // Update camera metrics
        if (cam != null && cam.orthographic)
        {
            camHalfHeight = cam.orthographicSize;
            camHalfWidth = camHalfHeight * cam.aspect;
        }
        
        // Update world bounds if collider exists
        hasWorldBounds = false;
        if (worldBoundsCollider != null)
        {
            worldBounds = worldBoundsCollider.bounds;
            hasWorldBounds = true;
        }
        else if (backgroundSprite != null)
        {
            backgroundBounds = backgroundSprite.bounds;
            hasBounds = true;
        }
        else
        {
            hasBounds = false;
        }
    }

    void LateUpdate()
    {
        if (target == null || cam == null) return; // Added null check for cam

        // --- Smooth Zoom ---
        // Check if zoom needs changing
        if (Mathf.Abs(cam.orthographicSize - currentTargetOrthographicSize) > 0.01f)
        {
            // Interpolate towards the target size
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, currentTargetOrthographicSize, zoomSmoothSpeed * Time.deltaTime);
            // IMPORTANT: Update camera metrics immediately after size change for accurate clamping
            UpdateCameraMetrics();
        }
        // -------------------

        // If we have a new camera *aspect ratio*, update metrics (less common)
        // if (cam.aspect * camHalfHeight != camHalfWidth) { UpdateCameraMetrics(); } // More precise check if aspect changes
        
        // If we have a new camera size, update metrics
        if (cam != null && cam.orthographicSize != camHalfHeight)
        {
            UpdateCameraMetrics();
        }
        
        // --- Modified Dynamic Offset Logic ---
        Vector3 currentOffset = offset; // Start with the base offset

        // Only calculate and apply dynamic offset if enabled globally AND currently active
        if (useDynamicOffset && dynamicOffsetActive)
        {
            float targetOffsetX = offset.x; // Default to base if no player sprite
            if (playerSprite != null)
            {
                float direction = playerSprite.flipX ? -1f : 1f;
                targetOffsetX = aheadOffset * direction;
            }
            // Smoothly interpolate the offset X value towards the dynamic target
            currentDynamicOffsetX = Mathf.Lerp(currentDynamicOffsetX, targetOffsetX, offsetLerpSpeed * Time.deltaTime);
            currentOffset.x = currentDynamicOffsetX; // Use the dynamic X
        }
        else
        {
             // If dynamic offset is disabled (globally or via trigger),
             // smoothly return the current dynamic offset towards the base offset X
             currentDynamicOffsetX = Mathf.Lerp(currentDynamicOffsetX, offset.x, offsetLerpSpeed * Time.deltaTime);
             currentOffset.x = currentDynamicOffsetX; // Use the (potentially resetting) dynamic X
             // Or uncomment below for an immediate snap to base offset:
             // currentOffset.x = offset.x;
        }
        // --- End Modified Dynamic Offset Logic ---
        
        // Calculate desired position (target + potentially modified offset)
        Vector3 desiredPosition = target.position + currentOffset;
        
        // Apply smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Apply world bounds clamping if enabled and we have valid bounds
        if (clampToBackground && hasWorldBounds)
        {
            float minX = worldBounds.min.x + camHalfWidth - backgroundBuffer;
            float maxX = worldBounds.max.x - camHalfWidth + backgroundBuffer;
            float minY = worldBounds.min.y + camHalfHeight - backgroundBuffer;
            float maxY = worldBounds.max.y - camHalfHeight + backgroundBuffer;
            
            // Handle case where the camera's view is larger than the world bounds
            if (minX > maxX) 
            {
                float centerX = worldBounds.center.x;
                minX = maxX = centerX;
            }
            
            if (minY > maxY)
            {
                float centerY = worldBounds.center.y;
                minY = maxY = centerY;
            }
            
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }
        // Fallback to background sprite bounds if no world bounds
        else if (clampToBackground && hasBounds)
        {
            float minX = backgroundBounds.min.x + camHalfWidth - backgroundBuffer;
            float maxX = backgroundBounds.max.x - camHalfWidth + backgroundBuffer;
            float minY = backgroundBounds.min.y + camHalfHeight - backgroundBuffer;
            float maxY = backgroundBounds.max.y - camHalfHeight + backgroundBuffer;
            
            if (minX > maxX)
            {
                float centerX = backgroundBounds.center.x;
                minX = maxX = centerX;
            }
            if (minY > maxY)
            {
                float centerY = backgroundBounds.center.y;
                minY = maxY = centerY;
            }
            
            smoothedPosition.x = Mathf.Clamp(smoothedPosition.x, minX, maxX);
            smoothedPosition.y = Mathf.Clamp(smoothedPosition.y, minY, maxY);
        }
        
        // Apply the final position (keeping original z-value to maintain distance)
        transform.position = new Vector3(
            smoothedPosition.x,
            smoothedPosition.y,
            transform.position.z
        );
    }
    
    // This can be called when background or world bounds changes or is resized
    public void RefreshBounds()
    {
        UpdateCameraMetrics();
    }

    // --- Modified public method ---
    public void SetFollowMode(CameraCenter.FollowAction mode) // Changed enum type
    {
        switch (mode)
        {
            case CameraCenter.FollowAction.CenterOnPlayer:
                dynamicOffsetActive = false;
                Debug.Log("CameraFollow: Centering enabled (Dynamic Offset OFF)");
                break;
            case CameraCenter.FollowAction.ResumeFollowing:
                dynamicOffsetActive = true;
                Debug.Log("CameraFollow: Normal following resumed (Dynamic Offset ON)");
                break;
            // Do nothing for DoNothing case
        }
    }

    // --- Modified SetTargetZoom ---
    public void SetTargetZoom(float targetSize)
    {
        if (targetSize > 0)
        {
            // Store the *current* target size as previous *before* changing it
            // Only store if the new target is actually different
            if (Mathf.Abs(currentTargetOrthographicSize - targetSize) > 0.01f)
            {
                previousOrthographicSize = currentTargetOrthographicSize;
                Debug.Log($"CameraFollow: Storing previous zoom: {previousOrthographicSize}");
            }
            currentTargetOrthographicSize = targetSize;
            Debug.Log($"CameraFollow: Setting target zoom to {targetSize}");
        }
        else
        {
             Debug.LogWarning($"CameraFollow: Invalid target zoom size requested: {targetSize}");
        }
    }

    // --- New public method for Zoom Resume ---
    public void ResumePreviousZoom()
    {
        currentTargetOrthographicSize = previousOrthographicSize;
        Debug.Log($"CameraFollow: Resuming previous zoom: {currentTargetOrthographicSize}");
    }
    // ---------------------------------------
}
