
using UnityEngine;

public class PlayerTakeDamageState : PlayerBaseState
{
    private float knockbackDuration;
    private float stateTimer;
    private bool wasMovementDisabled = false;

    // Constructor can optionally take parameters like knockback duration
    public PlayerTakeDamageState(PlayerStateMachine stateMachine, float duration = 0.5f)
        : base(stateMachine)
    {
        knockbackDuration = duration;
    }

    public override void Enter()
    {
        Debug.Log("Entering Take Damage State");
        stateTimer = knockbackDuration;

        // Disable direct movement control if it's enabled
        if (stateMachine.DiverMovement != null && stateMachine.DiverMovement.enabled)
        {
            stateMachine.DiverMovement.enabled = false;
            wasMovementDisabled = true;
        }
        else
        {
            wasMovementDisabled = false;
        }

        // Trigger knockback (DiverMovement handles the physics)
        stateMachine.DiverMovement?.SetKnockbackState(true, knockbackDuration);

        // Trigger damage animation/sound
        stateMachine.Animator?.SetTrigger("TakeDamage"); // Example trigger
        // Play sound effect

        // Potentially disable shooting/aiming temporarily?
        // stateMachine.ArmAim?.SetActive(false);
        // stateMachine.DiverShooter.enabled = false; // Example
    }

    public override void Update()
    {
        stateTimer -= Time.deltaTime;

        // --- Transition Check ---
        if (stateTimer <= 0f)
        {
            // Knockback finished, decide where to go
            // For simplicity, go back to Idle. A more robust check
            // could see if movement input is held and go to Swim.
            stateMachine.ChangeState(new PlayerIdleState(stateMachine));
            return;
        }

        // --- State Logic ---
        // Player is being knocked back, movement is handled by DiverMovement's knockback logic.
        // We just wait for the timer.
    }

    public override void Exit()
    {
        Debug.Log("Exiting Take Damage State");

        // Ensure knockback state is explicitly turned off
        // Although DiverMovement might handle timeout, it's safer here
        stateMachine.DiverMovement?.SetKnockbackState(false);

        // Re-enable movement if we disabled it
        if (wasMovementDisabled && stateMachine.DiverMovement != null)
        {
            stateMachine.DiverMovement.enabled = true;
        }

        // Re-enable any other components disabled in Enter()
        // stateMachine.ArmAim?.SetActive(true);
        // stateMachine.DiverShooter.enabled = true;
    }
}
