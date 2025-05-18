using UnityEngine;

public class PlayerIdleState : PlayerBaseState
{
    public PlayerIdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entering Idle State");
        // Disable movement processing (This might be interfering - Keep DiverMovement enabled)
        // if(stateMachine.DiverMovement != null) stateMachine.DiverMovement.enabled = false;

        // *** ADDED: Force treading state off when entering Idle ***
        if(stateMachine.DiverMovement != null) 
        {
            stateMachine.DiverMovement.isTreading = false;
            Debug.LogWarning("--- PlayerIdleState.Enter --- Forcing isTreading = false in DiverMovement.");
        }

        // Optional: Stop any existing movement from previous state
        // If sinking is force-based, zeroing velocity might cause a pause.
        // if (stateMachine.Rb != null)
        // {
        //     stateMachine.Rb.velocity = Vector2.zero;
        // }
        
        // Optional: Trigger Idle animation if Animator exists
        // stateMachine.Animator?.Play("IdleAnimationName");

        // Ensure aiming is enabled
        if (stateMachine.ArmAim != null) stateMachine.ArmAim.enabled = true;

        // Ensure shooting is enabled
        if (stateMachine.DiverShooter != null) stateMachine.DiverShooter.enabled = true;
    }

    public override void Update()
    {
        // --- Transition Checks ---
        // Example: Check for movement input
        // Replace Input.GetAxisRaw with your actual input system check
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        if (Mathf.Abs(moveX) > 0.1f || Mathf.Abs(moveY) > 0.1f)
        {
            stateMachine.ChangeState(new PlayerSwimState(stateMachine)); // Transition to Swim state
            // Temporarily log until SwimState exists
            // Debug.Log("Movement detected, would transition to SwimState");
            return; // Exit Update early after state change
        }

        // Check for boost key press (can boost from idle?)
        if (stateMachine.DiverMovement != null && Input.GetKeyDown(stateMachine.DiverMovement.GetBoostKey()))
        {
             // Check boost allowed flag AND oxygen before allowing boost
            if (stateMachine.IsBoostAllowed && stateMachine.PlayerOxygen != null && stateMachine.PlayerOxygen.GetCurrentOxygen() > 0)
            {
                stateMachine.ChangeState(new PlayerBoostState(stateMachine));
                return;
            }
        }

        // Example: Check for shoot input (Now handled via Aim state)
        // if (Input.GetButtonDown("Fire1")) // Replace with your input

        // --- State Logic ---
        // Player is idle, maybe slowly regenerate something?
        // Or just wait for input.
    }

    public override void Exit()
    {
        Debug.Log("Exiting Idle State");
        // Re-enable movement processing before entering next state
        if(stateMachine.DiverMovement != null) stateMachine.DiverMovement.enabled = true;
        // Cleanup if needed before leaving Idle state
    }
}
