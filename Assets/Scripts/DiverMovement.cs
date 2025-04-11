using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class DiverMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float sinkSpeed = 0.5f; // Speed at which diver sinks when idle
    public float horizontalDrag = 3f; // Extra drag applied to horizontal movement when no input
    
    [Header("Helmet Bubble Emitter")]
    [SerializeField] private Transform helmetBubbleEmitterTransform; // Reference to the helmet bubble emitter GameObject
    [SerializeField] private SpriteRenderer playerSprite; // Reference to the player's sprite renderer
    [SerializeField] private ArmAim armAimScript; // Reference to the ArmAim script (optional)
    
    [Header("Boost Settings")]
    public float boostMultiplier = 2.0f; // How much faster the diver moves when boosting
    public KeyCode boostKey = KeyCode.Space; // Key to activate boost
    
    [Header("Bubble Effect Settings")]
    [SerializeField] private ParticleSystem boostBubbles; // Particle system for boost bubbles
    [SerializeField] private float bubbleEmissionRate = 20f; // Rate at which bubbles are emitted when boosting
    [SerializeField] private float bubbleBaseSpeed = 2f; // Base speed of emitted particles
    [SerializeField] private float bubbleSpeedVariation = 0.5f; // Random variation in bubble speed (0-1)
    [SerializeField] private float bubbleSizeMin = 0.5f; // Minimum bubble size multiplier
    [SerializeField] private float bubbleSizeMax = 1.5f; // Maximum bubble size multiplier
    [SerializeField] private float directionalInfluence = 0.8f; // How much player direction influences bubble direction (0-1)
    [SerializeField] private bool usePlayerVelocityForBubbleSpeed = true; // Use player velocity to determine bubble speed
    
    [Header("Spawn Settings")]
    public string spawnPointName = "PlayerSpawn"; // Name of the spawn point GameObject
    public bool forceZPositionToZero = true; // Force Z position to 0 for 2D visibility
    public bool waitForNextFrame = true; // Wait for next frame before spawning (helps with prefabs)
    
    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool hasInput;
    private bool isBoosting;
    private float currentSpeedMultiplier = 1.0f;
    private bool hasSpawned = false;
    private bool initializationComplete = false;
    
    // Particle system modules
    private ParticleSystem.EmissionModule bubbleEmission;
    private ParticleSystem.MainModule bubbleMain;
    private ParticleSystem.VelocityOverLifetimeModule bubbleVelocity;
    private ParticleSystem.ShapeModule bubbleShape;
    private bool bubbleSystemInitialized = false;
    
    // Last movement direction for particle emission
    private Vector2 lastMoveDirection = Vector2.up;
    private Vector3 originalHelmetBubbleScale; // Store original scale to prevent stretching
    private bool helmetScaleInitialized = false;

    void Awake()
    {
        Debug.Log($"[DiverMovement] Awake called on {gameObject.name}", this);
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"[DiverMovement] Rigidbody2D component not found on {gameObject.name}!", this);
        }
        
        // Initialize particle system modules
        InitializeParticleSystem();
        
        // Store original helmet bubble emitter scale if available
        if (helmetBubbleEmitterTransform != null)
        {
            originalHelmetBubbleScale = helmetBubbleEmitterTransform.localScale;
            helmetScaleInitialized = true;
        }
        
        // Ensure time is running (sometimes gets stuck at 0)
        if (Time.timeScale == 0)
        {
            Debug.LogWarning("[DiverMovement] TimeScale was 0, resetting to 1.0", this);
            Time.timeScale = 1.0f;
        }
    }

    private void InitializeParticleSystem()
    {
        if (boostBubbles != null)
        {
            // Get all modules we want to control
            bubbleEmission = boostBubbles.emission;
            bubbleMain = boostBubbles.main;
            bubbleVelocity = boostBubbles.velocityOverLifetime;
            bubbleShape = boostBubbles.shape;
            
            // CRITICAL: Set simulation space to World to make particles stay in place
            bubbleMain.simulationSpace = ParticleSystemSimulationSpace.World;
            
            // Set up initial emission state (off)
            bubbleEmission.rateOverTime = 0;
            
            // Configure size randomization
            bubbleMain.startSize = new ParticleSystem.MinMaxCurve(bubbleSizeMin, bubbleSizeMax);
            
            // Mark as initialized
            bubbleSystemInitialized = true;
            Debug.Log("[DiverMovement] Bubble particle system initialized", this);
        }
        else
        {
            Debug.LogWarning("[DiverMovement] No boost bubble particle system assigned", this);
        }
    }
    
    void OnEnable()
    {
        Debug.Log($"[DiverMovement] OnEnable called on {gameObject.name}", this);
        
        // If we've already completed initialization but OnEnable is called again
        // (e.g., object was disabled and re-enabled), respawn once more
        if (initializationComplete && !hasSpawned)
        {
            // Use coroutine to handle spawning
            StartCoroutine(HandleSpawning());
        }
    }
    
    void Start()
    {
        Debug.Log($"[DiverMovement] Start called on {gameObject.name}. Current position: {transform.position}", this);
        
        // Use coroutine to handle spawning - this helps with prefab instantiation
        StartCoroutine(HandleSpawning());
    }
    
    IEnumerator HandleSpawning()
    {
        // Wait for one frame if needed (helps with prefabs)
        if (waitForNextFrame)
        {
            Debug.Log("[DiverMovement] Waiting one frame before spawning", this);
            yield return null;
        }
        
        // Find the spawn point
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        
        // Log detailed information about spawn point
        if (spawnPoint != null)
        {
            Debug.Log($"[DiverMovement] Found spawn point '{spawnPointName}' at position: {spawnPoint.transform.position}", this);
            
            // Store original position for comparison
            Vector3 originalPosition = transform.position;
            
            // Set player position to spawn point
            transform.position = forceZPositionToZero ? 
                new Vector3(spawnPoint.transform.position.x, spawnPoint.transform.position.y, 0f) : 
                spawnPoint.transform.position;
            
            // Reset velocity when spawning
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
            }
            
            // Log position change
            Debug.Log($"[DiverMovement] Player moved from {originalPosition} to {transform.position}", this);
            hasSpawned = true;
            
            // Check if camera exists and follows this player
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                // Check if camera has a follow script that targets this player
                CameraFollow followScript = mainCamera.GetComponent<CameraFollow>();
                if (followScript != null)
                {
                    // Ensure the camera's target is set to this player
                    if (followScript.target == transform)
                    {
                        Debug.Log("[DiverMovement] Camera is set to follow this player", this);
                    }
                    else
                    {
                        Debug.LogWarning($"[DiverMovement] Camera has a follow script but is targeting {(followScript.target != null ? followScript.target.name : "null")} instead of this player", this);
                        
                        // Auto-fix: Set camera target to this player
                        followScript.target = transform;
                        Debug.Log("[DiverMovement] Auto-set camera target to this player", this);
                    }
                }
                else
                {
                    Debug.LogWarning("[DiverMovement] Camera doesn't have a CameraFollow script attached", this);
                }
                
                // Check if player is within camera view
                Vector3 viewportPosition = mainCamera.WorldToViewportPoint(transform.position);
                bool isVisible = viewportPosition.x > 0 && viewportPosition.x < 1 && 
                                viewportPosition.y > 0 && viewportPosition.y < 1 &&
                                viewportPosition.z > 0;
                
                Debug.Log($"[DiverMovement] Player is {(isVisible ? "visible" : "NOT visible")} to camera. Viewport position: {viewportPosition}", this);
                
                // If not visible, move camera to player position
                if (!isVisible)
                {
                    Debug.LogWarning("[DiverMovement] Player not visible to camera. Moving camera to player position", this);
                    mainCamera.transform.position = new Vector3(transform.position.x, transform.position.y, mainCamera.transform.position.z);
                }
            }
            else
            {
                Debug.LogWarning("[DiverMovement] No main camera found in the scene", this);
            }
        }
        else
        {
            Debug.LogError($"[DiverMovement] Spawn point '{spawnPointName}' NOT FOUND in scene! Player will stay at current position: {transform.position}", this);
            
            // Check scene for objects to help debugging
            GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>();
            Debug.Log($"[DiverMovement] Scene contains {allObjects.Length} root GameObjects. Listing top-level objects:", this);
            
            int count = 0;
            foreach (GameObject obj in allObjects)
            {
                if (obj.transform.parent == null) // Only list root objects to avoid spam
                {
                    Debug.Log($"[DiverMovement] Root object #{++count}: '{obj.name}'", this);
                }
            }
            
            // Create a spawn point dynamically at the center of the screen if none exists
            Debug.LogWarning("[DiverMovement] Creating temporary spawn point at (0,0,0)", this);
            GameObject tempSpawn = new GameObject("TempPlayerSpawn");
            tempSpawn.transform.position = Vector3.zero;
            transform.position = new Vector3(0, 0, 0);
            hasSpawned = true;
        }
        
        // Force visibility check on the renderer
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            if (!renderer.enabled)
            {
                Debug.LogWarning("[DiverMovement] Sprite Renderer was disabled! Enabling it now.", this);
                renderer.enabled = true;
            }
            Debug.Log($"[DiverMovement] Sprite Renderer is {(renderer.enabled ? "enabled" : "disabled")}, visible: {renderer.isVisible}, sorting layer: {renderer.sortingLayerName}, order: {renderer.sortingOrder}", this);
        }
        
        // Log final position
        Debug.Log($"[DiverMovement] At end of HandleSpawning(), player position is: {transform.position}, active: {gameObject.activeInHierarchy}, enabled: {enabled}", this);
        
        // Mark initialization as complete
        initializationComplete = true;
        
        // Start a delayed position check
        StartCoroutine(CheckPositionAfterDelay());
    }
    
    IEnumerator CheckPositionAfterDelay()
    {
        // Wait half a second
        yield return new WaitForSeconds(0.5f);
        Debug.Log($"[DiverMovement] Position check after delay: {transform.position}, active: {gameObject.activeInHierarchy}", this);
        
        // Check if renderer is visible to camera
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            Debug.Log($"[DiverMovement] After delay: Sprite Renderer is {(renderer.enabled ? "enabled" : "disabled")}, visible: {renderer.isVisible}, sorting layer: {renderer.sortingLayerName}, order: {renderer.sortingOrder}", this);
        }
    }

    void Update()
    {
        // Don't process input until we've spawned
        if (!hasSpawned) return;
        
        // Get smooth input instead of instant snapping
        float moveX = Input.GetAxis("Horizontal");
        float moveY = Input.GetAxis("Vertical");
        
        // Detect if we have any meaningful input
        hasInput = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f;
        
        // Normalize for consistent speed in all directions
        moveInput = new Vector2(moveX, moveY).normalized;
        
        // If we have movement input, update the direction
        if (hasInput && moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput;
        }
        
        // Detect boost input
        bool wasBoosting = isBoosting;
        isBoosting = Input.GetKey(boostKey) && hasInput;
        
        // Set the appropriate speed multiplier based on boost state
        currentSpeedMultiplier = isBoosting ? boostMultiplier : 1.0f;
        
        // Update particle effects based on boosting state
        UpdateBubbleParticleSystem(wasBoosting);
        
        // Flip the helmet bubble emitter based on movement direction
        UpdateHelmetBubbleEmitterFlip();
    }

    private void UpdateBubbleParticleSystem(bool wasBoosting)
    {
        if (!bubbleSystemInitialized || boostBubbles == null) return;
        
        // Set emission rate based on whether we're boosting
        bubbleEmission.rateOverTime = isBoosting ? bubbleEmissionRate : 0f;
        
        // Only update particle system configuration if boosting state changed or we're currently boosting
        if (wasBoosting != isBoosting || isBoosting)
        {
            // Get opposite direction of movement for particles to come from behind the player
            Vector2 emissionDirection = -lastMoveDirection;
            
            // Update shape rotation to emit from behind the player
            float angle = Mathf.Atan2(emissionDirection.y, emissionDirection.x) * Mathf.Rad2Deg;
            bubbleShape.rotation = new Vector3(0, 0, angle);
            
            // Calculate particle velocity
            float currentSpeed = usePlayerVelocityForBubbleSpeed ? 
                rb.velocity.magnitude : (moveSpeed * currentSpeedMultiplier);
            
            float bubbleSpeed = bubbleBaseSpeed + (currentSpeed * directionalInfluence);
            
            // Fix for "Particle Velocity curves must all be in the same mode" error:
            // Set the velocity module using a ParticleSystem.MinMaxCurve with the same mode for all axes
            
            // Create a constant velocity in the emission direction with some variation
            bubbleVelocity.enabled = true;
            
            // Use a constant with multiplier to ensure same mode across all axes
            ParticleSystem.MinMaxCurve xVelocity = new ParticleSystem.MinMaxCurve(
                emissionDirection.x * bubbleSpeed,
                emissionDirection.x * bubbleSpeed * (1 + bubbleSpeedVariation));
            
            ParticleSystem.MinMaxCurve yVelocity = new ParticleSystem.MinMaxCurve(
                emissionDirection.y * bubbleSpeed,
                emissionDirection.y * bubbleSpeed * (1 + bubbleSpeedVariation));
                
            // Set all velocity components to the same mode
            xVelocity.mode = ParticleSystemCurveMode.TwoConstants;
            yVelocity.mode = ParticleSystemCurveMode.TwoConstants;
            
            // Set the velocity components
            bubbleVelocity.x = xVelocity;
            bubbleVelocity.y = yVelocity;
            bubbleVelocity.z = new ParticleSystem.MinMaxCurve(0, 0); // Z velocity is 0 for 2D
            
            // Log emission state change for debugging
            if (wasBoosting != isBoosting)
            {
                Debug.Log($"[DiverMovement] Bubble emission rate set to {(isBoosting ? bubbleEmissionRate : 0)}, direction: {emissionDirection}, speed: {bubbleSpeed}", this);
            }
        }
    }

    // Handle flipping the helmet bubble emitter based on movement direction
    private void UpdateHelmetBubbleEmitterFlip()
    {
        if (helmetBubbleEmitterTransform != null)
        {
            // Determine flip state using same method as other systems
            bool isFlipped = false;
            
            // First try to determine flip state from the sprite renderer
            if (playerSprite != null)
            {
                isFlipped = playerSprite.flipX;
            }
            // Fallback to ArmAim script if sprite reference is missing or not working
            else if (armAimScript != null)
            {
                isFlipped = !armAimScript.IsFacingRight;
            }
            
            // Convert to facing direction (true = right, false = left)
            bool isFacingRight = !isFlipped;
            
            // Get the HelmetBubbleEmitter component instead of changing the transform directly
            HelmetBubbleEmitter emitter = helmetBubbleEmitterTransform.GetComponent<HelmetBubbleEmitter>();
            if (emitter != null)
            {
                // Tell the emitter which direction we're facing
                emitter.SetFacingDirection(isFacingRight);
            }
            else
            {
                // If no emitter component found, use the old approach of flipping the transform
                // Initialize original scale if not already done (in case reference was assigned after Awake)
                if (!helmetScaleInitialized)
                {
                    originalHelmetBubbleScale = helmetBubbleEmitterTransform.localScale;
                    helmetScaleInitialized = true;
                }
                
                // Calculate target scale with original Y and Z values
                float targetScaleX = isFlipped ? -Mathf.Abs(originalHelmetBubbleScale.x) : Mathf.Abs(originalHelmetBubbleScale.x);
                
                // Only update if the scale needs to change (comparing with actual scale in case it was modified elsewhere)
                if (Mathf.Abs(helmetBubbleEmitterTransform.localScale.x - targetScaleX) > 0.01f)
                {
                    // Set new scale, flipping X while preserving original Y and Z
                    helmetBubbleEmitterTransform.localScale = new Vector3(
                        targetScaleX,
                        originalHelmetBubbleScale.y,
                        originalHelmetBubbleScale.z
                    );
                }
            }
        }
    }

    void FixedUpdate()
    {
        // Don't apply physics until we've spawned
        if (!hasSpawned) return;
        
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
    
    // Public accessor for other scripts to check if the player is boosting
    public bool IsBoosting()
    {
        return isBoosting && hasInput;
    }
    
    // Public accessor for current movement direction
    public Vector2 GetMovementDirection()
    {
        return lastMoveDirection;
    }
}
