using UnityEngine;
using System;

[System.Serializable]
public class HealthData
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    public float CurrentHealth 
    { 
        get => currentHealth; 
        set => currentHealth = Mathf.Clamp(value, 0, maxHealth); 
    }
    
    public float HealthPercentage => maxHealth > 0 ? currentHealth / maxHealth : 0f;
    public bool IsDead => currentHealth <= 0f;
    
    public void Initialize()
    {
        currentHealth = maxHealth;
    }
    
    public void Reset()
    {
        currentHealth = maxHealth;
    }
}

[System.Serializable]
public class StaminaData
{
    [Header("Stamina Settings")]
    public bool useStamina = true;
    public float maxStamina = 100f;
    [SerializeField] private float currentStamina;
    
    [Header("Stamina Regeneration")]
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 1f;
    
    // Private fields for regen system
    private float regenTimer;
    private bool isRegenerating;
    
    public float CurrentStamina 
    { 
        get => currentStamina; 
        set => currentStamina = Mathf.Clamp(value, 0, maxStamina); 
    }
    
    public float StaminaPercentage => maxStamina > 0 ? currentStamina / maxStamina : 0f;
    public bool IsEmpty => currentStamina <= 0f;
    
    public void Initialize()
    {
        currentStamina = maxStamina;
        regenTimer = 0f;
        isRegenerating = false;
    }
    
    public void Reset()
    {
        currentStamina = maxStamina;
        regenTimer = 0f;
        isRegenerating = false;
    }
    
    public bool CanUseStamina(float amount)
    {
        return !useStamina || currentStamina >= amount;
    }
    
    public bool UseStamina(float amount)
    {
        if (!CanUseStamina(amount)) return false;
        
        currentStamina -= amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        
        // Reset regeneration timer
        regenTimer = 0f;
        isRegenerating = false;
        
        return true;
    }
    
    public void UpdateRegeneration()
    {
        if (!useStamina || currentStamina >= maxStamina) return;
        
        if (!isRegenerating)
        {
            regenTimer += Time.deltaTime;
            if (regenTimer >= staminaRegenDelay)
            {
                isRegenerating = true;
            }
        }
        else
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }
    }
}

public class CharacterStats : MonoBehaviour
{
    [Header("Character Type")]
    public CharacterType characterType = CharacterType.Player;
    
    [Header("Stats")]
    public HealthData health;
    public StaminaData stamina;
    
    [Header("Death Settings")]
    public bool disableOnDeath = true;
    public bool destroyOnDeath = false;
    public float destroyDelay = 2f;
    
    [Header("Damage Settings")]
    public bool isInvulnerable = false;
    public float invulnerabilityDuration = 0f;
    private float invulnerabilityTimer = 0f;
    
    // Events
    public event Action<float, float> OnHealthChanged;  // current, max
    public event Action<float, float> OnStaminaChanged; // current, max
    public event Action<float> OnDamageTaken;           // damage amount
    public event Action<float> OnHealed;                // heal amount
    public event Action OnDeath;
    public event Action OnRevived;
    
    // State
    public bool IsDead { get; private set; }
    public bool IsInvulnerable => isInvulnerable || invulnerabilityTimer > 0f;
    
    // Component references
    private Animator animator;
    private Rigidbody2D rb;
    private Collider2D col;
    
    public enum CharacterType
    {
        Player,
        Enemy,
        NPC
    }
    
    void Awake()
    {
        // Get component references
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        
        // Initialize stats
        health.Initialize();
        stamina.Initialize();
    }
    
    void Update()
    {
        // Update stamina regeneration
        stamina.UpdateRegeneration();
        
        // Update invulnerability timer
        if (invulnerabilityTimer > 0f)
        {
            invulnerabilityTimer -= Time.deltaTime;
        }
    }
    
    #region Health Management
    
    public void TakeDamage(float damage)
    {
        if (IsDead || IsInvulnerable || damage <= 0f) return;
        
        // Apply damage
        float oldHealth = health.CurrentHealth;
        health.CurrentHealth -= damage;
        
        // Trigger events
        OnDamageTaken?.Invoke(damage);
        OnHealthChanged?.Invoke(health.CurrentHealth, health.maxHealth);
        
        // Trigger animation if available
        if (animator != null)
        {
            animator.SetTrigger("TakeDamage");
        }
        
        // Check for death
        if (health.IsDead && !IsDead)
        {
            Die();
        }
        
        Debug.Log($"{gameObject.name} took {damage} damage. Health: {health.CurrentHealth}/{health.maxHealth}");
    }
    
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        
        float oldHealth = health.CurrentHealth;
        health.CurrentHealth += amount;
        
        // Trigger events
        OnHealed?.Invoke(amount);
        OnHealthChanged?.Invoke(health.CurrentHealth, health.maxHealth);
        
