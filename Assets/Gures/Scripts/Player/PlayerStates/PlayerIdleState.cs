using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }

    public override void EnterState()
    {
        player.animator.SetBool("IsMoving", false);
        player.rb.velocity = Vector2.zero;

        // Idle durumunda olduğunu StaminaManager'a bildir
        player.staminaManager.SetPlayerIdle(true);

        Debug.Log("Entered Idle State - Stamina regeneration active");
    }

    public override void UpdateState()
    {
        // Idle durumunda stamina tüketmek yerine rejenerasyon oluyor (SetPlayerIdle true)
        // Artık burada ConsumeStaminaOverTime çağırmıyoruz!

        // Check for movement input (sadece stamina varsa)
        if (Mathf.Abs(player.horizontalInput) > 0.1f && !player.IsStaminaDepleted())
        {
            player.ChangeState(player.moveState);
            return;
        }

        // Handle combat inputs
        if (player.grabInput && player.CanUseStamina(player.GetGrabActionCost()))
        {
            // TODO: Change to grab state when implemented
            Debug.Log("Attempting to grab from idle!");
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
                Debug.Log($"Attempting to dodge {direction} from idle!");
                player.UseStamina(player.GetDodgeActionCost());
                player.ChangeState(player.dodgeState);
            }
            else
            {
                Debug.Log("Dodge attempted without direction - using last movement direction");
                player.UseStamina(player.GetDodgeActionCost());
                player.ChangeState(player.dodgeState);
            }
        }

        // Stamina tükendiyse hareket etmeye çalışırken uyarı
        if (Mathf.Abs(player.horizontalInput) > 0.1f && player.IsStaminaDepleted())
        {
            Debug.Log("Cannot move - stamina depleted! Resting to recover...");
        }
    }

    public override void ExitState()
    {
        // Idle durumundan çıkarken StaminaManager'a bildir
        player.staminaManager.SetPlayerIdle(false);

        Debug.Log("Exiting Idle State");
    }
}