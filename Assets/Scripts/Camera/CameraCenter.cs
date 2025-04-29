using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))] // Ensure a BoxCollider2D exists
public class CameraCenter : MonoBehaviour
{
    // --- Enums for Actions ---
    public enum FollowAction { DoNothing, CenterOnPlayer, ResumeFollowing }
    public enum ZoomAction { DoNothing, SetTargetZoom, ResumePreviousZoom } // Added Resume

    [Header("Follow Settings")]
    [Tooltip("What should happen to camera following?")]
    public FollowAction followAction = FollowAction.DoNothing; // Default to DoNothing

    [Header("Zoom Settings")]
    [Tooltip("What should happen to camera zoom?")]
    public ZoomAction zoomAction = ZoomAction.DoNothing; // Default to DoNothing
    [Tooltip("The target Orthographic Size (only used if Zoom Action is SetTargetZoom).")]
    public float targetOrthographicSize = 5f;

    [Header("Trigger Settings")]
    [Tooltip("Should this trigger only activate once?")]
    public bool triggerOnce = false;

    private CameraFollow mainCameraFollow;
    private bool hasTriggered = false;

    // Start is called before the first frame update
    void Start()
    {
        // Ensure the collider is set to be a trigger
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} ({nameof(CameraCenter)}) was not set to 'Is Trigger'. Fixing automatically.", this);
            col.isTrigger = true;
        }

        // Find the CameraFollow script in the scene
        // Note: This assumes only one CameraFollow script exists.
        // If you have multiple cameras, you might need a more specific way to find it.
        mainCameraFollow = FindObjectOfType<CameraFollow>();
        if (mainCameraFollow == null)
        {
            Debug.LogError($"Could not find required CameraFollow script! {nameof(CameraCenter)} on {gameObject.name} will not work.", this);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Check if the camera script was found and if the entering object is the player
        if (mainCameraFollow != null && other.CompareTag("Player"))
        {
            // Check if it should trigger only once and if it already has
            if (triggerOnce && hasTriggered)
            {
                return; // Already triggered, do nothing more
            }

            bool triggeredSomething = false;

            // Handle Follow Action
            if (followAction != FollowAction.DoNothing)
            {
                Debug.Log($"[{gameObject.name}] Setting camera follow mode to: {followAction}", this);
                mainCameraFollow.SetFollowMode(followAction);
                triggeredSomething = true;
            }

            // Handle Zoom Action
            switch (zoomAction)
            {
                case ZoomAction.SetTargetZoom:
                    Debug.Log($"[{gameObject.name}] Setting camera zoom to: {targetOrthographicSize}", this);
                    mainCameraFollow.SetTargetZoom(targetOrthographicSize);
                    triggeredSomething = true;
                    break;
                case ZoomAction.ResumePreviousZoom:
                    Debug.Log($"[{gameObject.name}] Resuming previous camera zoom.", this);
                    mainCameraFollow.ResumePreviousZoom(); // Call new method
                    triggeredSomething = true;
                    break;
                // Do nothing for DoNothing case
            }

            // Mark as triggered if set to trigger once and we actually did something
            if (triggerOnce && triggeredSomething)
            {
                hasTriggered = true;
                // Optional: Disable the collider after triggering once
                // GetComponent<Collider2D>().enabled = false;
            }
        }
    }

    // Optional: Draw gizmos in the editor for visibility
    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            // Color based on primary action (zoom takes precedence if set)
            Color gizmoColor;
            if (zoomAction == ZoomAction.SetTargetZoom) gizmoColor = new Color(1, 0.5f, 0, 0.4f); // Orange for Set Zoom
            else if (zoomAction == ZoomAction.ResumePreviousZoom) gizmoColor = new Color(1, 1, 0, 0.4f); // Yellow for Resume Zoom
            else if (followAction == FollowAction.CenterOnPlayer) gizmoColor = new Color(0, 1, 0, 0.3f); // Green for Center
            else if (followAction == FollowAction.ResumeFollowing) gizmoColor = new Color(0, 0, 1, 0.3f); // Blue for Resume Follow
            else gizmoColor = new Color(0.5f, 0.5f, 0.5f, 0.3f); // Grey for DoNothing

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);
        }
    }

    void OnDrawGizmosSelected()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Color gizmoColor;
            if (zoomAction == ZoomAction.SetTargetZoom) gizmoColor = new Color(1, 0.5f, 0, 0.7f);
            else if (zoomAction == ZoomAction.ResumePreviousZoom) gizmoColor = new Color(1, 1, 0, 0.7f);
            else if (followAction == FollowAction.CenterOnPlayer) gizmoColor = new Color(0, 1, 0, 0.6f);
            else if (followAction == FollowAction.ResumeFollowing) gizmoColor = new Color(0, 0, 1, 0.6f);
            else gizmoColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);

            Gizmos.color = gizmoColor;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.offset, col.size);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
