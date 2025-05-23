using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }
    
    public override void EnterState()
    {
        player.animator.SetBool("IsMoving", false);
        player.rb.velocity = Vector2.zero;
    }
    
    public override void UpdateState()
    {
        // Check for movement input
        if (Mathf.Abs(player.horizontalInput) > 0.1f)
        {
            player.ChangeState(player.moveState);
            return;
        }
        
        // Handle combat inputs
        if (player.grabInput && player.CanUseStamina(player.grabStaminaCost))
        {
            // TODO: Change to grab state when implemented
            Debug.Log("Attempting to grab!");
            player.UseStamina(player.grabStaminaCost);
        }
        
        if (player.dodgeInput && player.CanUseStamina(player.dodgeStaminaCost))
        {
            // TODO: Change to dodge state when implemented
            Debug.Log("Attempting to dodge!");
            player.UseStamina(player.dodgeStaminaCost);
        }
    }
    
    public override void ExitState()
    {
        // Clean up idle state
    }
}