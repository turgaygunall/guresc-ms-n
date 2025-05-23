using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }
    
    public override void EnterState()
    {
        player.animator.SetBool("IsMoving", true);
    }
    
    public override void UpdateState()
    {
        // Check if still moving
        if (Mathf.Abs(player.horizontalInput) < 0.1f)
        {
            player.ChangeState(player.idleState);
            return;
        }
        
        // Handle movement
        Vector2 movement = new Vector2(player.horizontalInput * player.moveSpeed, player.rb.velocity.y);
        player.rb.velocity = movement;
        
        // Handle sprite flipping
        if (player.horizontalInput > 0)
        {
            player.FlipSprite(true);
        }
        else if (player.horizontalInput < 0)
        {
            player.FlipSprite(false);
        }
        
        // Handle combat inputs while moving
        if (player.grabInput && player.CanUseStamina(player.grabStaminaCost))
        {
            // TODO: Change to grab state when implemented
            Debug.Log("Attempting to grab while moving!");
            player.UseStamina(player.grabStaminaCost);
        }
        
        if (player.dodgeInput && player.CanUseStamina(player.dodgeStaminaCost))
        {
            // TODO: Change to dodge state when implemented
            Debug.Log("Attempting to dodge while moving!");
            player.UseStamina(player.dodgeStaminaCost);
        }
    }
    
    public override void ExitState()
    {
        player.animator.SetBool("IsMoving", false);
    }
}