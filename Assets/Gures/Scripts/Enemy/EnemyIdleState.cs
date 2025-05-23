using UnityEngine;

public class EnemyIdleState : EnemyStateBase
{
    [Header("Idle State Settings")]
    private float idleTime = 0f;
    private float maxIdleTime = 2f; // Maximum bekleyiş süresi
    private float awarenessRadius = 5f; // Oyuncuyu fark etme mesafesi
    
    // Idle animasyon varyasyonları için
    private float lastIdleVariation = 0f;
    private float idleVariationInterval = 3f;
    
    public EnemyIdleState(Enemy enemy) : base(enemy)
    {
        // Idle sürelerini kısaltalım - hızlı geçişler için
        switch (enemy.difficultyLevel)
        {
            case 1:
                maxIdleTime = 1f; // Çok kısa idle
                awarenessRadius = 4f;
                break;
            case 2:
                maxIdleTime = 0.7f; // Daha da kısa
                awarenessRadius = 5f;
                break;
            case 3:
                maxIdleTime = 0.4f; // Neredeyse anında move'a geç
                awarenessRadius = 6f;
                break;
        }
    }
    
    public override void Enter()
    {
        base.Enter();
        
        // Idle animasyonunu başlat
        enemy.animator?.SetBool("IsIdle", true);
        enemy.animator?.SetBool("IsMoving", false);
        enemy.animator?.SetBool("IsAttacking", false);
        
        // Idle zamanını sıfırla
        idleTime = 0f;
        lastIdleVariation = 0f;
        
        Debug.Log($"Enemy entered Idle state (Difficulty: {enemy.difficultyLevel})");
    }
    
    public override void Update()
    {
        // Ölü mü kontrol et
        if (enemy.isDead)
        {
            nextState = "Dead";
            return;
        }
        
        // Tutulmuş mu kontrol et
        if (enemy.isGrabbed)
        {
            nextState = "Grabbed";
            return;
        }
        
        // Idle süresini artır
        idleTime += Time.deltaTime;
        lastIdleVariation += Time.deltaTime;
        
        // Idle animasyon varyasyonları
        HandleIdleAnimations();
        
        // Oyuncu farkındalığı kontrolü
        CheckPlayerAwareness();
        
        // Çok kısa idle sonrası hemen move'a geç
        if (idleTime >= maxIdleTime)
        {
            // %90 şansla move'a geç - neredeyse her zaman
            if (Random.Range(0f, 1f) > 0.1f)
            {
                nextState = "Move";
            }
            else
            {
                // Sadece %10 şansla biraz daha bekle
                idleTime = 0f;
            }
        }
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Idle animasyonunu kapat
        enemy.animator?.SetBool("IsIdle", false);
        
        Debug.Log("Enemy exited Idle state");
    }
    
    private void HandleIdleAnimations()
    {
        // Belirli aralıklarla idle animasyon varyasyonları çal
        if (lastIdleVariation >= idleVariationInterval)
        {
            lastIdleVariation = 0f;
            
            // Rastgele idle animasyonu seç
            int randomIdle = Random.Range(0, 3);
            
            switch (randomIdle)
            {
                case 0:
                    // Normal nefes alma animasyonu
                    enemy.animator?.SetTrigger("IdleBreathe");
                    break;
                case 1:
                    // Yağlanma kontrol animasyonu
                    enemy.animator?.SetTrigger("IdleCheckOil");
                    break;
                case 2:
                    // Kas germe animasyonu
                    enemy.animator?.SetTrigger("IdleStretch");
                    break;
            }
        }
    }
    
    private void CheckPlayerAwareness()
    {
        // Oyuncu tespit mesafesi kontrolü
        float distanceToPlayer = GetDistanceToPlayer();
        
        if (distanceToPlayer <= awarenessRadius)
        {
            // Oyuncuyu fark etti!
            
            // Zorluk seviyesine göre tepki süresi
            float reactionDelay = GetReactionDelay();
            
            if (idleTime >= reactionDelay)
            {
                // Mesafeye göre state seç
                if (distanceToPlayer <= 2f)
                {
                    // Çok yakın - saldır
                    if (HasEnoughStamina(enemy.grabStaminaCost))
                    {
                        nextState = "Attack";
                    }
                    else
                    {
                        nextState = "Defend"; // Stamina yoksa savun
                    }
                }
                else if (distanceToPlayer <= 4f)
                {
                    // Orta mesafe - yaklaş
                    nextState = "Approach";
                }
                else
                {
                    // Uzak mesafe - takip et
                    nextState = "Chase";
                }
            }
        }
    }
    
    private float GetReactionDelay()
    {
        // Zorluk seviyesine göre tepki süreleri
        switch (enemy.difficultyLevel)
        {
            case 1:
                return Random.Range(0.8f, 1.5f); // Yavaş tepki
            case 2:
                return Random.Range(0.4f, 0.8f); // Orta tepki
            case 3:
                return Random.Range(0.1f, 0.4f); // Hızlı tepki
            default:
                return 1f;
        }
    }
    
    // Dışarıdan state değişikliğine zorlamak için
    public void ForceStateChange(string newState)
    {
        nextState = newState;
    }
    
    // Debug için oyuncu mesafesini göster
    public float GetCurrentPlayerDistance()
    {
        return GetDistanceToPlayer();
    }
    
    // Idle state bilgilerini al
    public float GetIdleProgress()
    {
        return idleTime / maxIdleTime;
    }
}