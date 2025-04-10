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
    
    private Camera cam;
    private float camHalfHeight;
    private float camHalfWidth;
    private bool hasBounds;
    private Bounds backgroundBounds;
    
    void Start()
    {
        // Get the camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("CameraFollow script requires a Camera component!");
            return;
        }
        
        // Auto-find background if not set
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
        
        // Update background bounds information
        hasBounds = false;
        if (backgroundSprite != null)
        {
            backgroundBounds = backgroundSprite.bounds;
            hasBounds = true;
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
        
        // Calculate desired position (target + offset)
        Vector3 desiredPosition = target.position + offset;
        
        // Apply smooth follow
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // Apply bounds clamping if enabled and we have valid bounds
        if (clampToBackground && hasBounds)
        {
            // Calculate the clamping bounds (background bounds adjusted by camera size)
            float minX = backgroundBounds.min.x + camHalfWidth - backgroundBuffer;
            float maxX = backgroundBounds.max.x - camHalfWidth + backgroundBuffer;
            float minY = backgroundBounds.min.y + camHalfHeight - backgroundBuffer;
            float maxY = backgroundBounds.max.y - camHalfHeight + backgroundBuffer;
            
            // Handle case where the camera's view is larger than the background
            if (minX > maxX) 
            {
                // Center camera horizontally on background
                float centerX = backgroundBounds.center.x;
                minX = maxX = centerX;
            }
            
            if (minY > maxY)
            {
                // Center camera vertically on background
                float centerY = backgroundBounds.center.y;
                minY = maxY = centerY;
            }
            
            // Apply the clamping
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
    
    // This can be called when background changes or is resized
    public void RefreshBounds()
    {
        UpdateCameraMetrics();
    }
}
