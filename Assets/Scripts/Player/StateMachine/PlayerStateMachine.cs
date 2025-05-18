using UnityEngine;
using FMODUnity; // Add FMOD using directive
using System.Collections; // Required for IEnumerator

public class PlayerStateMachine : MonoBehaviour
{
    private IState currentState;
    public IState CurrentState => currentState; // Public getter for the current state

    // References to other player components (Assign in Inspector)
    public DiverMovement DiverMovement { get; private set; }
    public DiverShooter DiverShooter { get; private set; }
    public PlayerOxygen PlayerOxygen { get; private set; }
    public ArmAim ArmAim { get; private set; }
    public FlashlightController FlashlightController { get; private set; }
    public PlayerHealth PlayerHealth { get; private set; } // Assuming you have PlayerHealth in Assets/Scripts/Health/
    // Add other components as needed (e.g., Animator, Rigidbody2D)
    public Rigidbody2D Rb { get; private set; }
    public Animator Animator { get; private set; }

    [Header("Audio Events")] // Optional header for organization
    [SerializeField] private EventReference boostDisabledSoundEvent; // Add FMOD Event Reference

    [Header("Boost Disable Effects")] // New Header
    [SerializeField] private ParticleSystem boostDisableSpurtParticles; // Assign particle system in Inspector
    [SerializeField] private string boostDisableAnimationTrigger = "BoostMalfunction"; // Animator trigger parameter name
    [SerializeField] private float boostDisableEffectCooldown = 2.0f; // Cooldown for the disabled effect attempt

    // --- State Machine Control Flags ---
    public bool IsBoostAllowed { get; private set; } = true; // Controlled externally
    private float nextBoostDisableEffectTime = 0f; // Timer for boost disable effect cooldown

    // Input values (could be read here or passed from states)
    public Vector2 MoveInput { get; private set; } // Example
    // Add other input properties as needed

    void Awake()
    {
        // Get component references
        DiverMovement = GetComponent<DiverMovement>();
        PlayerOxygen = GetComponent<PlayerOxygen>();
        PlayerHealth = GetComponent<PlayerHealth>(); // Adjust if script name/location differs
        Rb = GetComponent<Rigidbody2D>();

        // Look for these components in children as they might not be on the root Player object
        DiverShooter = GetComponentInChildren<DiverShooter>();
        ArmAim = GetComponentInChildren<ArmAim>();
        FlashlightController = GetComponentInChildren<FlashlightController>();

        // Animator is often on a child object (e.g., "Sprite")
        Animator = GetComponentInChildren<Animator>();

        // Basic validation (Updated to check the components potentially found in children)
        if (!DiverMovement || !PlayerOxygen || !PlayerHealth || !Rb || !DiverShooter || !ArmAim || !FlashlightController)
        {
            Debug.LogError("PlayerStateMachine: Missing one or more required component references (check Player and children)!", this);
        }
        if (!Animator)
        {
            Debug.LogWarning("PlayerStateMachine: Animator not found. Animations may not work.", this);
        }
    }

    void Start()
    {
        // Initialize with a default state (e.g., Idle)
        ChangeState(new PlayerIdleState(this));
    }

    void Update()
    {
        // Update input reading here if applicable
        // MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")); // Example

        // Delegate Update logic to the current state
        currentState?.Update();

        // --- Check for disabled boost attempt ---
        // Use DiverMovement to get the correct keycode
        if (DiverMovement != null && Input.GetKeyDown(DiverMovement.GetBoostKey()) && !IsBoostAllowed && Time.time >= nextBoostDisableEffectTime)
        {
            // Play particle effect if assigned
            if (boostDisableSpurtParticles != null)
            {
                boostDisableSpurtParticles.Play();
                Debug.Log("[PlayerStateMachine] Played boost disable particle effect on key press.", this);
            }
            
            // Play sound effect if assigned (using the existing boostDisabledSoundEvent field)
            if (!boostDisabledSoundEvent.IsNull)
            {
                RuntimeManager.PlayOneShotAttached(boostDisabledSoundEvent, gameObject);
                Debug.Log("[PlayerStateMachine] Played boost disable sound effect on key press.", this);
            }
            
            // Start cooldown
            nextBoostDisableEffectTime = Time.time + boostDisableEffectCooldown;
        }
    }

