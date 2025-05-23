using UnityEngine;

// Add RequireComponent attributes to ensure these components exist
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(StaminaManager))]
public class PlayerStateMachine : MonoBehaviour
{
    public PlayerState currentState;

    [Header("Player Stats")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("Movement")]
    public float moveSpeed = 2f;

    // Components
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator animator;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    [HideInInspector] public StaminaManager staminaManager;

    // Input
    [HideInInspector] public float horizontalInput;
    [HideInInspector] public bool grabInput;
    [HideInInspector] public bool dodgeInput;
    [HideInInspector] public bool leftInput;    // Added for directional dodge
    [HideInInspector] public bool rightInput;   // Added for directional dodge

    // States
    [HideInInspector] public PlayerIdleState idleState;
    [HideInInspector] public PlayerMoveState moveState;
    [HideInInspector] public PlayerDodgeState dodgeState;

    void Awake()
    {
        // Get components and ensure they're enabled
        rb = GetComponent<Rigidbody2D>();
        if (rb) rb.simulated = true;  // Rigidbody2D uses simulated instead of enabled

        animator = GetComponent<Animator>();
        if (animator) animator.enabled = true;

        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer) spriteRenderer.enabled = true;

        staminaManager = GetComponent<StaminaManager>();

        // Stamina Manager yoksa ekle
        if (staminaManager == null)
        {
            staminaManager = gameObject.AddComponent<StaminaManager>();
            Debug.Log("StaminaManager component added automatically");
        }

        // Ensure StaminaManager is enabled
        if (staminaManager) staminaManager.enabled = true;

        // Initialize states
        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        dodgeState = new PlayerDodgeState(this);

        // Set initial values
        currentHealth = maxHealth;

        // Set initial state
        currentState = idleState;
        currentState.EnterState();

        // Subscribe to stamina events
        staminaManager.OnStaminaDepleted += OnStaminaDepleted;
        staminaManager.OnStaminaRecovered += OnStaminaRecovered;
    }

    void OnEnable()
    {
        // Re-enable components if they exist but were disabled
        if (rb) rb.simulated = true;  // Rigidbody2D uses simulated instead of enabled
        if (animator) animator.enabled = true;
        if (spriteRenderer) spriteRenderer.enabled = true;
        if (staminaManager) staminaManager.enabled = true;
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (staminaManager)
        {
            staminaManager.OnStaminaDepleted -= OnStaminaDepleted;
            staminaManager.OnStaminaRecovered -= OnStaminaRecovered;
        }
    }

    void Update()
    {
        HandleInput();
        currentState.UpdateState();

        // Update dodge cooldown if needed
        if (dodgeState != null && dodgeState.IsDodgeOnCooldown())
        {
            dodgeState.UpdateCooldown();
        }
    }

    void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        grabInput = Input.GetKeyDown(KeyCode.Space);
        dodgeInput = Input.GetKeyDown(KeyCode.LeftShift);
        leftInput = Input.GetKey(KeyCode.A);
        rightInput = Input.GetKey(KeyCode.D);
    }

    void OnStaminaDepleted()
    {
        Debug.Log("[Player] Stamina depleted! Forcing to idle state");
        if (currentState != idleState)
        {
            ChangeState(idleState);
        }
    }

    void OnStaminaRecovered()
    {
        Debug.Log("[Player] Stamina recovered! Can move again");
    }

    public void ChangeState(PlayerState newState)
    {
        currentState.ExitState();
        currentState = newState;
        currentState.EnterState();
    }

    // Stamina helper methods
    public bool CanUseStamina(float staminaCost)
    {
        return staminaManager.CanUseStamina(staminaCost);
    }

    public bool UseStamina(float staminaCost)
    {
        return staminaManager.ConsumeStamina(staminaCost);
    }

    public void ConsumeStaminaOverTime(float consumptionRate)
    {
        staminaManager.ConsumeStaminaOverTime(consumptionRate);
    }

    public bool IsStaminaDepleted()
    {
        return staminaManager.IsStaminaDepleted;
    }

    // Consumption rate getters
    public float GetIdleStaminaConsumption() => staminaManager.GetIdleConsumption();
    public float GetMoveStaminaConsumption() => staminaManager.GetMoveConsumption();
    public float GetDodgeStaminaConsumption() => staminaManager.GetDodgeConsumption();
    public float GetGrabStaminaConsumption() => staminaManager.GetGrabConsumption();

    // Action cost getters
    public float GetDodgeActionCost() => staminaManager.GetDodgeActionCost();
    public float GetGrabActionCost() => staminaManager.GetGrabActionCost();

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (currentHealth <= 0)
        {
            Debug.Log("Player defeated!");
        }
    }

    public void FlipSprite(bool facingRight)
    {
        spriteRenderer.flipX = !facingRight;
    }
}