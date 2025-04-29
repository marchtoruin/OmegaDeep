using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using FMODUnity; // <-- Add FMOD namespace

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(badFishHealth))] // Ensure health component exists
public class BadFishAI : MonoBehaviour
{
    #region Inspector Settings

    [Header("General Settings")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] public bool artworkFacesRight = true; // Sprite orientation

    [Header("Detection & Aggro")]
    [SerializeField] private float playerDetectionRange = 6f;
    [SerializeField] private float aggroRange = 10f; // Max distance to maintain aggro
    [SerializeField] private float maxAggroTime = 8f; // How long aggro lasts without seeing player
    [SerializeField] private LayerMask obstacleLayer; // Layers fish sees as obstacles

    [Header("Movement")]
    [SerializeField] private float basePatrolSpeed = 2f;
    [SerializeField] private float baseChaseSpeed = 4f;
    [SerializeField] private float rotationSpeed = 5f; // How quickly the fish turns
    [SerializeField] private float minDistanceToPlayer = 1.5f; // Minimum distance during chase

    [Header("Patrolling")]
    [SerializeField] private bool useRandomPatrolPoints = true;
    [SerializeField] private float patrolRadius = 5f; // Used if useRandomPatrolPoints is true
    [SerializeField] private int maxRandomPatrolPoints = 4; // Used if useRandomPatrolPoints is true
    [SerializeField] private Vector2[] explicitPatrolPoints; // Used if useRandomPatrolPoints is false
    [SerializeField] private float minDistanceToWaypoint = 0.5f;
    [SerializeField] private float patrolPointWaitTime = 1.5f;

    [Header("Boss Settings")]
    [SerializeField] public bool isBoss = false; // Activate Boss behaviors
    [SerializeField] private float bossHealthMultiplier = 3f;
    [SerializeField] private float bossSpeedMultiplier = 1.5f;
    [SerializeField] private float bossScaleMultiplier = 2f;

    [Header("Boss Charge Attack")]
    [SerializeField] private float chargeWindupTime = 0.75f; // Visible delay before charge
    [SerializeField] private float chargeSpeed = 10f; // Speed during charge dash
    [SerializeField] private float chargeDuration = 0.6f; // How long the charge dash lasts
    [SerializeField] private float chargeDamageMultiplier = 2f; // Damage increase during charge
    [SerializeField] private float postChargeWanderDuration = 0.75f; // How long to wander after charge
    [SerializeField] private float postChargeWanderSpeed = 1.5f; // Speed during post-charge wander
    [SerializeField] private EventReference bossChargeSound; // FMOD Event for charge dash start <-- Add FMOD EventReference field

    [Header("Cooldowns (Boss Only)")]
    [SerializeField] private float aggroToChargeDelay = 2f; // Delay after becoming aggro before first charge
    [SerializeField] private float chargeCooldown = 5f; // Delay between charges

    #endregion

    #region State

    private enum FishState { Patrol, Chase, ChargingWindup, ChargingDash, PostChargeWander, Stunned }
    private FishState currentState = FishState.Patrol;
    private bool facingLocked = false; // Flag to prevent flipping during certain actions

    // Timers & Flags
    private float stateTimer = 0f; // Generic timer for states like Patrol Wait or Stun
    private float aggroTimer = 0f; // Tracks how long the fish remains aggro
    private bool isAggro = false;
    private float chargeStateTimer = 0f; // Tracks windup and dash duration
    private bool isChargeOnCooldown = false;
    private bool canChargeAfterAggro = false; // Tracks the aggro-to-charge delay

    // Movement & Targeting
    private Vector2 startPosition;
    private Vector2 currentTarget;
    private int currentPatrolIndex = 0;
    private List<Vector2> generatedPatrolPoints = new List<Vector2>();
    private Vector2 chargeDirection;
    private bool isFacingRight = true;
    private float currentPatrolSpeed;
    private float currentChaseSpeed;

    // Coroutine References
    private Coroutine chargeCooldownCoroutineRef = null;
    private Coroutine aggroToChargeDelayCoroutineRef = null;

    #endregion

    #region Components

    private Rigidbody2D rb;
    private badFishHealth healthComponent;
    private SpriteRenderer spriteRenderer;
    private GameObject player;

    #endregion

    #region Public Properties

    // Used by external scripts (like a damage script) to check charge status
    public bool IsCurrentlyCharging => currentState == FishState.ChargingDash;
    public float ChargeDamageMultiplier => isBoss ? chargeDamageMultiplier : 1f;

