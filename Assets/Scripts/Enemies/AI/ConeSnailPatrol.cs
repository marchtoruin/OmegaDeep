using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class ConeSnailPatrol : MonoBehaviour
{
    [Header("Patrol Points (A, B, C, D...)")]
    [Tooltip("Assign patrol points in order. Patrol Point A will be the snail's starting position.")]
    public Transform[] patrolPoints;

    [Header("Movement Settings")]
    public float moveSpeed = 1.5f;
    public float forceMultiplier = 10f; // Added for force-based movement
    public float pauseDuration = 1.0f;

    [Header("Surface Alignment")]
    [Tooltip("How far below the snail to check for ground.")]
    public float surfaceRaycastLength = 0.5f;
    [Tooltip("How quickly the snail rotates to match the ground.")]
    public float rotationSmoothSpeed = 10f;

    [Tooltip("Slight vertical offset for raycast origins to avoid starting inside colliders.")]
    public float raycastVerticalOffset = 0.01f;

    [Tooltip("Distance side raycasts are offset from the center for ground check.")]
    public float raycastSpread = 0.1f;

    [Tooltip("Force applied downwards to keep the snail stuck to surfaces.")]
    public float stickingForce = 10f;

    [Tooltip("Layer(s) considered as ground for the snail.")]
    public LayerMask groundLayer;

    [Header("Random Movement (if no patrol points)")]
    public float randomMoveSpeed = 1.5f;
    public float minMoveTime = 1f;
    public float maxMoveTime = 3f;
    [Space(10)] // Add some visual spacing
    [Tooltip("(Random Movement Only) Limit movement to a maximum distance from the starting point?")]
    public bool limitMoveDistance = false; 
    [Tooltip("(Random Movement Only) Maximum distance the snail can move from its starting point if limitMoveDistance is true.")]
    public float maxMoveDistance = 5.0f; 
    
    private float randomMoveTimer = 0f;
    private int randomMoveDirection = 1;
    private bool useRandomMovement = false;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private int currentTargetIndex = 1; // Always start by moving to B
    private int direction = 1; // 1 = forward, -1 = backward
    private bool isPaused = false;
    private bool isGrounded = false; // Track if the snail is on the ground
    private Vector3 startPosition;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0; // Disable standard gravity to use custom sticking force
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    void Start()
    {
        // Ensure Patrol Point A is at the snail's starting position
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
        {
            patrolPoints[0].position = transform.position;
        }
        startPosition = transform.position;
        
        currentTargetIndex = 1; // Always start by moving to B
        direction = 1;
        if (patrolPoints == null || patrolPoints.Length < 2)
        {
            useRandomMovement = true;
            // REMOVED Call to non-existent method
            // PickRandomDirection(); 
            
            // ADDED: Initialize random state directly
            randomMoveDirection = Random.value < 0.5f ? -1 : 1;
            randomMoveTimer = Random.Range(minMoveTime, maxMoveTime);
            if (spriteRenderer != null) spriteRenderer.flipX = randomMoveDirection < 0;
            Debug.Log($"[ConeSnailPatrol - Start] No patrol points. Initializing Random Movement. Direction: {randomMoveDirection}, Timer: {randomMoveTimer:F2}s");
        }
    }

    void Update()
    {
        // Only handle sprite flipping in Update based on movement direction
        if (isPaused) return;

        if (useRandomMovement)
        {
            if (spriteRenderer != null)
                spriteRenderer.flipX = randomMoveDirection < 0;
        }
        else if (patrolPoints != null && patrolPoints.Length >= 2)
        {
            Transform targetPoint = patrolPoints[currentTargetIndex];
            Vector2 targetPos = targetPoint.position;
            Vector2 directionToTarget = targetPos - rb.position;
            if (spriteRenderer != null)
            {
                // Flip based on the horizontal component of the direction to the target
                if (directionToTarget.x < -0.1f) // Add a small threshold
                    spriteRenderer.flipX = true;
                else if (directionToTarget.x > 0.1f)
                    spriteRenderer.flipX = false;
            }
        }
    }


    void FixedUpdate()
    {
        // --- Surface alignment and Ground Check ---
        Vector3 verticalOffset = transform.up * raycastVerticalOffset;
        Vector3 centerOrigin = transform.position + verticalOffset;
        Vector3 forwardOrigin = centerOrigin + transform.right * raycastSpread;
        Vector3 backwardOrigin = centerOrigin - transform.right * raycastSpread;
        Vector3 raycastDirection = -transform.up;

        RaycastHit2D centerHit = Physics2D.Raycast(centerOrigin, raycastDirection, surfaceRaycastLength, groundLayer);
        RaycastHit2D forwardHit = Physics2D.Raycast(forwardOrigin, raycastDirection, surfaceRaycastLength, groundLayer);
        RaycastHit2D backwardHit = Physics2D.Raycast(backwardOrigin, raycastDirection, surfaceRaycastLength, groundLayer);

        // Consider grounded if any of the three rays hit the ground layer
        isGrounded = centerHit.collider != null || forwardHit.collider != null || backwardHit.collider != null;

        // Use the center hit for alignment primarily, if available
        RaycastHit2D primaryHit = centerHit.collider != null ? centerHit : (forwardHit.collider != null ? forwardHit : backwardHit); // Fallback to side hits if center misses

        // Visualize the raycasts in Scene view
        Color centerRayColor = centerHit.collider != null ? Color.green : Color.red;
        Color forwardRayColor = forwardHit.collider != null ? Color.green : Color.red;
        Color backwardRayColor = backwardHit.collider != null ? Color.green : Color.red;
        Debug.DrawRay(centerOrigin, raycastDirection * surfaceRaycastLength, centerRayColor);
        Debug.DrawRay(forwardOrigin, raycastDirection * surfaceRaycastLength, forwardRayColor);
        Debug.DrawRay(backwardOrigin, raycastDirection * surfaceRaycastLength, backwardRayColor);

        // DEBUG: Log ground check results (optional, can be verbose)
        // Debug.Log($"[ConeSnailPatrol-DEBUG] Ground Check: isGrounded={isGrounded}, Center Hit={(centerHit.collider != null ? centerHit.collider.name : "None")}, Fwd Hit={(forwardHit.collider != null ? forwardHit.collider.name : "None")}, Bwd Hit={(backwardHit.collider != null ? backwardHit.collider.name : "None")}");

        if (isGrounded && primaryHit.collider != null) // Ensure we have a valid hit reference for calculations
        {
            Vector2 surfaceNormal = primaryHit.normal;
            float angle = Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothSpeed * Time.fixedDeltaTime);

            // Apply sticking force perpendicular to the surface
            rb.AddForce(-transform.up * stickingForce);
        }
        // else: Snail is airborne, no forces applied (it will just float unless standard gravity is > 0)

        if (isPaused || !isGrounded) // Don't move if paused or airborne
        {
            // Optional: Could add a small downward force here if needed when airborne, but gravity should handle it.
            return;
        }


        if (useRandomMovement)
        {
            // --- ADDED: Continuous Boundary Check --- 
            if (limitMoveDistance)
            {
                float currentDistance = Vector3.Distance(transform.position, startPosition);
                if (currentDistance > maxMoveDistance)
                {                    Debug.Log($"[ConeSnailPatrol - FixedUpdate Check] OUTSIDE BOUNDS! Dist={currentDistance:F1}, Max={maxMoveDistance:F1}. Forcing stop & reversal.");
                    rb.velocity = Vector2.zero; // Stop momentum
                    
                    // Force direction back towards start
                    Vector2 directionToStart = ((Vector2)startPosition - rb.position).normalized;
                    float dot = Vector2.Dot(directionToStart, transform.right);
                    randomMoveDirection = (dot >= 0) ? 1 : -1; 
                    
                    // Optionally: Trigger pause immediately? For now, just reverse & let next pause handle timer.
                    // isPaused = true;
                    // StartCoroutine(PauseAndPickRandomDirection()); // This might cause rapid pausing
                    
                    // Ensure sprite flips immediately
                    if (spriteRenderer != null) spriteRenderer.flipX = randomMoveDirection < 0;
                }
            }
            // --- END Continuous Boundary Check --- 
            
            randomMoveTimer -= Time.fixedDeltaTime;
            // Apply force along the surface (transform.right is aligned to surface)
            rb.AddForce(transform.right * randomMoveDirection * randomMoveSpeed * forceMultiplier);
            
            if (randomMoveTimer <= 0f)
            {
                // Original logic: Pause when timer runs out
                isPaused = true; // Ensure pause flag is set
                StartCoroutine(PauseAndPickRandomDirection()); // Pause briefly before switching
            }
        }
        else // Patrol movement
        {
            if (patrolPoints == null || patrolPoints.Length < 2) return;

            Transform targetPoint = patrolPoints[currentTargetIndex];
            Vector2 targetPos = targetPoint.position;
            Vector2 directionToTarget = targetPos - rb.position;

            // Apply force along the surface, oriented towards the target point's direction
            // Determine if target is generally left or right relative to snail's right vector
            float dot = Vector2.Dot(directionToTarget.normalized, transform.right);
            int moveDirection = (dot >= 0) ? 1 : -1; // Move along transform.right or -transform.right

            rb.AddForce(transform.right * moveDirection * moveSpeed * forceMultiplier);

            // Check if reached patrol point (use distance along the direction of travel)
            // Project the vector to the target onto the current movement direction
            float distanceToTargetAlongPath = Vector2.Dot(directionToTarget, transform.right * moveDirection);

            // Consider 'close enough' if the projected distance is small,
            // or if the snail has overshot (dot product signs differ)
            bool gettingCloser = Vector2.Dot(directionToTarget.normalized, rb.velocity.normalized) > 0.5f; // Are we generally moving towards it?
             float distanceThreshold = 0.5f;

            // Simplified check: Check distance and if we are moving roughly towards the target
            if (Vector2.Distance(rb.position, targetPos) < distanceThreshold && gettingCloser)
            {
                Debug.Log($"[ConeSnailPatrol] Reached proximity of Patrol Point {currentTargetIndex} at {targetPos}");
                rb.velocity = Vector2.zero; // Stop moving before pausing
                isPaused = true;
                StartCoroutine(PauseAndSwitch());
            }
            else if (Vector2.Distance(rb.position, targetPos) < distanceThreshold * 0.5f) // Closer threshold if not moving towards it (might have overshot slightly)
            {
                 Debug.Log($"[ConeSnailPatrol] Reached Patrol Point {currentTargetIndex} at {targetPos} (overshot/close proximity).");
                rb.velocity = Vector2.zero;
                isPaused = true;
                StartCoroutine(PauseAndSwitch());
            }
        }
    }

    private IEnumerator PauseAndSwitch()
    {
        // rb.velocity = Vector2.zero; // Already set velocity to zero before calling
        Debug.Log($"[ConeSnailPatrol] Pausing at Patrol Point {currentTargetIndex}");
        yield return new WaitForSeconds(pauseDuration);

        // Ensure snail is still grounded before resuming (might have fallen while paused?)
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, surfaceRaycastLength, groundLayer); // Use LayerMask
        if (hit.collider == null) // Only need to check if hit occurred on the correct layer
        {
             Debug.LogWarning("[ConeSnailPatrol] Lost ground during pause. Waiting to resume.");
             // Optionally wait here until grounded again, or just let gravity handle it
             isPaused = false; // Allow gravity to potentially pull it down
             yield break; // Exit coroutine early if not grounded
        }

        // --- RESTORED Original Patrol Switching Logic --- 
        if (patrolPoints.Length == 2)
        {
            currentTargetIndex = 1 - currentTargetIndex;
            Debug.Log($"[ConeSnailPatrol] Switching to Patrol Point {currentTargetIndex}");
        }
        else
        {
            currentTargetIndex += direction;
            if (currentTargetIndex >= patrolPoints.Length)
            {
                direction = -1;
                currentTargetIndex = patrolPoints.Length - 2;
            }
            else if (currentTargetIndex < 0)
            {
                direction = 1;
                currentTargetIndex = 1;
            }
            Debug.Log($"[ConeSnailPatrol] Switching to Patrol Point {currentTargetIndex} (direction: {direction})");
        }
        // --- END Restored Logic --- 
        
        isPaused = false;
        AlignToSurface(); // Re-align rotation immediately after pause if needed
    }

    private IEnumerator PauseAndPickRandomDirection()
    {
        isPaused = true; // Pause movement
        rb.velocity = Vector2.zero; // Stop current motion
        Debug.Log($"[ConeSnailPatrol] Pausing before random direction change.");
        yield return new WaitForSeconds(pauseDuration / 2); // Shorter pause for random turns

        // Ensure still grounded
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, surfaceRaycastLength, groundLayer); // Use LayerMask
        if (hit.collider == null) // Only need to check if hit occurred on the correct layer
        {
             Debug.LogWarning("[ConeSnailPatrol] Lost ground during random pause. Waiting to resume.");
             isPaused = false;
             yield break;
        }

        // --- MODIFIED: Add distance check logic for random movement --- 
        if (limitMoveDistance)
        {
            Debug.Log($"[ConeSnailPatrol - Random Limit Check] limitMoveDistance=True. MaxDistance={maxMoveDistance:F1}. StartPos={startPosition}. CurrentPos={transform.position}");
            
            float currentDistance = Vector3.Distance(transform.position, startPosition);
            float threshold = maxMoveDistance * 0.95f; // Use 95% threshold

            Debug.Log($"[ConeSnailPatrol - Random Limit Check] CurrentDistance={currentDistance:F1}, Threshold={threshold:F1}");

            if (currentDistance >= threshold) // At or past boundary
            {
                Vector2 directionToStart = ((Vector2)startPosition - rb.position).normalized;
                float dot = Vector2.Dot(directionToStart, transform.right);
                randomMoveDirection = (dot >= 0) ? 1 : -1; // Force direction generally towards start
                
                // ADDED: Force a short move timer when turning back from boundary
                randomMoveTimer = 0.5f; 
                
                Debug.Log($"[ConeSnailPatrol - Random Limit Check] At/Near Boundary! Forcing direction towards start ({randomMoveDirection}). Setting short timer: {randomMoveTimer}s.");
            }
            else // Within boundary
            {
                randomMoveDirection = Random.value < 0.5f ? -1 : 1; // Pick randomly
                // ADDED: Set standard random timer
                randomMoveTimer = Random.Range(minMoveTime, maxMoveTime);
                 Debug.Log($"[ConeSnailPatrol - Random Limit Check] Inside boundary. Picked random direction: {randomMoveDirection}. Setting timer: {randomMoveTimer:F2}s.");
            }
        }
        else // limitMoveDistance is False
        {
            randomMoveDirection = Random.value < 0.5f ? -1 : 1; // Pick randomly
            // ADDED: Set standard random timer
            randomMoveTimer = Random.Range(minMoveTime, maxMoveTime);
            Debug.Log($"[ConeSnailPatrol - Random Limit Check] limitMoveDistance=False. Picked random direction: {randomMoveDirection}. Setting timer: {randomMoveTimer:F2}s.");
        }
        
        // MOVED: Flip sprite after direction and timer are determined
        if (spriteRenderer != null)
            spriteRenderer.flipX = randomMoveDirection < 0;

        isPaused = false; // Resume movement
        AlignToSurface(); // Re-align rotation immediately after pause if needed
    }


    // Original PickRandomDirection is now effectively replaced by the logic in PauseAndPickRandomDirection
    // We can remove it or keep it as a private helper if needed elsewhere, but it's not used by the main loop now.
    // private void PickRandomDirection()
    // {
    //    ...
    // }

     // Helper function for alignment
    private void AlignToSurface()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, -transform.up, surfaceRaycastLength, groundLayer); // Use LayerMask
        if (hit.collider != null) // Only need to check if hit occurred on the correct layer
        {
            Vector2 surfaceNormal = hit.normal;
            float angle = Mathf.Atan2(surfaceNormal.y, surfaceNormal.x) * Mathf.Rad2Deg;
            Quaternion targetRot = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            // Use a faster slerp or direct set for immediate alignment after pause
            transform.rotation = targetRot;
           // transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f); // Near instant alignment
        }
    }

    // --- ADDED: Gizmo drawing --- 
    private void OnDrawGizmosSelected()
    {
        // Draw max move distance if enabled (only relevant for random movement)
        if (limitMoveDistance)
        {
            // Use startPosition if available (runtime), otherwise use current position (editor)
            Vector3 center = Application.isPlaying ? startPosition : transform.position;
            // Fallback if startPosition hasn't been initialized yet in editor or early runtime
            if (Application.isPlaying && startPosition == Vector3.zero && Time.time < 0.1f) center = transform.position;
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, maxMoveDistance);
            Gizmos.DrawIcon(center, "MovePoint", true); // Add an icon at the start position
        }

        // Optional: Draw lines between patrol points
        if (patrolPoints != null && patrolPoints.Length >= 2)
        {
            Gizmos.color = Color.yellow;
            Vector3 prevPoint = (patrolPoints[0] != null) ? patrolPoints[0].position : transform.position;
            for (int i = 1; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawLine(prevPoint, patrolPoints[i].position);
                    Gizmos.DrawIcon(patrolPoints[i].position, "MovePoint", true);
                    prevPoint = patrolPoints[i].position;
                }
            }
            // Optionally draw line back to start if it loops (not implemented here)
        }
    }
    // --- END Gizmo drawing ---
}