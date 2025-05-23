using UnityEngine;

public class PlayerDodgeState : PlayerState
{
    // Dodge parameters
    private float dodgeDuration = 0.5f;    // How long the dodge lasts
    private float dodgeSpeed = 8f;         // How fast the dodge moves
    private float dodgeTimer = 0f;         // Timer to track dodge progress
    private Vector2 dodgeDirection;        // Direction of the dodge
    private bool dodgeDirectionSet = false; // Flag to ensure direction is set once
    private float invulnerabilityDuration = 0.3f; // Duration of invulnerability during dodge
    private float dodgeCooldown = 0.7f;    // Cooldown before next dodge
    private bool isDodgeOnCooldown = false; // Flag to track cooldown state
    private float cooldownTimer = 0f;      // Timer for cooldown

    public PlayerDodgeState(PlayerStateMachine playerStateMachine) : base(playerStateMachine) { }

    public override void EnterState()
    {
        // Reset timer
        dodgeTimer = 0f;
        dodgeDirectionSet = false;

        // Set animation
        player.animator.SetBool("IsDodging", true);

        // Determine dodge direction based on input
        DetermineDodgeDirection();

        Debug.Log($"Entered Dodge State - Direction: {(dodgeDirection.x > 0 ? "Right" : "Left")}");

        // Apply initial dodge force
        ApplyDodgeForce();

        // Apply temporary invulnerability if there's a method for it
        // player.SetInvulnerable(invulnerabilityDuration);
    }   

    public override void UpdateState()
    {
        // Update timer
        dodgeTimer += Time.deltaTime;

        // Apply dodge force each frame for smooth movement
        ApplyDodgeForce();

        // Check if dodge is complete
        if (dodgeTimer >= dodgeDuration)
        {
            // Set dodge on cooldown
            isDodgeOnCooldown = true;
            cooldownTimer = 0f;

            // Return to appropriate state
            if (Mathf.Abs(player.horizontalInput) > 0.1f && !player.IsStaminaDepleted())
            {
                player.ChangeState(player.moveState);
            }
            else
            {
                player.ChangeState(player.idleState);
            }
        }
    }

    // Static method to check if dodge is on cooldown (can be called from other states)
    public bool IsDodgeOnCooldown()
    {
        return isDodgeOnCooldown;
    }

    // Method to update cooldown - should be called by PlayerStateMachine in Update
    public void UpdateCooldown()
    {
        if (isDodgeOnCooldown)
        {
            cooldownTimer += Time.deltaTime;

            if (cooldownTimer >= dodgeCooldown)
            {
                isDodgeOnCooldown = false;
                Debug.Log("Dodge cooldown complete - ready to dodge again");
            }
        }
    }

    public override void ExitState()
    {
        // Reset animation
        player.animator.SetBool("IsDodging", false);

        Debug.Log("Exiting Dodge State");
    }

    private void DetermineDodgeDirection()
    {
        if (dodgeDirectionSet) return;

        // Check for A+Shift (left dodge) or D+Shift (right dodge)
        if (player.leftInput)
        {
            // Dodge left
            dodgeDirection = Vector2.left;
            player.FlipSprite(false);
        }
        else if (player.rightInput)
        {
            // Dodge right
            dodgeDirection = Vector2.right;
            player.FlipSprite(true);
        }
        else
        {
            // If no specific direction key is pressed, use the last movement direction
            dodgeDirection = new Vector2(Mathf.Sign(player.horizontalInput), 0);

            // Default to right if no direction
            if (dodgeDirection.x == 0)
            {
                dodgeDirection = Vector2.right;
                player.FlipSprite(true);
            }
            else
            {
                player.FlipSprite(dodgeDirection.x > 0);
            }
        }

        dodgeDirectionSet = true;
    }

    private void ApplyDodgeForce()
    {
        // Calculate dodge velocity with easing (faster at start, slower at end)
        float progress = dodgeTimer / dodgeDuration;
        float currentDodgeSpeed = dodgeSpeed * (1 - progress);

        // Apply dodge velocity
        Vector2 dodgeVelocity = dodgeDirection * currentDodgeSpeed;
        player.rb.velocity = new Vector2(dodgeVelocity.x, player.rb.velocity.y);
    }
}