    #endregion

    #region Unity Lifecycle Methods

    void Awake()
    {
        // Get Components
        rb = GetComponent<Rigidbody2D>();
        healthComponent = GetComponent<badFishHealth>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>(); // Assumes sprite is on child

        if (rb == null) Debug.LogError($"BadFishAI on {gameObject.name}: Rigidbody2D component not found!", this);
        if (healthComponent == null) Debug.LogError($"BadFishAI on {gameObject.name}: badFishHealth component not found!", this);
        if (spriteRenderer == null) Debug.LogWarning($"BadFishAI on {gameObject.name}: SpriteRenderer component not found on children.", this);

        // Apply Boss Modifiers
        ApplyBossModifiers();

        // Set initial facing direction based on artwork
        isFacingRight = artworkFacesRight;
        if (spriteRenderer != null) spriteRenderer.flipX = !artworkFacesRight; // Initial flip if needed
    }

    void Start()
    {
        // Find Player
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) Debug.LogError($"BadFishAI on {gameObject.name}: Player GameObject not found! Ensure player has 'Player' tag.", this);

        // Setup Patrolling
        startPosition = transform.position;
        GeneratePatrolPoints();
        SetNextPatrolTarget();

        // Initialize State
        currentState = FishState.Patrol;
        isChargeOnCooldown = true; // Start with charge on cooldown for bosses
        canChargeAfterAggro = false;

