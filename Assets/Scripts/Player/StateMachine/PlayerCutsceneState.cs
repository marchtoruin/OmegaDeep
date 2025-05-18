using UnityEngine;

public class PlayerCutsceneState : PlayerBaseState
{
    public PlayerCutsceneState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void Enter()
    {
        Debug.Log("Entering Cutscene State (Player Immobile)");

        // Disable essential actions
        if (stateMachine.DiverMovement != null)
        {
            stateMachine.DiverMovement.enabled = false;
            stateMachine.DiverMovement.SetKnockbackState(false); // Call method, don't assign
        }
        // Use explicit null check before disabling ArmAim
        if (stateMachine.ArmAim != null) 
        { 
            stateMachine.ArmAim.enabled = false; 
        }
        if (stateMachine.DiverShooter != null) stateMachine.DiverShooter.enabled = false; // Disable shooting script
        // Optionally disable flashlight, other abilities?
        // stateMachine.FlashlightController?.enabled = false;

        // Stop physical movement
        if (stateMachine.Rb != null)
        {
            stateMachine.Rb.velocity = Vector2.zero;
            stateMachine.Rb.isKinematic = true; // Make kinematic to ignore physics/gravity
        }

        // Set appropriate animation?
        // stateMachine.Animator?.Play("CutsceneIdle"); 
    }

    // Player does nothing in this state, waits for external trigger to change state
    public override void Update() { }

    public override void Exit()
    {
        Debug.Log("Exiting Cutscene State");

        // Restore physics control
        if (stateMachine.Rb != null)
        {
            stateMachine.Rb.isKinematic = false;
        }

        // Re-enable components IF the next state needs them.
        // Often, the next state (e.g., Idle) will handle enabling what it needs.
        // It's generally safer to let the *entering* state enable what it requires.
        // However, explicitly enabling here can be done if needed.
        // stateMachine.DiverMovement?.enabled = true; // Idle/Swim state will likely do this
        // stateMachine.ArmAim?.enabled = true; // Assuming aiming is always possible outside cutscenes
        // if (stateMachine.DiverShooter != null) stateMachine.DiverShooter.enabled = true;
        // stateMachine.FlashlightController?.enabled = true;
    }
}