    public void ChangeState(IState newState)
    {
        // Prevent changing state *from* Death state unless handled explicitly
        if (currentState is PlayerDeathState && !(newState is PlayerIdleState)) // Example allow idle for respawn?
        {
            Debug.Log("Attempted to change state while Dead. Blocked.");
            return;
        }

        // *** ADD LOGGING ***
        string previousStateName = currentState?.GetType().Name ?? "null";
        Debug.LogWarning($"--- PlayerStateMachine.ChangeState --- Preparing to exit state: {previousStateName}");
        
        currentState?.Exit();

        // *** ADD LOGGING ***
        Debug.LogWarning($"--- PlayerStateMachine.ChangeState --- CurrentState Exit() called (or skipped if null). Setting new state: {newState.GetType().Name}");

        currentState = newState;
        // Debug.Log($"Changed state to: {newState.GetType().Name}"); // Keep original log commented/removed if preferred
        currentState.Enter();
    }

    // --- Public methods for other scripts to trigger state changes ---

    public void TriggerDamageState(float knockbackDuration = 0.5f)
    {
        // Don't take damage if already dead
        if (currentState is PlayerDeathState) return;

        // You might add checks here: e.g., don't enter damage state if already in damage state?
        // Or allow re-triggering knockback?
        // if (currentState is PlayerTakeDamageState) return; 

        ChangeState(new PlayerTakeDamageState(this, knockbackDuration));
    }

    public void TriggerDeathState()
    {
        // Don't trigger death again if already dead
        if (currentState is PlayerDeathState) return;

        ChangeState(new PlayerDeathState(this));
    }

    // Called externally (e.g., by CameraPan or CameraCenter) to enable/disable boosting
    public void SetBoostAllowed(bool allowed)
    {
        if (IsBoostAllowed == allowed) return; // No change

        IsBoostAllowed = allowed;
        Debug.Log($"PlayerStateMachine: Boost Allowed set to {allowed}");

        // Play glitch sound when boost is DISABLED
        if (!allowed && !boostDisabledSoundEvent.IsNull)
        {
            RuntimeManager.PlayOneShotAttached(boostDisabledSoundEvent, gameObject);
            Debug.Log("Played boost disable sound.");
        }

        // Trigger particle spurt effect
        if (!allowed && boostDisableSpurtParticles != null)
        {
            boostDisableSpurtParticles.Play();
            Debug.Log("Triggered boost disable particle spurt.");
        }

        // Trigger animation
        if (!allowed && Animator != null && !string.IsNullOrEmpty(boostDisableAnimationTrigger))
        {
            Animator.SetTrigger(boostDisableAnimationTrigger);
            Debug.Log($"Triggered boost disable animation: {boostDisableAnimationTrigger}");
        }

        // If disabling boost while currently boosting, force exit from boost state
        if (!allowed && currentState is PlayerBoostState)
        {
            Debug.Log("Boost disabled while boosting. Exiting BoostState.");
            // Transition back to Idle or Swim based on movement? For simplicity, go to Idle.
            ChangeState(new PlayerIdleState(this)); 
            // Alternative: Could check Rb velocity or last input to decide Idle vs Swim
        }
    }

    // Called externally (e.g., by CameraPan) to immobilize the player
    public void EnterCutsceneState()
    {
        // Don't enter cutscene if dead
        if (currentState is PlayerDeathState) return;

        // If already in cutscene state, do nothing
        if (currentState is PlayerCutsceneState) return;

        Debug.Log("PlayerStateMachine: Entering Cutscene State via external call.");
        ChangeState(new PlayerCutsceneState(this));
    }

     // Optionally, add a method to explicitly exit cutscene state, 
     // though often the caller (CameraPan) just transitions back to Idle.
    // public void ExitCutsceneState()
    // {
    //     if (currentState is PlayerCutsceneState)
    //     {
    //         ChangeState(new PlayerIdleState(this)); // Default back to Idle
    //     }
    // }

    // Helper for states to run coroutines
    public Coroutine StartCoroutineFromState(IEnumerator coroutine)
    {
        return StartCoroutine(coroutine);
    }
}
