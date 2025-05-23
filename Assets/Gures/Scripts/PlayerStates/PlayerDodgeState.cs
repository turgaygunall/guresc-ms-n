// PlayerDodgeState.cs
using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    private float dodgeForce = 8f;
    private float dodgeDuration = 0.5f;
    private float dodgeTimer;
    private Vector2 dodgeDirection;
    private bool isDodging;
    
    public PlayerDodgeState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }
    
    public override void EnterState()
    {
        // Set dodge direction based on input or facing direction
        if (Mathf.Abs(player.horizontalInput) > 0.1f)
        {
            // Dodge in input direction
            dodgeDirection = new Vector2(Mathf.Sign(player.horizontalInput), 0);
        }
        else
        {
            // Dodge forward (based on sprite facing)
            dodgeDirection = new Vector2(player.spriteRenderer.flipX ? -1 : 1, 0);
        }
        
        // Apply dodge force
        player.rb.velocity = dodgeDirection * dodgeForce;
        
        // Set animation
        player.animator.SetBool("IsDodging", true);
        
        // Reset timer
        dodgeTimer = 0f;
        isDodging = true;
        
        Debug.Log($"Dodging in direction: {dodgeDirection}");
    }
    
    public override void UpdateState()
    {
        dodgeTimer += Time.deltaTime;
        
        // Gradually reduce velocity during dodge
        if (isDodging)
        {
            float remainingTime = dodgeDuration - dodgeTimer;
            if (remainingTime > 0)
            {
                float velocityMultiplier = remainingTime / dodgeDuration;
                player.rb.velocity = dodgeDirection * dodgeForce * velocityMultiplier;
            }
            else
            {
                // End dodge
                isDodging = false;
                player.rb.velocity = Vector2.zero;
            }
        }
        
        // Return to idle when dodge is complete
        if (dodgeTimer >= dodgeDuration)
        {
            // Check if player wants to move after dodge
            if (Mathf.Abs(player.horizontalInput) > 0.1f)
            {
                player.ChangeState(player.moveState);
            }
            else
            {
                player.ChangeState(player.idleState);
            }
        }
    }
    
    public override void ExitState()
    {
        player.animator.SetBool("IsDodging", false);
        player.rb.velocity = Vector2.zero;
        isDodging = false;
    }
}