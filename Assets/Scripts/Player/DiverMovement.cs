using UnityEngine;
using System.Collections;
using FMODUnity; // Add FMOD namespace

[RequireComponent(typeof(Rigidbody2D))]
public class DiverMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float sinkSpeed = 0.5f; // Speed at which diver sinks when idle
    public float horizontalDrag = 3f; // Extra drag applied to horizontal movement when no input
    
    [Header("FMOD Sound Settings")]
    public EventReference movementSoundEvent; // Updated to use EventReference type
    private bool wasMovingLastFrame = false;
    
    // References moved to ArmAim.cs
    // [Header("Helmet Bubble Emitter")]
    // [SerializeField] private Transform helmetBubbleEmitterTransform;
    [SerializeField] private SpriteRenderer playerSprite; // Reference to the player's sprite renderer (Still needed here? ArmAim has it too)
    [SerializeField] private ArmAim armAimScript; // Reference to the ArmAim script (Still needed here? ArmAim has it too)
    
    [Header("Boost Settings")]
    // public float boostMultiplier = 2.0f; // Removed - Replaced by boostSpeed
    [SerializeField] private float boostSpeed = 8f; // Dedicated speed when boosting
    public KeyCode boostKey = KeyCode.Space; // Key to activate boost
    [SerializeField] private float boostRepulsionRadius = 1.5f; // Area to check for enemies when boosting
    [SerializeField] private float boostRepulsionImpulse = 10f; // Impulse force applied to enemies when boosting near them
    [SerializeField] private LayerMask enemyLayerMask; // Layer mask to specify enemy colliders
    
    // References moved to ArmAim.cs
    // [Header("Child Object References")]
    // [SerializeField] private Transform bloodSplatTransform;
    // [SerializeField] private float bloodSplatOffsetX = 0.5f;
    // [SerializeField] private Transform faceMaskLightPivotTransform;
    // private float faceMaskLightOriginalOffsetX = 0f;
    // private Quaternion faceMaskLightOriginalRotation;
    
    [Header("Bubble Effect Settings")] // Keep bubble settings
    [SerializeField] private ParticleSystem boostBubbles;
    [SerializeField] private float bubbleEmissionRate = 20f;
    [SerializeField] private float bubbleBaseSpeed = 2f;
    [SerializeField] private float bubbleSpeedVariation = 0.5f;
    [SerializeField] private float bubbleSizeMin = 0.5f;
    [SerializeField] private float bubbleSizeMax = 1.5f;
    [SerializeField] private float directionalInfluence = 0.8f;
    [SerializeField] private bool usePlayerVelocityForBubbleSpeed = true;
    
    [Header("Spawn Settings")]
    public string spawnPointName = "PlayerSpawn"; // Name of the spawn point GameObject
    public bool forceZPositionToZero = true; // Force Z position to 0 for 2D visibility
    public bool waitForNextFrame = true; // Wait for next frame before spawning (helps with prefabs)
    
    [Header("Treading Water Settings")] // New Header
    [SerializeField] private bool enableTreadingWater = true; // Toggle the treading mechanic
    [SerializeField] private float treadDuration = 5.0f; // How long the player treads water after pressing W
    [SerializeField] private float treadCooldown = 1.0f; // Cooldown before W can be used to tread again
    // [SerializeField] private float treadWaterCounterForce = 1.0f; // Removed - No longer needed
    
    // --- Boost Disable Effects fields REMOVED - Moved to PlayerStateMachine ---
    // [Header("Boost Disable Effects")] 
    // [SerializeField] private ParticleSystem boostDisableSpurtParticles; 
    // [SerializeField] private float boostDisableEffectCooldown = 2.0f; 
    // [SerializeField] private EventReference boostDisableSoundEvent; 
    // [SerializeField] private string boostDisableAnimationTrigger = "BoostMalfunction";
    
    // --- State Machine Control Flags ---
    // Moved to PlayerStateMachine
    // public bool IsBoostAllowed { get; private set; } = true; // Controlled externally

    // Input values (could be read here or passed from states)
    // Moved to PlayerStateMachine
    // public Vector2 MoveInput { get; private set; } // Example
    // Add other input properties as needed

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private bool hasInput;
    private bool isBoosting;
    // private float currentSpeedMultiplier = 1.0f; // Removed - Using direct moveSpeed/boostSpeed now
    private bool hasSpawned = false;
    private bool initializationComplete = false;
    
    // Treading water state - Make public set for state machine access
    public bool isTreading { get; set; } = false; 
    private float treadTimer = 0f;

    // Treading Cooldown State
    private bool isOnTreadCooldown = false;
    private float treadCooldownTimer = 0f;
    
    // Previous frame input state
    private Vector2 lastMoveInput;
    
    // Particle system modules
    private ParticleSystem.EmissionModule bubbleEmission;
    private ParticleSystem.MainModule bubbleMain;
    private ParticleSystem.VelocityOverLifetimeModule bubbleVelocity;
    private ParticleSystem.ShapeModule bubbleShape;
    private bool bubbleSystemInitialized = false;
    
    // Last movement direction for particle emission
    private Vector2 lastMoveDirection = Vector2.up;
    // Removed helmet scale fields
    // private Vector3 originalHelmetBubbleScale;
    // private bool helmetScaleInitialized = false;
    
    // Add this field to track knockback state
    private bool isBeingKnockedBack = false;
    private float knockbackEndTime = 0f;

    private PlayerOxygen playerOxygen;
    private PlayerStateMachine stateMachine; // Add reference

    // Public property for state machine control
    public bool ForceBoost { get; set; } = false;

    // Public getter for boost key
    public KeyCode GetBoostKey() => boostKey;

    // --- State Machine Control Flags ---
    // Deleted duplicate lines below

    void Awake()
    {
        Debug.Log($"[DiverMovement] Awake called on {gameObject.name}", this);
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError($"[DiverMovement] Rigidbody2D component not found on {gameObject.name}!", this);
        }
        
        // Explicitly initialize treading state
        isTreading = false;
        isOnTreadCooldown = false;
        treadTimer = 0f;
        treadCooldownTimer = 0f;
        lastMoveInput = Vector2.zero; // Also initialize last input
        
        // Initialize particle system modules
        InitializeParticleSystem();
        
        // Removed helmet bubble emitter scale init
        
        // Ensure time is running (sometimes gets stuck at 0)
        if (Time.timeScale == 0)
        {
            Debug.LogWarning("[DiverMovement] TimeScale was 0, resetting to 1.0", this);
            Time.timeScale = 1.0f;
        }

        // Removed face mask light pivot init

        playerOxygen = GetComponent<PlayerOxygen>();
        stateMachine = GetComponent<PlayerStateMachine>(); // Get reference
        if (stateMachine == null)
        {
             Debug.LogWarning("[DiverMovement] PlayerStateMachine component not found! Boost disabling might not work correctly.", this);
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
        
        // Check for teleport override
        string effectiveSpawnPointName = spawnPointName;
        if (!string.IsNullOrEmpty(SceneTransitionData.nextSpawnPointName))
        {
            var overrideSpawn = GameObject.Find(SceneTransitionData.nextSpawnPointName);
            if (overrideSpawn != null)
            {
                Debug.Log($"[DiverMovement] Using teleport override spawn point: {SceneTransitionData.nextSpawnPointName}", this);
                effectiveSpawnPointName = SceneTransitionData.nextSpawnPointName;
            }
            else
            {
                Debug.LogWarning($"[DiverMovement] Teleport override spawn point '{SceneTransitionData.nextSpawnPointName}' not found, falling back to default.", this);
            }
            SceneTransitionData.nextSpawnPointName = null;
        }
        
        // Find the spawn point
        GameObject spawnPoint = GameObject.Find(effectiveSpawnPointName);
        
        // Log detailed information about spawn point
        if (spawnPoint != null)
        {
            Debug.Log($"[DiverMovement] Found spawn point '{effectiveSpawnPointName}' at position: {spawnPoint.transform.position}", this);
            
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
            Debug.LogError($"[DiverMovement] Spawn point '{effectiveSpawnPointName}' NOT FOUND in scene! Player will stay at current position: {transform.position}", this);
            
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
        // Don't process input if being knocked back or initialization isn't complete
        if (isBeingKnockedBack || !initializationComplete)
        {
            moveInput = Vector2.zero;
            hasInput = false;
            isBoosting = false; // Ensure boost stops during knockback
            return;
        }

        // Read movement input
        moveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        hasInput = moveInput.magnitude > 0.1f; // Keep general hasInput check sensitivity

        // Check for boost input
        bool boostAllowed = stateMachine == null || stateMachine.IsBoostAllowed;
        isBoosting = boostAllowed && (ForceBoost || Input.GetKey(boostKey));

        // --- DEBUG INPUT --- 
        Debug.Log($"[Input Debug] Current moveInput.y: {moveInput.y:F4}, Last moveInput.y: {lastMoveInput.y:F4}");

        // Treading water logic - Use a slightly larger threshold for input detection
        bool upwardInputStarted = moveInput.y > 0.2f && lastMoveInput.y <= 0.2f; 
        if (enableTreadingWater && upwardInputStarted && !isBoosting && !isTreading && !isOnTreadCooldown)
        {
            isTreading = true;
            treadTimer = treadDuration;
            // Use LogWarning for visibility
            Debug.LogWarning($"!!! Update: Set isTreading = true. Timer started."); 
        }

        // Update particle system based on boosting state changes
        bool wasBoostingLast = isBoosting; // Capture current boost state before potential update
        UpdateBubbleParticleSystem(wasBoostingLast);

        // Sound logic remains the same
        bool isCurrentlyMoving = rb != null && rb.velocity.magnitude > 0.1f;
        if (isCurrentlyMoving && !wasMovingLastFrame)
        {
            if (!movementSoundEvent.IsNull)
            {
                 RuntimeManager.PlayOneShotAttached(movementSoundEvent, gameObject);
            }
        }
        wasMovingLastFrame = isCurrentlyMoving;

        // Update lastMoveDirection if there is significant input
        if (moveInput.sqrMagnitude > 0.01f)
        {
            lastMoveDirection = moveInput.normalized;
        }

        // Store current input for next frame comparison
        lastMoveInput = moveInput;
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
            Vector2 emissionDirection;
            if (rb.velocity.magnitude > 0.1f)
            {
                emissionDirection = -rb.velocity.normalized;
            }
            else if (lastMoveDirection.sqrMagnitude > 0.01f) // Use last known movement if not moving now
            {
                emissionDirection = -lastMoveDirection;
            }
            else // If no velocity and no last movement, default to straight back relative to player's up
            {
                emissionDirection = -transform.up; // Default straight back from player's perspective
            }
            
            // Update shape rotation to emit in the calculated world direction
            float angle = Mathf.Atan2(emissionDirection.y, emissionDirection.x) * Mathf.Rad2Deg;

            // Log the calculated direction and angle (Removed facing check, just log world angle)
            Debug.Log($"[Bubble Flip Debug] Angle: {angle:F1}, EmissionDir: {emissionDirection}"); 

            // Ensure the shape rotation aligns correctly in world space
            bubbleShape.rotation = new Vector3(0, 0, angle); 

            // Calculate particle velocity source speed
            float sourceSpeed = usePlayerVelocityForBubbleSpeed ? 
                rb.velocity.magnitude : (isBoosting ? boostSpeed : moveSpeed);
            
            float bubbleSpeed = bubbleBaseSpeed + (sourceSpeed * directionalInfluence);
            
            // Set the velocity module using a ParticleSystem.MinMaxCurve with the same mode for all axes
            
            // Create a constant velocity in the emission direction with some variation
            bubbleVelocity.enabled = true;
            
            // Adjust emission direction for velocity based on facing direction (because parent scale is flipped)
            Vector2 velocityEmissionDirection = emissionDirection;
            if (armAimScript != null && !armAimScript.IsFacingRight) // If facing LEFT (scale is flipped)
            {
                velocityEmissionDirection.x *= -1f; // Invert X component for local velocity
            }

            // Use a constant with multiplier to ensure same mode across all axes
            ParticleSystem.MinMaxCurve xVelocity = new ParticleSystem.MinMaxCurve(
                velocityEmissionDirection.x * bubbleSpeed,
                velocityEmissionDirection.x * bubbleSpeed * (1 + bubbleSpeedVariation));
            
            ParticleSystem.MinMaxCurve yVelocity = new ParticleSystem.MinMaxCurve(
                velocityEmissionDirection.y * bubbleSpeed, // Y component doesn't need inversion
                velocityEmissionDirection.y * bubbleSpeed * (1 + bubbleSpeedVariation));
                
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
                 Debug.Log($"[DiverMovement] Bubble emission rate set to {(isBoosting ? bubbleEmissionRate : 0)}, direction: {velocityEmissionDirection}, speed: {bubbleSpeed}", this);
            }
        }
    }

    // Handle flipping the helmet bubble emitter and face mask light based on movement direction
    // *** ENTIRE FUNCTION REMOVED as logic moved to ArmAim.cs ***
    /*
    private void UpdateHelmetBubbleEmitterFlip()
    {
        // --- DEBUG LOG --- 
        Debug.Log("[DiverMovement Debug] Entering UpdateHelmetBubbleEmitterFlip");

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

        // --- DEBUG LOG --- 
        Debug.Log($"[DiverMovement Debug] UpdateFlip: isFacingRight = {isFacingRight} (Sprite.flipX = {playerSprite?.flipX})");

        // --- Flip Blood Spatter Child Position AND Scale --- 
        if (bloodSplatTransform != null)
        {
            Vector3 currentSplatPos = bloodSplatTransform.localPosition;
            Vector3 currentSplatScale = bloodSplatTransform.localScale;

            // Determine target position and scale based on facing direction
            float targetPosX = isFacingRight ? bloodSplatOffsetX : -bloodSplatOffsetX;
            float targetScaleX = isFacingRight ? Mathf.Abs(currentSplatScale.x) : -Mathf.Abs(currentSplatScale.x);

            // Apply position if changed
            if (Mathf.Abs(currentSplatPos.x - targetPosX) > 0.01f)
            {
                bloodSplatTransform.localPosition = new Vector3(targetPosX, currentSplatPos.y, currentSplatPos.z);
            }
            // Apply scale if changed
            if (Mathf.Abs(currentSplatScale.x - targetScaleX) > 0.01f)
            {
                bloodSplatTransform.localScale = new Vector3(targetScaleX, currentSplatScale.y, currentSplatScale.z);
            }
        }
        // --- End Flip Blood Spatter --- 

        // --- Flip Face Mask Light Pivot --- 
        if (faceMaskLightPivotTransform != null)
        {
            // Mirroring FlashlightController logic:
            if (isFacingRight) // Should face right (original state)
            {
                // Restore original position and rotation if necessary
                if (Mathf.Abs(faceMaskLightPivotTransform.localPosition.x - faceMaskLightOriginalOffsetX) > 0.01f || 
                    Quaternion.Angle(faceMaskLightPivotTransform.localRotation, faceMaskLightOriginalRotation) > 0.1f)
                {
                    Debug.Log("[DiverMovement Debug] Restoring Light Pivot to RIGHT facing state.");
                    faceMaskLightPivotTransform.localPosition = new Vector3(faceMaskLightOriginalOffsetX, faceMaskLightPivotTransform.localPosition.y, faceMaskLightPivotTransform.localPosition.z);
                    faceMaskLightPivotTransform.localRotation = faceMaskLightOriginalRotation;
                }
            }
            else // Should face left (flipped state)
            {
                float targetPosX = -faceMaskLightOriginalOffsetX;
                Quaternion targetRot = Quaternion.Euler(
                    faceMaskLightOriginalRotation.eulerAngles.x,
                    faceMaskLightOriginalRotation.eulerAngles.y + 180f,
                    faceMaskLightOriginalRotation.eulerAngles.z
                );

                // Apply flipped position and rotation if necessary
                if (Mathf.Abs(faceMaskLightPivotTransform.localPosition.x - targetPosX) > 0.01f ||
                    Quaternion.Angle(faceMaskLightPivotTransform.localRotation, targetRot) > 0.1f)
                {
                    Debug.Log("[DiverMovement Debug] Flipping Light Pivot to LEFT facing state.");
                    faceMaskLightPivotTransform.localPosition = new Vector3(targetPosX, faceMaskLightPivotTransform.localPosition.y, faceMaskLightPivotTransform.localPosition.z);
                    faceMaskLightPivotTransform.localRotation = targetRot;
                }
            }
        }
        // --- End Flip Face Mask Light --- 

        // Existing Helmet Bubble Emitter Logic
        if (helmetBubbleEmitterTransform != null)
        {
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
    */

    void FixedUpdate()
    {
        // Wait until initialization is complete
        if (!initializationComplete)
        {
            return;
        }

        // Skip movement logic if being knocked back
        if (isBeingKnockedBack)
        {
            // Apply sinking force even during knockback
            rb.AddForce(Vector2.down * sinkSpeed, ForceMode2D.Force);
            return;
        }

        // --- Determine Current Speed --- 
        // We no longer use a multiplier, just decide which speed to use based on state
        float currentAppliedSpeed = isBoosting ? boostSpeed : moveSpeed;

        // Check if boosting is possible (for applying force calculation)
        bool canBoost = playerOxygen != null && playerOxygen.GetCurrentOxygen() > 0f;
        if (playerOxygen == null && isBoosting) {
             Debug.LogWarning("[DiverMovement] Trying to boost but PlayerOxygen reference is null!", this);
        }

        // --- Handle Tread Timer and Cooldown Timer ---
        if (isTreading)
        {
            treadTimer -= Time.fixedDeltaTime;
            if (treadTimer <= 0f)
            {
                isTreading = false;
                isOnTreadCooldown = true;
                treadCooldownTimer = treadCooldown;
                // Add LogWarning here
                Debug.LogWarning($"!!! FixedUpdate: Set isTreading = false. Cooldown started."); 
            }
        }
        else if (isOnTreadCooldown) // Only decrement cooldown if not actively treading
        {
            treadCooldownTimer -= Time.fixedDeltaTime;
            if (treadCooldownTimer <= 0f)
            {
                isOnTreadCooldown = false;
                 // Debug.Log("[DiverMovement] Treading Cooldown Ended.");
            }
        }

        // --- Calculate Base Movement Force --- 
        Vector2 moveForce = Vector2.zero;
        bool applyInputBasedForce = moveInput.magnitude > 0.1f;
        bool isEffectivelyBoosting = isBoosting && canBoost; 
        bool isBoostingWithNoDirectionalInput = isEffectivelyBoosting && !applyInputBasedForce;

        if (isBoostingWithNoDirectionalInput) // Use Force for default upward boost
        {
            moveForce = Vector2.up * boostSpeed;
             // Debug.Log($"[FixedUpdate] Applying Upward Boost Force: {moveForce}");
        }
        else if (applyInputBasedForce) // Apply normal or boosted movement as Force
        {
            Vector2 desiredDirection = moveInput.normalized;
            // Use boostSpeed or moveSpeed based on effective boost status
            float speedToUse = isEffectivelyBoosting ? boostSpeed : moveSpeed;
            moveForce.x = desiredDirection.x * speedToUse;
            if (enableTreadingWater)
            {
                // Allow downward or boosted movement
                if (desiredDirection.y < -0.1f || isEffectivelyBoosting)
                {
                    moveForce.y = desiredDirection.y * speedToUse;
                }
            }
            else
            {
                moveForce.y = desiredDirection.y * speedToUse;
            }
            // Debug.Log($"[FixedUpdate] Applying {(isEffectivelyBoosting ? "Boosted" : "Normal")} Move Force: {moveForce}");
        }

        // --- Apply Horizontal Drag --- 
        if (Mathf.Abs(moveInput.x) < 0.1f) // Apply drag if no horizontal input
        {
            Vector2 horizontalVelocity = new Vector2(rb.velocity.x, 0);
            rb.AddForce(-horizontalVelocity * horizontalDrag, ForceMode2D.Force);
        }

        // --- Apply Calculated Movement Force (Includes Boost Force now) --- 
        if (moveForce != Vector2.zero)
        {
            rb.AddForce(moveForce, ForceMode2D.Force);
        }

        // --- Apply Enemy Repulsion if Boosting --- 
        if (isEffectivelyBoosting)
        {
             ApplyBoostRepulsion();
        }

        // --- Apply Sinking Force ---
        bool shouldSink = true;
        // Update condition check for effective boosting
        if (isTreading || isBoostingWithNoDirectionalInput)
        {
            shouldSink = false;
        }

        // *** DEBUG LOGGING START ***
        string logMessage = $"[FixedUpdate] isTreading: {isTreading} (Timer: {treadTimer:F2}), isCooldown: {isOnTreadCooldown} (Timer: {treadCooldownTimer:F2}), isBoostingNoDir: {isBoostingWithNoDirectionalInput}, shouldSink: {shouldSink}";
        
        if (shouldSink)
        {
            rb.AddForce(Vector2.down * sinkSpeed, ForceMode2D.Force);
            logMessage += $", Applied Sink Force ({sinkSpeed}). Vel Y: {rb.velocity.y:F2}";
        }
        else
        {
            logMessage += $", Sinking Prevented. Vel Y: {rb.velocity.y:F2}";
        }
        Debug.Log(logMessage);
        // *** DEBUG LOGGING END ***

        // Limit overall velocity (optional)
        // ...
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

    // Method to start a knockback effect from another script
    public void SetKnockbackState(bool state, float duration = 0.5f)
    {
        isBeingKnockedBack = state;
        
        if (state)
        {
            // Set when knockback should end
            knockbackEndTime = Time.time + duration;
            Debug.Log($"DiverMovement: Knockback started for {duration} seconds");
        }
    }
    
    // Public accessor to check if player is being knocked back
    public bool IsBeingKnockedBack()
    {
        // Automatically end knockback if time has elapsed
        if (isBeingKnockedBack && Time.time > knockbackEndTime)
        {
            isBeingKnockedBack = false;
        }
        
        return isBeingKnockedBack;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        // Check if we're colliding with an enemy
        if (collision.gameObject.CompareTag("BadFish"))
        {
            // Calculate push direction away from enemy
            Vector2 pushDirection = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
            
            // Enhance the vertical component to help push upward
            if (pushDirection.y < 0.2f)
            {
                pushDirection.y = 0.2f;
                pushDirection = pushDirection.normalized;
            }
            
            // Get the distance to determine force strength
            float distance = Vector2.Distance(transform.position, collision.transform.position);
            
            // Apply immediate force to separate - stronger when closer
            if (rb != null)
            {
                // Only override knockback state if not from a major hit
                bool canApplyForce = !IsBeingKnockedBack() || (Time.time > knockbackEndTime - 0.3f);
                
                if (canApplyForce)
                {
                    // Use stronger force (2.5f instead of 1.5f) and scale it based on proximity
                    float forceMagnitude = 2.5f * (1.0f + (1.0f - Mathf.Clamp01(distance)));
                    rb.AddForce(pushDirection * forceMagnitude, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void ApplyBoostRepulsion()
    {
        // Find enemies within the repulsion radius
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(rb.position, boostRepulsionRadius, enemyLayerMask);
        
        if (hitColliders.Length > 0)
        {
             // Debug.Log($"[BoostRepulsion] Found {hitColliders.Length} enemies in boost radius.");
        }

        foreach (var hitCollider in hitColliders)
        {
            // Ensure we hit an enemy with a Rigidbody2D
            Rigidbody2D enemyRb = hitCollider.GetComponent<Rigidbody2D>();
            if (enemyRb != null && enemyRb != rb) // Don't push self
            {
                // Calculate direction away from the player
                Vector2 directionToEnemy = (enemyRb.position - rb.position).normalized;
                
                // Ensure direction is valid (avoid zero vector if perfectly overlapped)
                if (directionToEnemy == Vector2.zero)
                { 
                    directionToEnemy = Random.insideUnitCircle.normalized; // Push in random direction
                    if (directionToEnemy == Vector2.zero) directionToEnemy = Vector2.up; // Ultimate fallback
                }
                
                // Apply impulse to the enemy
                enemyRb.AddForce(directionToEnemy * boostRepulsionImpulse, ForceMode2D.Impulse);
                 Debug.Log($"[BoostRepulsion] Applied impulse {directionToEnemy * boostRepulsionImpulse} to {hitCollider.name}");
            }
        }
    }
}