        // Start initial cooldown if boss (moved out of removed block)
        if (isBoss)
        {
             StartCoroutine(InitialChargeCooldown());
        }
    }

    void Update()
    {
        if (player == null) return; // Don't do anything if player is missing

        ProcessAggro();
        StateMachineUpdate();
    }

    void FixedUpdate()
    {
        // Apply movement based on velocity set in Update methods
        // (Rigidbody velocity is set directly in state updates)
    }

    #endregion

    #region Initialization & Setup

    private void ApplyBossModifiers()
    {
        // Store base speeds before potential modification
        currentPatrolSpeed = basePatrolSpeed;
        currentChaseSpeed = baseChaseSpeed;

        if (isBoss)
        {
            // Apply Health Multiplier (Requires method in badFishHealth)
            if (healthComponent != null)
            {
                // Assuming badFishHealth has a method like this:
                // public void ApplyHealthMultiplier(float multiplier) { maxHealth = Mathf.CeilToInt(maxHealth * multiplier); currentHealth = maxHealth; }
                 healthComponent.ApplyHealthMultiplier(bossHealthMultiplier);
                Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Applied health multiplier ({bossHealthMultiplier}x)", this);
            }

            // Apply Speed Multiplier
            currentPatrolSpeed *= bossSpeedMultiplier;
            currentChaseSpeed *= bossSpeedMultiplier;
            Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Applied speed multiplier ({bossSpeedMultiplier}x). Chase Speed: {currentChaseSpeed}", this);

            // Apply Scale Multiplier
            transform.localScale *= bossScaleMultiplier;
            Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Applied scale multiplier ({bossScaleMultiplier}x)", this);
        }
    }

    private void GeneratePatrolPoints()
    {
        generatedPatrolPoints.Clear();
        if (useRandomPatrolPoints)
        {
            generatedPatrolPoints.Add(startPosition); // Include start position
            for (int i = 0; i < maxRandomPatrolPoints -1; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                float distance = Random.Range(patrolRadius * 0.3f, patrolRadius);
                Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
                generatedPatrolPoints.Add(startPosition + offset);
            }
            Debug.Log($"[BadFishAI] {gameObject.name}: Generated {generatedPatrolPoints.Count} random patrol points around {startPosition} within radius {patrolRadius}.", this);
        }
        else
        {
            if (explicitPatrolPoints == null || explicitPatrolPoints.Length == 0)
            {
                Debug.LogWarning($"[BadFishAI] {gameObject.name}: useRandomPatrolPoints is false, but no explicitPatrolPoints are set. Defaulting to start position.", this);
                generatedPatrolPoints.Add(startPosition);
            }
            else
            {
                foreach (Vector2 point in explicitPatrolPoints)
                {
                    generatedPatrolPoints.Add(point);
                }
                 Debug.Log($"[BadFishAI] {gameObject.name}: Using {generatedPatrolPoints.Count} explicit patrol points.", this);
            }
        }
    }

    private void SetNextPatrolTarget()
    {
        if (generatedPatrolPoints.Count == 0)
        {
            currentTarget = startPosition; // Fallback
            return;
        }
        currentPatrolIndex = (currentPatrolIndex + 1) % generatedPatrolPoints.Count;
        currentTarget = generatedPatrolPoints[currentPatrolIndex];
    }

    // Coroutine for the initial charge cooldown when the boss spawns
    private IEnumerator InitialChargeCooldown()
    {
         // This ensures the boss doesn't charge *immediately* upon spawning
        yield return new WaitForSeconds(chargeCooldown * 0.5f); // Start with half cooldown
        isChargeOnCooldown = false;
        Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Initial charge cooldown finished.", this);
    }


    #endregion

    #region State Machine

    private void StateMachineUpdate()
    {
        switch (currentState)
        {
            case FishState.Patrol:
                UpdatePatrolState();
                break;
            case FishState.Chase:
                UpdateChaseState();
                break;
            case FishState.ChargingWindup:
                UpdateChargingWindupState();
                break;
            case FishState.ChargingDash:
                UpdateChargingDashState();
                break;
            case FishState.PostChargeWander:
                UpdatePostChargeWanderState();
                break;
            case FishState.Stunned:
                UpdateStunnedState();
                break;
        }
         UpdateFacingDirection(); // Update facing based on velocity or target
    }

    private void ChangeState(FishState newState)
    {
        if (currentState == newState) return;

        // Exit logic for old state (optional)
        // OnStateExit(currentState);

        Debug.Log($"[BadFishAI] {gameObject.name}: Changing state from {currentState} to {newState}", this);
        currentState = newState;
        stateTimer = 0f; // Reset generic timer on state change

        // Enter logic for new state
        OnStateEnter(newState);
    }

    private void OnStateEnter(FishState state)
    {
        switch (state)
        {
            case FishState.Patrol:
                isWaitingAtPatrolPoint = false;
                // SetNextPatrolTarget(); // Already handled when returning to patrol
                break;
            case FishState.Chase:
                 if (isBoss && !canChargeAfterAggro && aggroToChargeDelayCoroutineRef == null)
                 {
                     // Start the delay before the first charge is allowed
                     aggroToChargeDelayCoroutineRef = StartCoroutine(AggroToChargeDelayCoroutine());
                 }
                break;
            case FishState.ChargingWindup:
                chargeStateTimer = chargeWindupTime;
                rb.velocity = Vector2.zero; // Stop for windup
                // chargeDirection is now calculated at the END of windup
                facingLocked = true; // Lock facing during windup
                break;
            case FishState.ChargingDash:
                chargeStateTimer = chargeDuration;
                // Velocity is set using the direction calculated just before entering this state
                rb.velocity = chargeDirection * chargeSpeed;
                facingLocked = true; // Lock facing during dash

                // Play charge sound using PlayOneShotAttached <-- CHANGE Playback Logic
                if (isBoss && !bossChargeSound.IsNull)
                {
                     Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Entering ChargingDash state. Playing charge sound via PlayOneShotAttached.", this); // <-- Updated log
                     FMODUnity.RuntimeManager.PlayOneShotAttached(bossChargeSound, gameObject); // <-- Use PlayOneShotAttached
                }
                
                // Maybe play dash animation/sound
                break;
            case FishState.PostChargeWander:
                stateTimer = postChargeWanderDuration; // Use generic timer for wander duration
                rb.velocity = chargeDirection * postChargeWanderSpeed;
                facingLocked = true; // Lock facing during wander
                break;
            case FishState.Stunned:
                rb.velocity = Vector2.zero;
                facingLocked = false;
                // stateTimer should be set by the StunFish() call
                break;
            default: // Reset lock on entering other states like Patrol, Chase
                 facingLocked = false;
                 break;
        }
    }

    // Optional: Add OnStateExit if complex cleanup is needed per state

    #endregion

    #region State Logic

    private void ProcessAggro()
    {
        if (player == null || currentState == FishState.Stunned) return;

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        if (isAggro)
        {
            aggroTimer -= Time.deltaTime;
            // If player is still within aggro range, reset timer
            if (distanceToPlayer <= aggroRange)
            {
                aggroTimer = maxAggroTime;
            }
            else if (aggroTimer <= 0)
            {
                // Aggro timer ran out and player is out of range
                Debug.Log($"[BadFishAI] {gameObject.name}: Lost aggro (timer expired & out of range). Returning to Patrol.", this);
                isAggro = false;
                canChargeAfterAggro = false; // Reset charge permission
                if (aggroToChargeDelayCoroutineRef != null) StopCoroutine(aggroToChargeDelayCoroutineRef); // Stop delay timer
                 if (currentState == FishState.Chase || currentState == FishState.ChargingWindup || currentState == FishState.ChargingDash) // Only return if actively hostile
                 {
                     ChangeState(FishState.Patrol);
                     SetNextPatrolTarget(); // Ensure we have a patrol target
                 }
            }
        }
        else // Not currently aggro
        {
            // Check if player entered detection range
            if (distanceToPlayer <= playerDetectionRange)
            {
                Debug.Log($"[BadFishAI] {gameObject.name}: Player detected within range ({distanceToPlayer:F1}m). Becoming Aggro.", this);
                BecomeAggro();
            }
        }
    }

    private void BecomeAggro()
    {
         isAggro = true;
         aggroTimer = maxAggroTime;
         if (currentState == FishState.Patrol) // Only switch if patrolling
         {
             ChangeState(FishState.Chase);
         }
         // Reset charge readiness for boss
         if (isBoss)
         {
             canChargeAfterAggro = false;
             if (aggroToChargeDelayCoroutineRef != null) StopCoroutine(aggroToChargeDelayCoroutineRef);
             aggroToChargeDelayCoroutineRef = StartCoroutine(AggroToChargeDelayCoroutine());
         }
    }


    private bool isWaitingAtPatrolPoint = false; // Added missing flag

    private void UpdatePatrolState()
    {
        // Check for player detection first
        if (isAggro)
        {
            ChangeState(FishState.Chase);
            return;
        }

        if (isWaitingAtPatrolPoint)
        {
            stateTimer += Time.deltaTime;
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 5f); // Slow down
            if (stateTimer >= patrolPointWaitTime)
            {
                isWaitingAtPatrolPoint = false;
                SetNextPatrolTarget();
            }
            return; // Don't move while waiting
        }

        // Move towards target
        float distanceToTarget = Vector2.Distance(transform.position, currentTarget);
        if (distanceToTarget > minDistanceToWaypoint)
        {
            MoveTowards(currentTarget, currentPatrolSpeed);
        }
        else
        {
            // Reached waypoint
            isWaitingAtPatrolPoint = true;
            stateTimer = 0f; // Reset timer for waiting
        }
    }

    private void UpdateChaseState()
    {
        if (!isAggro)
        {
            // Should have been caught by ProcessAggro, but double-check
            ChangeState(FishState.Patrol);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // --- Boss Charge Logic ---
        if (isBoss && canChargeAfterAggro && !isChargeOnCooldown)
        {
             // Check if within a suitable range to initiate charge (not too close, not too far)
             if (distanceToPlayer > minDistanceToPlayer * 1.5f && distanceToPlayer <= aggroRange * 0.8f)
             {
                 Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Conditions met. Initiating Charge Windup.", this);
                 ChangeState(FishState.ChargingWindup);
                 return; // Don't execute normal chase movement
             }
        }

        // --- Normal Chase Movement ---
        if (distanceToPlayer > minDistanceToPlayer)
        {
            MoveTowards(player.transform.position, currentChaseSpeed);
        }
        else
        {
            // Close enough, stop moving towards player
            rb.velocity = Vector2.Lerp(rb.velocity, Vector2.zero, Time.deltaTime * 5f); // Slow down smoothly
        }
    }

    private void UpdateChargingWindupState()
    {
        chargeStateTimer -= Time.deltaTime;
        if (rb.velocity != Vector2.zero) rb.velocity = Vector2.zero;

        // Continuously update direction towards player during windup
        if (player != null)
        {
            chargeDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
            // Immediately face the *current* target direction
            bool shouldFaceRight = chargeDirection.x > 0;
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
        else // If player disappears during windup
        {
            chargeDirection = transform.right * (isFacingRight ? 1 : -1); // Use current facing
        }
        

        if (chargeStateTimer <= 0)
        {   
            // Final direction is locked just before dash starts
            ChangeState(FishState.ChargingDash);
        }
    }

    private void UpdateChargingDashState()
    {
        chargeStateTimer -= Time.deltaTime;

        // --- Collision Check During Dash ---
        float checkDistance = rb.velocity.magnitude * Time.deltaTime + 0.5f; // Check slightly ahead
        RaycastHit2D hit = Physics2D.Raycast(transform.position, chargeDirection, checkDistance, obstacleLayer);

        if (hit.collider != null)
        {
            Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Charge hit obstacle ({hit.collider.name})! Stopping dash.", this);
            rb.velocity = Vector2.zero; // Stop immediately
            StartChargeCooldown();      // Start cooldown
            ChangeState(FishState.PostChargeWander); // Transition to wander (or Chase/Stunned)
            return; // Exit early
        }
        // --- End Collision Check ---
        
        // Maintain charge velocity
        if (rb.velocity.normalized != chargeDirection) 
        {
             rb.velocity = chargeDirection * chargeSpeed;
        }
       
        if (chargeStateTimer <= 0)
        {
            Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Charge Dash finished normally.", this);
            StartChargeCooldown(); 
            ChangeState(FishState.PostChargeWander); 
        }
    }

    private void UpdatePostChargeWanderState()
    {
        stateTimer -= Time.deltaTime;

        // Maintain wander velocity (already set in OnStateEnter)
        // Optional: Add slight curve or variation here if desired

        if (stateTimer <= 0)
        {
            Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Post-charge wander finished. Returning to Chase.", this);
            rb.velocity = Vector2.zero; // Stop wandering
            ChangeState(FishState.Chase); // Now return to chase
        }
    }

    private void UpdateStunnedState()
    {
        stateTimer -= Time.deltaTime;
        if (stateTimer <= 0)
        {
             // Recover from stun - return to chase if aggro, otherwise patrol
             ChangeState(isAggro ? FishState.Chase : FishState.Patrol);
             if (!isAggro) SetNextPatrolTarget();
        }
    }

    #endregion

    #region Movement & Facing

    private void MoveTowards(Vector2 target, float speed)
    {
        Vector2 direction = (target - (Vector2)transform.position).normalized;
        rb.velocity = Vector2.Lerp(rb.velocity, direction * speed, Time.deltaTime * rotationSpeed); // Smoother velocity change
    }

    private void UpdateFacingDirection()
    {
        // *** Exit if facing is locked ***
        if (facingLocked) return;

        float xVelocity = rb.velocity.x;

        // Determine target direction based on velocity or target if velocity is near zero
        float xDirectionTarget = 0f;
        if (Mathf.Abs(xVelocity) > 0.1f)
        {
             xDirectionTarget = xVelocity;
        }
        else if (currentState == FishState.Patrol && !isWaitingAtPatrolPoint)
        {
             xDirectionTarget = currentTarget.x - transform.position.x;
        }
        else if (currentState == FishState.Chase && player != null)
        {
             xDirectionTarget = player.transform.position.x - transform.position.x;
        }
        else if (currentState == FishState.ChargingWindup || currentState == FishState.ChargingDash)
        {
            xDirectionTarget = chargeDirection.x;
        }


        // Only flip if the target direction is significant
        if (Mathf.Abs(xDirectionTarget) > 0.1f)
        {
            bool shouldFaceRight = xDirectionTarget > 0;
            if (shouldFaceRight != isFacingRight)
            {
                FlipSprite(shouldFaceRight);
            }
        }
    }

    private void FlipSprite(bool faceRight)
    {
        isFacingRight = faceRight;
        if (spriteRenderer != null)
        {
            // Flip based on whether desired facing matches artwork's natural facing
            spriteRenderer.flipX = (faceRight != artworkFacesRight);
        }
        else
        {
            // Fallback: Scale flipping if no sprite renderer (less common for 2D)
            Vector3 scale = transform.localScale;
            scale.x = Mathf.Abs(scale.x) * (faceRight ? 1 : -1) * (artworkFacesRight ? 1 : -1);
            transform.localScale = scale;
        }
    }

    #endregion

    #region Cooldowns (Boss Only)

    // Coroutine for delay between getting aggro and being allowed to charge
    private IEnumerator AggroToChargeDelayCoroutine()
    {
        Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Starting Aggro-to-Charge delay ({aggroToChargeDelay}s).", this);
        canChargeAfterAggro = false;
        yield return new WaitForSeconds(aggroToChargeDelay);
        canChargeAfterAggro = true;
        aggroToChargeDelayCoroutineRef = null; // Mark as finished
        Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Aggro-to-Charge delay finished. Can now charge.", this);
    }

    private void StartChargeCooldown()
    {
        if (!isBoss) return;

        if (chargeCooldownCoroutineRef != null)
        {
            StopCoroutine(chargeCooldownCoroutineRef);
        }
        chargeCooldownCoroutineRef = StartCoroutine(ChargeCooldownCoroutine());
    }

    // Coroutine for delay between charges
    private IEnumerator ChargeCooldownCoroutine()
    {
        isChargeOnCooldown = true;
        Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Starting Charge Cooldown ({chargeCooldown}s).", this);
        yield return new WaitForSeconds(chargeCooldown);
        isChargeOnCooldown = false;
        chargeCooldownCoroutineRef = null; // Mark as finished
        Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Charge Cooldown finished. Ready to charge again.", this);
    }

    #endregion

    #region Public Methods & Event Handlers

    // Call this from other scripts (e.g., projectile collision)
    public void OnAttacked()
    {
        if (!isAggro)
        {
             Debug.Log($"[BadFishAI] {gameObject.name}: Attacked! Becoming Aggro.", this);
             BecomeAggro();
        }
        else
        {
             // Already aggro, refresh timer
             aggroTimer = maxAggroTime;
        }
    }

    // Call this if the fish should be stunned (e.g., by specific attack)
    public void StunFish(float duration)
    {
        Debug.Log($"[BadFishAI] {gameObject.name}: Stunned for {duration} seconds.", this);
        stateTimer = duration; // Set stun duration
        ChangeState(FishState.Stunned);
    }

    // Optional: Call when health changes (e.g., to trigger fleeing at low health)
    public void OnHealthChanged(float currentHealth, float maxHealth)
    {
        // Example: Flee at low health
        // if (currentHealth / maxHealth < 0.2f && currentState != FishState.Flee)
        // {
        //     ChangeState(FishState.Flee);
        // }
    }

     // Reset state, typically called on player respawn
     public void ResetToInitialState()
     {
         Debug.Log($"[BadFishAI] {gameObject.name}: Resetting to initial state.", this);
         StopAllCoroutines(); // Stop cooldowns etc.

         isAggro = false;
         aggroTimer = 0f;
         isChargeOnCooldown = true; // Reset cooldown state
         canChargeAfterAggro = false;
         chargeCooldownCoroutineRef = null;
         aggroToChargeDelayCoroutineRef = null;

         ChangeState(FishState.Patrol);
         SetNextPatrolTarget();

         // Reset velocity
         if (rb != null) rb.velocity = Vector2.zero;

         // Re-start initial cooldown if boss
         if (isBoss)
         {
             StartCoroutine(InitialChargeCooldown());
         }
     }

    #endregion

    #region Collision & Triggers

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Example: If player collides directly, become aggro
        if (collision.gameObject.CompareTag("Player"))
        {
             Debug.Log($"[BadFishAI] {gameObject.name}: Collided directly with Player.", this);
             OnAttacked();
             // Apply collision damage here or in player script
        }

         // If charging and hit player, potentially apply charge damage
         if (IsCurrentlyCharging && collision.gameObject.CompareTag("Player"))
         {
             Debug.Log($"[BadFishAI-BOSS] {gameObject.name}: Hit Player during charge!", this);
             // Damage application logic should check IsCurrentlyCharging and ChargeDamageMultiplier
         }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Example: If hit by player projectile, become aggro
        if (other.CompareTag("PlayerProjectile"))
        {
             Debug.Log($"[BadFishAI] {gameObject.name}: Hit by PlayerProjectile.", this);
             OnAttacked();
        }
    }

    #endregion

    #region Debug Gizmos

    private void OnDrawGizmosSelected()
    {
        if (!showDebugGizmos) return;

        Vector3 pos = transform.position;

        // Detection Range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(pos, playerDetectionRange);

        // Aggro Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(pos, aggroRange);

        // Patrol Radius (only if using random)
        if (useRandomPatrolPoints)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(Application.isPlaying ? startPosition : pos, patrolRadius);
        }

        // Patrol Points
        Gizmos.color = Color.green;
        if (Application.isPlaying)
        {
             if (generatedPatrolPoints.Count > 0)
             {
                 foreach (Vector2 point in generatedPatrolPoints) Gizmos.DrawSphere(point, 0.2f);
                 Gizmos.color = Color.blue; // Current target
                 Gizmos.DrawSphere(currentTarget, 0.3f);
             }
        }
        else // Draw explicit points in editor
        {
             if (!useRandomPatrolPoints && explicitPatrolPoints != null)
             {
                 foreach (Vector2 point in explicitPatrolPoints) Gizmos.DrawSphere(point, 0.2f);
             }
        }
    }

    #endregion
}
