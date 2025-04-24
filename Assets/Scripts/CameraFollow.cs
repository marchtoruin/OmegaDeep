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
    [SerializeField] private float aheadOffset = 6f; // How far ahead to look in the facing direction
    [SerializeField] private float offsetLerpSpeed = 6f; // How quickly the offset transitions
    [SerializeField] private SpriteRenderer playerSprite; // Reference to the player's SpriteRenderer
    private float currentDynamicOffsetX = 0f; // Smoothed offset value
    
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
        // Exit if no target
        if (target == null) return;
        
        // If we have a new camera size, update metrics
        if (cam != null && cam.orthographicSize != camHalfHeight)
        {
            UpdateCameraMetrics();
        }
        
        // --- Dynamic Offset Logic ---
        float targetOffsetX = offset.x;
        if (playerSprite != null)
        {
            float direction = playerSprite.flipX ? -1f : 1f;
            targetOffsetX = aheadOffset * direction;
        }
        // Smoothly interpolate the offset X value
        currentDynamicOffsetX = Mathf.Lerp(currentDynamicOffsetX, targetOffsetX, offsetLerpSpeed * Time.deltaTime);
        Vector3 dynamicOffset = offset;
        dynamicOffset.x = currentDynamicOffsetX;
        // --- End Dynamic Offset Logic ---
        
        // Calculate desired position (target + dynamic offset)
        Vector3 desiredPosition = target.position + dynamicOffset;
        
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
}
