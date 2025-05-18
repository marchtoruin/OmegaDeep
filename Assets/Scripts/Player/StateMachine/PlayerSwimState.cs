using UnityEngine;

public class PlayerSwimState : PlayerBaseState
{
    // You might want to add speed parameters or references if needed here

    public PlayerSwimState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entering Swim State");
        // Ensure movement script is enabled
        if (stateMachine.DiverMovement != null) stateMachine.DiverMovement.enabled = true;

        // Ensure shooting is enabled
        if (stateMachine.DiverShooter != null) stateMachine.DiverShooter.enabled = true;

        // Optional: Trigger Swim animation if Animator exists
        // stateMachine.Animator?.Play("SwimAnimationName"); // Or set a bool parameter
        stateMachine.Animator?.SetBool("IsMoving", true); // Example using a bool parameter

        // --- State Logic (Movement) ---
        // Movement logic is handled by the enabled DiverMovement script itself.
        // This state only needs to handle transitions out.
    }

    public override void Update()
    {
        // --- Read Input --- (Assuming input is read centrally in PlayerStateMachine or here)
        // Replace with your actual input reading mechanism
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 moveInput = new Vector2(moveX, moveY);

        // --- Transition Checks ---
        // Check if player stopped moving
        if (moveInput.magnitude < 0.1f)
        {
            stateMachine.ChangeState(new PlayerIdleState(stateMachine));
            return; // Exit Update early after state change
        }

        // Check for boost key press
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

        // --- State Logic (Movement) ---
        // Movement logic is handled by the enabled DiverMovement script itself.
        // This state only needs to handle transitions out.
    }

    public override void Exit()
    {
        Debug.Log("Exiting Swim State");
        // Optional: Reset animation state if needed
        stateMachine.Animator?.SetBool("IsMoving", false);

        // Optional: Stop movement if DiverMovement doesn't handle zero input automatically
        // if (stateMachine.Rb != null) stateMachine.Rb.velocity = Vector2.zero;
    }
}
