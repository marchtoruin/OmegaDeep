using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class DiverMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float sinkSpeed = 0.5f; // Speed at which diver sinks when idle
    public float horizontalDrag = 3f; // Extra drag applied to horizontal movement when no input
    
    [Header("Boost Settings")]
    public float boostMultiplier = 2.0f; // How much faster the diver moves when boosting
    public KeyCode boostKey = KeyCode.Space; // Key to activate boost
    
    [Header("Spawn Settings")]
    public string spawnPointName = "PlayerSpawn"; // Name of the spawn point GameObject
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool hasInput;
    private bool isBoosting;
    private float currentSpeedMultiplier = 1.0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }
    
    void Start()
    {
        // Find the spawn point
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        
        // Set player position to spawn point if found
        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
            // Reset velocity when spawning
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            Debug.Log($"Player spawned at: {spawnPoint.transform.position}");
        }
        else
        {
            Debug.LogWarning($"Spawn point '{spawnPointName}' not found in scene. Using current position.");
        }
    }

    void Update()
    {
        // Get smooth input instead of instant snapping
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        
        // Detect if we have any meaningful input
        hasInput = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f;
        
        // Normalize for consistent speed in all directions
        moveInput = new Vector2(moveX, moveY).normalized;
        
        // Detect boost input
        isBoosting = Input.GetKey(boostKey) && hasInput;
        
        // Set the appropriate speed multiplier based on boost state
        currentSpeedMultiplier = isBoosting ? boostMultiplier : 1.0f;
    }

    void FixedUpdate()
    {
        if (hasInput)
        {
            // Calculate target velocity with boost if applicable
            Vector2 targetVelocity = moveInput * moveSpeed * currentSpeedMultiplier;
            
            // When there is input, apply velocity
            // If boosting, apply full velocity for immediate boost
            // If not boosting, we can optionally smooth it for better feel
            if (isBoosting)
            {
                rb.velocity = targetVelocity;
            }
            else
            {
                // Option 1: Direct velocity control (same as before)
                rb.velocity = targetVelocity;
                
                // Option 2: For more fluid transition from boost to normal:
                // Uncomment this and comment out the line above for smoother transitions
                // rb.velocity = Vector2.Lerp(rb.velocity, targetVelocity, 0.5f);
            }
        }
        else
        {
            // When no input:
            // 1. Apply drag to horizontal movement to slow down gradually
            Vector2 currentVelocity = rb.velocity;
            float dragFactor = 1f - (horizontalDrag * Time.fixedDeltaTime);
            dragFactor = Mathf.Clamp01(dragFactor); // Ensure it's between 0 and 1
            
            // 2. Let gravity handle vertical movement, but ensure minimum sink speed
            // If moving downward slower than sinkSpeed, ensure we sink at least at sinkSpeed
            if (currentVelocity.y > -sinkSpeed)
            {
                currentVelocity.y = -sinkSpeed;
            }
            
            // Apply horizontal drag but keep vertical velocity
            currentVelocity.x *= dragFactor;
            
            // Apply the adjusted velocity
            rb.velocity = currentVelocity;
        }
    }
}
