using System.Collections;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class CameraPan : MonoBehaviour
{
    [Header("Pan Settings")]
    [Tooltip("(Optional) Leave empty to use Pan Target Position instead.")]
    public Transform followTarget = null;
    [Tooltip("If Follow Target is set, should the camera follow it during the hold phase?")]
    public bool followDuringHold = false;
    [Tooltip("The static world position the camera should pan to IF Follow Target is empty or Follow During Hold is false.")]
    public Vector3 panTargetPosition = Vector3.zero;
    [Tooltip("How fast the camera moves TO the initial target (units per second).")]
    public float panToSpeed = 5.0f;
    [Tooltip("How long the camera stays at the target / follows the target (seconds).")]
    public float holdDuration = 2.0f;
    [Tooltip("How quickly the camera adjusts to the Follow Target during the hold phase.")]
    public float followSmoothSpeed = 5.0f;
    [Tooltip("How fast the camera moves BACK towards the player (units per second).")]
    public float returnPanSpeed = 4.0f;


    [Header("Control")]
    [Tooltip("Should the CameraFollow script be temporarily disabled? Recommended.")]
    public bool disableCameraFollow = true;
    [Tooltip("Should this trigger only activate once?")]
    public bool triggerOnce = false;

    private CameraFollow mainCameraFollow;
    private Camera mainCamera;
    private bool hasTriggered = false;
    private Coroutine activePanCoroutine = null;
    private PlayerStateMachine playerStateMachine = null; // Cache the player's state machine
    private const float TARGET_REACH_THRESHOLD = 0.1f; // How close is close enough

    void Start()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        if (!col.isTrigger)
        {
            Debug.LogWarning($"Collider on {gameObject.name} ({nameof(CameraPan)}) was not set to 'Is Trigger'. Fixing automatically.", this);
            col.isTrigger = true;
        }

        mainCameraFollow = FindObjectOfType<CameraFollow>();
        if (mainCameraFollow == null) { Debug.LogError($"Could not find required CameraFollow script! {nameof(CameraPan)} on {gameObject.name} will not work.", this); }
        else
        {
            mainCamera = mainCameraFollow.GetComponent<Camera>();
            if (mainCamera == null) { Debug.LogError($"Could not find Camera component on the CameraFollow object! {nameof(CameraPan)} on {gameObject.name} might have issues.", this); }
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (mainCameraFollow != null && mainCamera != null && other.CompareTag("Player") && activePanCoroutine == null)
        {
            // Try to get the player state machine
            playerStateMachine = other.GetComponent<PlayerStateMachine>();
            if (playerStateMachine == null)
            {
                Debug.LogError($"[{gameObject.name}] Player entered trigger but has no PlayerStateMachine component! Cannot control player state.", other.gameObject);
                return; // Don't start sequence if we can't control player
            }

            if (triggerOnce && hasTriggered) { return; }

            Debug.Log($"Player entered {gameObject.name}. Starting camera pan sequence.", this);
            activePanCoroutine = StartCoroutine(DoCameraPanSequence());

            if (triggerOnce) { hasTriggered = true; }
        }
    }

    IEnumerator DoCameraPanSequence()
    {
        // --- 0. Enter Player Cutscene State --- 
        playerStateMachine?.EnterCutsceneState(); // Tell state machine player can't move

        // --- 1. Disable CameraFollow ---
        if (disableCameraFollow && mainCameraFollow.enabled)
        {
            mainCameraFollow.enabled = false;
            Debug.Log($"[{gameObject.name}] Disabled CameraFollow script.");
        }

        Vector3 currentCameraPos = mainCamera.transform.position;
        float cameraZ = currentCameraPos.z; // Store Z to preserve it

        // --- Determine Initial Pan Destination ---
        Vector3 initialPanDestination;
        bool isFollowingDynamic = followDuringHold && followTarget != null;

        if (isFollowingDynamic)
        {
            initialPanDestination = new Vector3(followTarget.position.x, followTarget.position.y, cameraZ);
             Debug.Log($"[{gameObject.name}] Initial pan target: Follow Target ({followTarget.name}) at {initialPanDestination}");
        }
        else
        {
            initialPanDestination = new Vector3(panTargetPosition.x, panTargetPosition.y, cameraZ);
             Debug.Log($"[{gameObject.name}] Initial pan target: Static Position {initialPanDestination}");
        }


        // --- 2. Pan TO Initial Target ---
        Debug.Log($"[{gameObject.name}] Panning camera TO initial target at speed {panToSpeed}.");
        while (Vector3.Distance(mainCamera.transform.position, initialPanDestination) > TARGET_REACH_THRESHOLD)
        {
            mainCamera.transform.position = Vector3.MoveTowards(
                mainCamera.transform.position,
                initialPanDestination,
                panToSpeed * Time.deltaTime
            );
            // Update destination if target moved during initial pan (only if following dynamic)
             if (isFollowingDynamic && followTarget != null)
             {
                  initialPanDestination = new Vector3(followTarget.position.x, followTarget.position.y, cameraZ);
             }
            yield return null; // Wait for the next frame
        }
        mainCamera.transform.position = initialPanDestination; // Snap to exact position
        Debug.Log($"[{gameObject.name}] Reached initial pan target.");


        // --- 3. HOLD at Static Target / FOLLOW Dynamic Target ---
        float holdTimer = 0f;
        Debug.Log($"[{gameObject.name}] Starting Hold/Follow phase for {holdDuration}s. Following dynamic: {isFollowingDynamic}");
        while(holdTimer < holdDuration)
        {
            if (isFollowingDynamic)
            {
                if (followTarget == null) // Target destroyed during follow?
                {
                     Debug.LogWarning($"[{gameObject.name}] Follow Target lost during hold phase. Holding position.");
                     isFollowingDynamic = false; // Revert to holding static position
                }
                else
                {
                    // Calculate desired position based on follow target + camera's base offset
                    Vector3 desiredFollowPos = followTarget.position + mainCameraFollow.offset;
                    Vector3 targetPosWithZ = new Vector3(desiredFollowPos.x, desiredFollowPos.y, cameraZ);

                    // Smoothly move towards the dynamic target
                    mainCamera.transform.position = Vector3.Lerp(
                        mainCamera.transform.position,
                        targetPosWithZ,
                        followSmoothSpeed * Time.deltaTime
                    );
                }
            }
            // If not following dynamic, we just stay put, so only increment timer

            holdTimer += Time.deltaTime;
            yield return null; // Wait for next frame
        }
        Debug.Log($"[{gameObject.name}] Hold/Follow phase finished.");


        // --- 4. Pan BACK Towards Player ---
        Debug.Log($"[{gameObject.name}] Panning camera BACK towards player at speed {returnPanSpeed}.");
        bool followScriptWasEnabled = mainCameraFollow.enabled;
        if (disableCameraFollow && mainCameraFollow.enabled) mainCameraFollow.enabled = false;

        while (true)
        {
            if (mainCameraFollow.enabled && followScriptWasEnabled) break;
            if (mainCameraFollow.target == null)
            {
                 Debug.LogWarning($"[{gameObject.name}] Player target lost during return pan. Re-enabling CameraFollow.", this);
                 break;
            }

            Vector3 idealFollowPos = mainCameraFollow.target.position + mainCameraFollow.offset;
            Vector3 idealFollowPosWithZ = new Vector3(idealFollowPos.x, idealFollowPos.y, cameraZ);

            mainCamera.transform.position = Vector3.MoveTowards(
                mainCamera.transform.position,
                idealFollowPosWithZ,
                returnPanSpeed * Time.deltaTime
            );

            if (Vector3.Distance(mainCamera.transform.position, idealFollowPosWithZ) < TARGET_REACH_THRESHOLD * 2f)
            {
                Debug.Log($"[{gameObject.name}] Reached close proximity to player target during return pan.", this);
                break;
            }
            yield return null;
        }

        // --- 5. Re-enable CameraFollow ---
        if (disableCameraFollow && !mainCameraFollow.enabled)
        {
             Debug.Log($"[{gameObject.name}] Re-enabling CameraFollow script after return pan.");
             mainCameraFollow.enabled = true;
        }

        // --- 5b. Restore Player Control --- 
        // Transition player back to a controllable state (e.g., Idle)
        // The Idle state will handle re-enabling necessary components like movement.
        playerStateMachine?.ChangeState(new PlayerIdleState(playerStateMachine)); 

        // --- 6. Finish ---
        Debug.Log($"[{gameObject.name}] Camera pan sequence complete.");
        activePanCoroutine = null;
    }


    // --- Gizmos ---
    void OnDrawGizmos()
    {
        BoxCollider2D col = GetComponent<BoxCollider2D>();
        Vector3 displayTargetPos = panTargetPosition; // Default to static pos
        Color targetColor = Color.red; // Default marker color

        // Draw trigger area
        if (col != null)
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f); // Orange fill
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(col.offset, col.size);
        }

        // Determine target for gizmo line/marker
         Gizmos.matrix = Matrix4x4.identity; // Use world space
        Vector3 triggerCenter = transform.TransformPoint(col != null ? col.offset : Vector2.zero);

        if (followDuringHold && followTarget != null)
        {
             // If following dynamic target, draw line to it and use its position
             displayTargetPos = followTarget.position;
             Gizmos.color = Color.cyan; // Line to dynamic target
             targetColor = Color.cyan * 0.8f; // Marker color for dynamic target
             Gizmos.DrawLine(triggerCenter, displayTargetPos);
        }
        else
        {
             // If using static target OR followTarget isn't set, draw line to static pos
             displayTargetPos = panTargetPosition;
             Gizmos.color = Color.yellow; // Line to static target
             targetColor = Color.red; // Marker color for static target
             Gizmos.DrawLine(triggerCenter, displayTargetPos);
        }

        // Draw a marker at the display target position
        Gizmos.color = targetColor;
        Gizmos.DrawWireSphere(displayTargetPos, 0.5f);
        Gizmos.DrawLine(displayTargetPos - Vector3.up * 0.7f, displayTargetPos + Vector3.up * 0.7f);
        Gizmos.DrawLine(displayTargetPos - Vector3.right * 0.7f, displayTargetPos + Vector3.right * 0.7f);
    }

    void OnDrawGizmosSelected()
    {
         BoxCollider2D col = GetComponent<BoxCollider2D>();
         // Draw brighter trigger wireframe
         if (col != null)
         {
             Gizmos.color = new Color(1, 0.5f, 0, 0.7f);
             Gizmos.matrix = transform.localToWorldMatrix;
             Gizmos.DrawWireCube(col.offset, col.size);
         }

        // Draw brighter lines/markers
         Gizmos.matrix = Matrix4x4.identity;
         Vector3 triggerCenter = transform.TransformPoint(col != null ? col.offset : Vector2.zero);
         Vector3 displayTargetPos = panTargetPosition;
         Color targetColor = Color.red * 1.2f;
         Color lineColor = Color.yellow * 1.2f;

         if (followDuringHold && followTarget != null)
         {
            displayTargetPos = followTarget.position;
            targetColor = Color.cyan * 1.2f;
            lineColor = Color.cyan * 1.2f;
         }

         Gizmos.color = lineColor;
         Gizmos.DrawLine(triggerCenter, displayTargetPos);
         Gizmos.color = targetColor;
         Gizmos.DrawWireSphere(displayTargetPos, 0.55f);
    }
}
