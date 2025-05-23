using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public PlayerState currentState;
    
    [Header("Player Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 2f;
    
    [Header("Movement")]
    public float moveSpeed = 2f;
    
    [Header("Combat")]
    public float grabStaminaCost = 30f;
    public float dodgeStaminaCost = 25f;
    
    // Components
    [HideInInspector] public Rigidbody2D rb;
    [HideInInspector] public Animator animator;
    [HideInInspector] public SpriteRenderer spriteRenderer;
    
    // Input
    [HideInInspector] public float horizontalInput;
    [HideInInspector] public bool grabInput;
    [HideInInspector] public bool dodgeInput;
    
    // States
    [HideInInspector] public PlayerIdleState idleState;
    [HideInInspector] public PlayerMoveState moveState;
    
    // Stamina regen timer
    private float staminaRegenTimer;
    private bool isRegeneratingStamina = false;
    
    void Awake()
    {
        // Get components
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Initialize states
        idleState = new PlayerIdleState(this);
        moveState = new PlayerMoveState(this);
        
        // Set initial values
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        
        // Set initial state
        currentState = idleState;
        currentState.EnterState();
    }
    
    void Update()
    {
        HandleInput();
        HandleStaminaRegen();
        currentState.UpdateState();
    }
    
    void HandleInput()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        grabInput = Input.GetKeyDown(KeyCode.Space);
        dodgeInput = Input.GetKeyDown(KeyCode.LeftShift);
    }
    
    void HandleStaminaRegen()
    {
        if (currentStamina < maxStamina)
        {
            if (!isRegeneratingStamina)
            {
                staminaRegenTimer += Time.deltaTime;
                if (staminaRegenTimer >= staminaRegenDelay)
                {
                    isRegeneratingStamina = true;
                }
            }
            else
            {
                currentStamina += staminaRegenRate * Time.deltaTime;
                currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            }
        }
    }
    
    public void ChangeState(PlayerState newState)
    {
        currentState.ExitState();
        currentState = newState;
        currentState.EnterState();
    }
    
    public bool CanUseStamina(float staminaCost)
    {
        return currentStamina >= staminaCost;
    }
    
    public void UseStamina(float staminaCost)
    {
        currentStamina -= staminaCost;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        
        // Reset stamina regen
        staminaRegenTimer = 0f;
        isRegeneratingStamina = false;
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (currentHealth <= 0)
        {
            // Handle death
            Debug.Log("Player defeated!");
        }
    }
    
    public void FlipSprite(bool facingRight)
    {
        spriteRenderer.flipX = !facingRight;
    }
}