        Debug.Log($"{gameObject.name} healed {amount}. Health: {health.CurrentHealth}/{health.maxHealth}");
    }
    
    public void SetInvulnerability(float duration)
    {
        invulnerabilityTimer = duration;
    }
    
    #endregion
    
    #region Stamina Management
    
    public bool CanUseStamina(float amount)
    {
        return stamina.CanUseStamina(amount);
    }
    
    public bool UseStamina(float amount)
    {
        bool success = stamina.UseStamina(amount);
        if (success)
        {
            OnStaminaChanged?.Invoke(stamina.CurrentStamina, stamina.maxStamina);
            Debug.Log($"{gameObject.name} used {amount} stamina. Stamina: {stamina.CurrentStamina}/{stamina.maxStamina}");
        }
        return success;
    }
    
    public void RestoreStamina(float amount)
    {
        if (amount <= 0f) return;
        
        stamina.CurrentStamina += amount;
        OnStaminaChanged?.Invoke(stamina.CurrentStamina, stamina.maxStamina);
    }
    
    #endregion
    
    #region Death/Revival System
    
    private void Die()
    {
        if (IsDead) return;
        
        IsDead = true;
        
        // Trigger death event
        OnDeath?.Invoke();
        
        // Trigger death animation
        if (animator != null)
        {
            animator.SetTrigger("Death");
            animator.SetBool("IsDead", true);
        }
        
        Debug.Log($"{gameObject.name} has died!");
        
        // Handle death based on settings
        if (disableOnDeath)
        {
            DisableCharacter();
        }
        
        if (destroyOnDeath)
        {
            Destroy(gameObject, destroyDelay);
        }
    }
    
    public void Revive(float healthPercentage = 1f)
    {
        if (!IsDead) return;
        
        IsDead = false;
        
        // Restore health
        health.CurrentHealth = health.maxHealth * Mathf.Clamp01(healthPercentage);
        stamina.Reset();
        
        // Re-enable character
        EnableCharacter();
        
        // Trigger revival event
        OnRevived?.Invoke();
        
        // Update animations
        if (animator != null)
        {
            animator.SetBool("IsDead", false);
            animator.SetTrigger("Revive");
        }
        
        // Trigger events
        OnHealthChanged?.Invoke(health.CurrentHealth, health.maxHealth);
        OnStaminaChanged?.Invoke(stamina.CurrentStamina, stamina.maxStamina);
        
        Debug.Log($"{gameObject.name} has been revived with {healthPercentage * 100f}% health!");
    }
    
    private void DisableCharacter()
    {
        // Disable movement
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.simulated = false;
        }
        
        // Disable collision (but keep trigger for interactions)
        if (col != null && !col.isTrigger)
        {
            col.enabled = false;
        }
        
        // Disable character-specific components based on type
        switch (characterType)
        {
            case CharacterType.Player:
                var playerStateMachine = GetComponent<PlayerStateMachine>();
                if (playerStateMachine != null)
                {
                    playerStateMachine.enabled = false;
                }
                break;
                
            case CharacterType.Enemy:
                var enemy = GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.canAct = false;
                    enemy.isDead = true;
                }
                break;
        }
    }
    
    private void EnableCharacter()
    {
        // Enable movement
        if (rb != null)
        {
            rb.simulated = true;
        }
        
        // Enable collision
        if (col != null)
        {
            col.enabled = true;
        }
        
        // Enable character-specific components
        switch (characterType)
        {
            case CharacterType.Player:
                var playerStateMachine = GetComponent<PlayerStateMachine>();
                if (playerStateMachine != null)
                {
                    playerStateMachine.enabled = true;
                }
                break;
                
            case CharacterType.Enemy:
                var enemy = GetComponent<Enemy>();
                if (enemy != null)
                {
                    enemy.canAct = true;
                    enemy.isDead = false;
                }
                break;
        }
    }
    
    #endregion
    
    #region Utility Methods
    
    public void ResetAllStats()
    {
        health.Reset();
        stamina.Reset();
        
        if (IsDead)
        {
            Revive(1f);
        }
        
        OnHealthChanged?.Invoke(health.CurrentHealth, health.maxHealth);
        OnStaminaChanged?.Invoke(stamina.CurrentStamina, stamina.maxStamina);
    }
    
    public void SetMaxHealth(float newMaxHealth)
    {
        float ratio = health.HealthPercentage;
        health.maxHealth = newMaxHealth;
        health.CurrentHealth = newMaxHealth * ratio;
        
        OnHealthChanged?.Invoke(health.CurrentHealth, health.maxHealth);
    }
    
    public void SetMaxStamina(float newMaxStamina)
    {
        float ratio = stamina.StaminaPercentage;
        stamina.maxStamina = newMaxStamina;
        stamina.CurrentStamina = newMaxStamina * ratio;
        
        OnStaminaChanged?.Invoke(stamina.CurrentStamina, stamina.maxStamina);
    }
    
    #endregion
    
    #region Debug Methods
    
    [ContextMenu("Take 10 Damage")]
    public void DebugTakeDamage()
    {
        TakeDamage(10f);
    }
    
    [ContextMenu("Heal 20")]
    public void DebugHeal()
    {
        Heal(20f);
    }
    
    [ContextMenu("Kill Character")]
    public void DebugKill()
    {
        TakeDamage(health.CurrentHealth);
    }
    
    [ContextMenu("Revive Character")]
    public void DebugRevive()
    {
        Revive(1f);
    }
    
    [ContextMenu("Reset Stats")]
    public void DebugResetStats()
    {
        ResetAllStats();
    }
    
    #endregion
}