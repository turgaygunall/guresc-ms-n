using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }

    public override void EnterState()
    {
        // Hareket durumunu bildir
        player.staminaManager.SetPlayerMoving(true);

        player.animator.SetBool("IsMoving", true);
        Debug.Log("Entered Move State - Increased stamina consumption with reduced regeneration");
    }

    public override void UpdateState()
    {
        // Sürekli stamina consumption (hareket halinde)
        player.ConsumeStaminaOverTime(player.GetMoveStaminaConsumption());

        // Stamina tükendiyse idle'a geç
        if (player.IsStaminaDepleted())
        {
            Debug.Log("Cannot continue moving - stamina depleted!");
            player.ChangeState(player.idleState);
            return;
        }

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
        if (player.grabInput && player.CanUseStamina(player.GetGrabActionCost()))
        {
            // TODO: Change to grab state when implemented
            Debug.Log("Attempting to grab while moving!");
            player.UseStamina(player.GetGrabActionCost());
        }

        // Check for directional dodge (A+Shift or D+Shift)
        if (player.dodgeInput && player.CanUseStamina(player.GetDodgeActionCost()))
        {
            // Check if dodge is on cooldown
            if (player.dodgeState.IsDodgeOnCooldown())
            {
                Debug.Log("Dodge is on cooldown - cannot dodge yet");
                return;
            }

            // Check if a direction key is pressed
            bool hasDirection = player.leftInput || player.rightInput;

            if (hasDirection)
            {
                string direction = player.leftInput ? "left" : "right";
                Debug.Log($"Attempting to dodge {direction} while moving!");
                player.UseStamina(player.GetDodgeActionCost());
                player.ChangeState(player.dodgeState);
            }
            else
            {
                Debug.Log("Dodge attempted without direction - using movement direction");
                player.UseStamina(player.GetDodgeActionCost());
                player.ChangeState(player.dodgeState);
            }
        }
    }

    public override void ExitState()
    {
        player.animator.SetBool("IsMoving", false);
        Debug.Log("Exiting Move State");
    }
}