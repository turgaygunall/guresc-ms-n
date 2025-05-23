using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Combat Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxStamina = 100f;
    public float currentStamina;
    public float staminaRegenRate = 10f;
    
    
    [Header("Movement")]
    public float moveSpeed = 2f;
    public float detectionRange = 5f;
    
    [Header("Movement Speed Multipliers")]
    [SerializeField] public float forwardSpeedMultiplier = 1f;
    [SerializeField] public float backwardSpeedMultiplier = 0.7f;
    [SerializeField] public float sideStepSpeedMultiplier = 0.5f;
    
    [Header("Combat")]
    public float grabChance = 0.6f; // %60 yakalama şansı
    public float dodgeChance = 0.4f; // %40 kaçma şansı
    public float grabStaminaCost = 20f;
    public float dodgeStaminaCost = 15f;
    
    [Header("Dodge System")]
    [SerializeField] public float dodgeSpeed = 6f; // Dodge hızı
    [SerializeField] public float dodgeDistance = 2f; // Dodge mesafesi
    [SerializeField] public float dodgeDuration = 0.5f; // Maximum dodge süresi
    [SerializeField] public float dodgeCooldown = 2f; // Dodge'lar arası bekleme
    [SerializeField] public float dodgeDecisionDelay = 0.15f; // Attack circle'a girdikten sonra karar süresi
    [SerializeField] public float postDodgeWaitTime = 0.2f; // Dodge sonrası bekleme
    [SerializeField, Range(0f, 1f)] public float dodgeChanceInAttackCircle = 0.7f; // Attack circle içinde dodge şansı
    [SerializeField, Range(0f, 2f)] public float dodgeDistanceMultiplier = 1.2f; // Mesafe çarpanı (1.2 = %20 fazla)
    [SerializeField, Range(0f, 2f)] public float dodgeSpeedMultiplier = 1.5f; // Hız çarpanı (1.5 = %50 fazla)
    
    [Header("Attack Circle")]
    [SerializeField] public float attackCircleRadius = 2f; // Saldırı dairesi yarıçapı
    [SerializeField] public bool showAttackCircle = true; // Gizmos'ta göster
    [SerializeField] public Color attackCircleColor = Color.red;
    
    [Header("AI Difficulty")]
    public int difficultyLevel = 1; // 1-3 arası zorluk
    
    [Header("References")]
    public Transform player;
    public Animator animator;
    
    // State Machine
    private EnemyStateMachine stateMachine;
    
    // Combat flags
    public bool isGrabbed = false;
    public bool canAct = true;
    public bool isDead = false;
    
    // Attack Circle Status
    public bool isPlayerInAttackCircle = false;
    private bool wasPlayerInAttackCircle = false; // Önceki frame'deki durum
    
    // Events
    public System.Action OnHealthChanged;
    public System.Action OnStaminaChanged;
    public System.Action OnDeath;
    
    void Start()
    {
        InitializeEnemy();
        SetupStateMachine();
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Stamina regeneration
        RegenerateStamina();
        
        // Attack circle kontrolü
        UpdateAttackCircleStatus();
        
        // State machine update
        stateMachine?.Update();
    }
    
    void InitializeEnemy()
    {
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        
        // Player referansını bul
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
        }
        
        // Animator referansını al
        if (animator == null)
            animator = GetComponent<Animator>();
            
        // Zorluk seviyesine göre stats ayarla
        AdjustDifficultyStats();
    }
    
    void SetupStateMachine()
    {
        stateMachine = new EnemyStateMachine();
        
        // States ekle
        stateMachine.AddState("Idle", new EnemyIdleState(this));
        stateMachine.AddState("Move", new EnemyMoveState(this));
        // Diğer state'ler sonra eklenecek
        
        // Başlangıç state'i
        stateMachine.ChangeState("Idle");
    }
    
    void RegenerateStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            OnStaminaChanged?.Invoke();
        }
    }
    
   void AdjustDifficultyStats()
    {
        switch (difficultyLevel)
        {
            case 1: // Kolay
                grabChance = 0.5f;
                dodgeChance = 0.3f;
                moveSpeed = 1.5f;
                attackCircleRadius = 1.8f; // Daha küçük attack circle
                
                // Dodge parametreleri - kolay
                dodgeSpeed = 5f;
                dodgeDistance = 1.8f;
                dodgeDuration = 0.7f;
                dodgeCooldown = 3f;
                dodgeDecisionDelay = 0.25f;
                postDodgeWaitTime = 0.3f;
                dodgeChanceInAttackCircle = 0.5f;
                dodgeSpeedMultiplier = 1.2f;
                dodgeDistanceMultiplier = 1f;
                break;
                
            case 2: // Orta
                grabChance = 0.6f;
                dodgeChance = 0.4f;
                moveSpeed = 2f;
                attackCircleRadius = 2f; // Orta attack circle
                
                // Dodge parametreleri - orta (default değerler)
                dodgeSpeed = 6f;
                dodgeDistance = 2f;
                dodgeDuration = 0.5f;
                dodgeCooldown = 2f;
                dodgeDecisionDelay = 0.15f;
                postDodgeWaitTime = 0.2f;
                dodgeChanceInAttackCircle = 0.7f;
                dodgeSpeedMultiplier = 1.5f;
                dodgeDistanceMultiplier = 1.2f;
                break;
                
            case 3: // Zor
                grabChance = 0.7f;
                dodgeChance = 0.5f;
                moveSpeed = 2.5f;
                attackCircleRadius = 2.2f; // Daha büyük attack circle
                
                // Dodge parametreleri - zor
                dodgeSpeed = 7.5f;
                dodgeDistance = 2.2f;
                dodgeDuration = 0.4f;
                dodgeCooldown = 1.5f;
                dodgeDecisionDelay = 0.1f;
                postDodgeWaitTime = 0.15f;
                dodgeChanceInAttackCircle = 0.85f;
                dodgeSpeedMultiplier = 1.8f;
                dodgeDistanceMultiplier = 1.4f;
                break;
        }
        
        Debug.Log($"Difficulty {difficultyLevel} stats applied - Dodge Speed: {dodgeSpeed}, " +
                  $"Dodge Chance: {dodgeChanceInAttackCircle}, Cooldown: {dodgeCooldown}");
    }
    
    #region Combat Methods
    
    public bool TryGrab()
    {
        if (currentStamina < grabStaminaCost) return false;
        
        currentStamina -= grabStaminaCost;
        OnStaminaChanged?.Invoke();
        
        // Yakalama animasyonu
        animator?.SetTrigger("Grab");
        
        // Şans hesapla
        float roll = Random.Range(0f, 1f);
        bool success = roll <= grabChance;
        
        return success;
    }
    
    public bool TryDodge()
    {
        if (currentStamina < dodgeStaminaCost) return false;
        
        currentStamina -= dodgeStaminaCost;
        OnStaminaChanged?.Invoke();
        
        // Kaçma animasyonu
        animator?.SetTrigger("Dodge");
        
        // Şans hesapla
        float roll = Random.Range(0f, 1f);
        bool success = roll <= dodgeChance;
        
        return success;
    }
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        OnHealthChanged?.Invoke();
        
        // Hasar animasyonu
        animator?.SetTrigger("TakeDamage");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        OnHealthChanged?.Invoke();
    }
    
    void Die()
    {
        isDead = true;
        canAct = false;
        
        animator?.SetTrigger("Death");
        OnDeath?.Invoke();
        
        // State machine'i durdur
        stateMachine?.ChangeState("Dead");
    }
    
    #endregion
    
    #region Utility Methods
    
    #region Attack Circle Methods
    
    void UpdateAttackCircleStatus()
    {
        wasPlayerInAttackCircle = isPlayerInAttackCircle;
        isPlayerInAttackCircle = IsPlayerInAttackCircle();
        
        // Player attack circle'a girdi
        if (isPlayerInAttackCircle && !wasPlayerInAttackCircle)
        {
            OnPlayerEnteredAttackCircle();
        }
        // Player attack circle'dan çıktı
        else if (!isPlayerInAttackCircle && wasPlayerInAttackCircle)
        {
            OnPlayerExitedAttackCircle();
        }
    }
    
    public bool IsPlayerInAttackCircle()
    {
        if (player == null) return false;
        
        float distanceToPlayer = GetDistanceToPlayer();
        return distanceToPlayer <= attackCircleRadius;
    }
    
    void OnPlayerEnteredAttackCircle()
    {
        Debug.Log($"Player entered attack circle! Distance: {GetDistanceToPlayer():F2}");
        
        // Burada attack circle'a girince yapılacak işlemler
        // Örneğin state'e bilgi gönderme, animasyon tetikleme vs.
    }
    
    void OnPlayerExitedAttackCircle()
    {
        Debug.Log($"Player exited attack circle! Distance: {GetDistanceToPlayer():F2}");
        
        // Burada attack circle'dan çıkınca yapılacak işlemler
    }
    
    #endregion
    
    public float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector3.Distance(transform.position, player.position);
    }
    
    public bool IsPlayerInRange(float range)
    {
        return GetDistanceToPlayer() <= range;
    }
    
    public Vector3 GetDirectionToPlayer()
    {
        if (player == null) return Vector3.zero;
        return (player.position - transform.position).normalized;
    }
    
    public bool HasEnoughStamina(float requiredStamina)
    {
        return currentStamina >= requiredStamina;
    }
    
    #endregion
    
    #region Debug
    
    void OnDrawGizmosSelected()
    {
        // Detection range göster
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Attack circle göster
        if (showAttackCircle)
        {
            // Player içerideyse farklı renk
            Gizmos.color = isPlayerInAttackCircle ? Color.red : attackCircleColor;
            Gizmos.DrawWireSphere(transform.position, attackCircleRadius);
            
            // Attack circle'ın dolgusunu da hafif göster
            Color fillColor = Gizmos.color;
            fillColor.a = 0.1f;
            Gizmos.color = fillColor;
            Gizmos.DrawSphere(transform.position, attackCircleRadius);
        }
    }
    
    void OnDrawGizmos()
    {
        // Sürekli görünür attack circle (seçili olmasa da)
        if (showAttackCircle && Application.isPlaying)
        {
            Gizmos.color = isPlayerInAttackCircle ? Color.red : attackCircleColor;
            Gizmos.DrawWireSphere(transform.position, attackCircleRadius);
        }
    }
    
    
    
    #endregion
}