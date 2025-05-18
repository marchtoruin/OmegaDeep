using UnityEngine;

public class PlayerBoostState : PlayerBaseState
{
    private KeyCode boostKey;

    public PlayerBoostState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entering Boost State");
        if (stateMachine.DiverMovement == null) return;

        // Get the boost key from DiverMovement
        boostKey = stateMachine.DiverMovement.GetBoostKey();

        // Ensure DiverMovement is enabled and force boost
        stateMachine.DiverMovement.enabled = true;
        stateMachine.DiverMovement.ForceBoost = true;

        // *** ADDED: Tell PlayerOxygen we are boosting ***
        if (stateMachine.PlayerOxygen != null) stateMachine.PlayerOxygen.SetBoosting(true);

        // Ensure shooting is enabled
        if (stateMachine.DiverShooter != null) stateMachine.DiverShooter.enabled = true;

        // Trigger boost animations/effects if applicable
        // stateMachine.Animator?.SetBool("IsBoosting", true); // Example parameter - COMMENTED OUT
    }

    public override void Update()
    {
        if (stateMachine.DiverMovement == null) return;

        // --- Read Input (primarily for transitions) ---
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        bool hasMovementInput = Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f;
        bool boostKeyReleased = Input.GetKeyUp(boostKey);
        bool noOxygen = stateMachine.PlayerOxygen != null && stateMachine.PlayerOxygen.GetCurrentOxygen() <= 0;

        // --- Transition Checks ---
        // Transition back if boost key is released OR oxygen runs out
        if (boostKeyReleased || noOxygen)
        {
            Debug.LogWarning($"--- Boost State Transition Triggered! boostKeyReleased: {boostKeyReleased}, noOxygen: {noOxygen}, hasMovementInput: {hasMovementInput} ---");

            // Decide whether to go to Swim or Idle based on movement input
            if (hasMovementInput)
            {
                stateMachine.ChangeState(new PlayerSwimState(stateMachine));
            }
            else
            {
                stateMachine.ChangeState(new PlayerIdleState(stateMachine));
            }
            return; // Exit after state change
        }

        // --- State Logic ---
        // DiverMovement handles the actual boosting logic while ForceBoost is true.
        // This state mainly handles the condition to *stop* boosting.
    }

    public override void Exit()
    {
        Debug.LogWarning("--- PlayerBoostState EXITING --- Setting ForceBoost = false");
        if (stateMachine.DiverMovement != null)
        {
            // IMPORTANT: Turn off the boost force when exiting the state
            stateMachine.DiverMovement.ForceBoost = false;
        }
        // *** ADDED: Tell PlayerOxygen we stopped boosting ***
        if (stateMachine.PlayerOxygen != null) stateMachine.PlayerOxygen.SetBoosting(false);
        
        // Reset boost animations/effects
        // stateMachine.Animator?.SetBool("IsBoosting", false); // Example parameter - COMMENTED OUT
    }
}
