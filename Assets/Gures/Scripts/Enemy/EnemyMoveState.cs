using UnityEngine;

public class EnemyMoveState : EnemyStateBase
{
    [Header("Movement Behavior")]
    private float baseMovementSpeed;
    private float currentMoveDirection = 1f; // 1 = ileri, -1 = geri
    
    // Timing variables
    private float movementTimer = 0f;
    private float currentMovementDuration;
    private float pauseTimer = 0f;
    private float currentPauseDuration;
    
    // Behavior states
    private MovementPhase currentPhase = MovementPhase.Advancing;
    private MovementPhase lastPhase = MovementPhase.Waiting;
    private bool isPaused = false;
    
    // Distance tracking - Daha büyük mesafeler
    private float idealDistance = 3f; // Büyütüldü
    private float comfortZoneMin = 2f; // Büyütüldü  
    private float comfortZoneMax = 5f; // Büyütüldü
    
    // Pressure system
    private float pressureTimer = 0f;
    private float maxPressureTime = 6f;
    
    // Hareket geçmişi - art arda aynı hareketi engellemek için
    private int consecutiveAdvanceCount = 0;
    private int consecutiveRetreatCount = 0;
    
    // DODGE SYSTEM - Inspector'dan alınan parametreler
    [Header("Dodge System")]
    private float lastDodgeTime = 0f;
    private float dodgeCooldown; // Inspector'dan alınacak
    private bool wasInAttackCircleLastFrame = false;
    private float attackCircleEntryTime = 0f;
    private float dodgeDecisionDelay; // Inspector'dan alınacak
    
    private enum MovementPhase
    {
        Advancing,      // İlerliyor
        Retreating,     // Geri çekiliyor
        Waiting         // Bekliyor/gözlüyor
    }
    
    public EnemyMoveState(Enemy enemy) : base(enemy)
    {
        // Inspector'dan dodge parametrelerini al
        dodgeCooldown = enemy.dodgeCooldown;
        dodgeDecisionDelay = enemy.dodgeDecisionDelay;
        
        // Zorluk seviyesine göre ayarlamalar - daha büyük comfort zone'lar
        switch (enemy.difficultyLevel)
        {
            case 1: // Kolay
                baseMovementSpeed = enemy.moveSpeed * 0.8f;
                idealDistance = 3.5f; 
                comfortZoneMin = 2.5f;
                comfortZoneMax = 5f;
                maxPressureTime = 8f;
                // Dodge parametrelerini zorluğa göre ayarla
                dodgeCooldown *= 1.3f; // Daha uzun cooldown
                dodgeDecisionDelay *= 1.5f; // Daha yavaş karar
                break;
                
            case 2: // Orta
                baseMovementSpeed = enemy.moveSpeed * 1f;
                idealDistance = 3f;
                comfortZoneMin = 2f;
                comfortZoneMax = 4.5f;
                maxPressureTime = 6f;
                // Inspector değerlerini kullan
                break;
                
            case 3: // Zor
                baseMovementSpeed = enemy.moveSpeed * 1.2f;
                idealDistance = 2.5f;
                comfortZoneMin = 1.5f;
                comfortZoneMax = 4f;
                maxPressureTime = 4f;
                // Dodge parametrelerini zorluğa göre ayarla
                dodgeCooldown *= 0.7f; // Daha kısa cooldown
                dodgeDecisionDelay *= 0.6f; // Daha hızlı karar
                break;
        }
        
        SetNextMovementDuration();
    }
    
    public override void Enter()
    {
        base.Enter();
        
        // Animasyonları ayarla
        enemy.animator?.SetBool("IsMoving", true);
        enemy.animator?.SetBool("IsIdle", false);
        enemy.animator?.SetFloat("MoveSpeed", 0.5f);
        
        // İlk hareketi belirle
        lastPhase = MovementPhase.Waiting;
        consecutiveAdvanceCount = 0;
        consecutiveRetreatCount = 0;
        
        // Dodge system başlangıç değerleri
        wasInAttackCircleLastFrame = enemy.isPlayerInAttackCircle;
        attackCircleEntryTime = 0f;
        
        DecideNextMovement();
        
        Debug.Log($"Enemy entered Move state - Phase: {currentPhase}");
    }
    
    public override void Update()
    {
        // Temel kontroller
        if (enemy.isDead)
        {
            nextState = "Dead";
            return;
        }
        
        if (enemy.isGrabbed)
        {
            nextState = "Grabbed";
            return;
        }
        
        // DODGE KONTROLÜ - Attack circle'a giriş tespiti
        CheckAttackCircleForDodge();
        
        // Timers güncelle
        movementTimer += Time.deltaTime;
        pressureTimer += Time.deltaTime;
        
        // Ana hareket mantığı
        if (isPaused)
        {
            HandlePause();
        }
        else
        {
            HandleMovement();
        }
        
        // State geçiş kontrolleri
        CheckStateTransitions();
        
        // Animasyon güncellemeleri
        UpdateAnimations();
        
        // Frame sonu güncellemesi
        wasInAttackCircleLastFrame = enemy.isPlayerInAttackCircle;
    }
    
    #region Dodge System
    
    private void CheckAttackCircleForDodge()
    {
        // Attack circle'a yeni girdi mi?
        bool justEnteredAttackCircle = enemy.isPlayerInAttackCircle && !wasInAttackCircleLastFrame;
        
        if (justEnteredAttackCircle)
        {
            attackCircleEntryTime = 0f;
            Debug.Log("Player just entered attack circle during move state!");
        }
        
        // Attack circle içindeyse timer'ı artır
        if (enemy.isPlayerInAttackCircle)
        {
            attackCircleEntryTime += Time.deltaTime;
            
            // Karar verme zamanı geldi mi?
            if (attackCircleEntryTime >= dodgeDecisionDelay)
            {
                TryDecideDodge();
            }
        }
        else
        {
            // Attack circle dışına çıktı, timer'ı sıfırla
            attackCircleEntryTime = 0f;
        }
    }
    
    private void TryDecideDodge()
    {
        // Dodge cooldown kontrolü
        if (Time.time - lastDodgeTime < dodgeCooldown)
        {
            return;
        }
        
        // Stamina kontrolü
        if (!HasEnoughStamina(enemy.dodgeStaminaCost))
        {
            return;
        }
        
        // Dodge şansı hesapla
        float dodgeChance = CalculateDodgeChance();
        float randomRoll = Random.Range(0f, 1f);
        
        Debug.Log($"Dodge decision: Roll={randomRoll:F3}, Chance={dodgeChance:F3}, " +
                  $"Cooldown={Time.time - lastDodgeTime:F1}s");
        
        if (randomRoll <= dodgeChance)
        {
            // DODGE YAP!
            ExecuteDodge();
        }
        else
        {
            // Dodge yapmadı, karar süresini sıfırla ki tekrar düşünebilsin
            attackCircleEntryTime = 0f;
        }
    }
    
    private float CalculateDodgeChance()
    {
        // Inspector'dan base dodge şansını al
        float baseChance = enemy.dodgeChanceInAttackCircle;
        
        // Mesafe faktörü - çok yakınsa daha yüksek şans
        float distance = GetDistanceToPlayer();
        float distanceFactor = 1f;
        if (distance <= 0.8f)
        {
            distanceFactor = 1.8f; // Çok yakınsa %80 bonus
        }
        else if (distance <= 1.2f)
        {
            distanceFactor = 1.5f; // Yakınsa %50 bonus
        }
        else if (distance <= 1.5f)
        {
            distanceFactor = 1.2f; // Orta yakınsa %20 bonus
        }
        
        // Hareket fazı faktörü - ilerlerken daha yüksek şans
        float phaseFactor = 1f;
        if (currentPhase == MovementPhase.Advancing)
        {
            phaseFactor = 1.4f; // İlerlerken %40 bonus
        }
        else if (currentPhase == MovementPhase.Retreating)
        {
            phaseFactor = 0.6f; // Geri çekilirken %40 azalma
        }
        
        // Pressure faktörü - baskı altındaysa daha yüksek şans
        float pressureFactor = 1f + (pressureTimer / maxPressureTime) * 0.6f;
        
        // Stamina faktörü - stamina düşükse daha düşük şans
        float staminaRatio = enemy.currentStamina / enemy.maxStamina;
        float staminaFactor = Mathf.Lerp(0.3f, 1f, staminaRatio);
        
        // Zorluk faktörü
        float difficultyFactor = 1f;
        switch (enemy.difficultyLevel)
        {
            case 1: difficultyFactor = 0.8f; break; // Kolay - %20 azalma
            case 2: difficultyFactor = 1f; break;   // Orta - normal
            case 3: difficultyFactor = 1.3f; break; // Zor - %30 artış
        }
        
        // Final hesaplama
        float finalChance = baseChance * distanceFactor * phaseFactor * pressureFactor * staminaFactor * difficultyFactor;
        
        // Maksimum %95 ile sınırla (her zaman biraz öngörülemezlik olsun)
        finalChance = Mathf.Clamp(finalChance, 0f, 0.95f);
        
        return finalChance;
    }
    
    private void ExecuteDodge()
    {
        lastDodgeTime = Time.time;
        nextState = "Dodge";
        
        Debug.Log($"DODGE EXECUTED! Distance: {GetDistanceToPlayer():F2}, " +
                  $"Phase: {currentPhase}, Pressure: {pressureTimer:F1}s");
    }
    
    #endregion
    
    private void HandleMovement()
    {
        float currentDistance = GetDistanceToPlayer();
        
        switch (currentPhase)
        {
            case MovementPhase.Advancing:
                HandleAdvancing();
                break;
                
            case MovementPhase.Retreating:
                HandleRetreating();
                break;
                
            case MovementPhase.Waiting:
                HandleWaiting();
                break;
        }
        
        // Movement süresi doldu mu?
        if (movementTimer >= currentMovementDuration)
        {
            StartPause();
        }
    }
    
    private void HandleAdvancing()
    {
        // İleri hareket - mesafe kontrolü olmadan sürekli ileri git
        float speedMultiplier = GetSpeedMultiplier() * enemy.forwardSpeedMultiplier;
        MoveTowardsPlayer(baseMovementSpeed * speedMultiplier);
        
        Debug.Log($"Advancing with speed: {baseMovementSpeed * speedMultiplier}, Distance: {GetDistanceToPlayer()}");
        
        // NOT: Mesafe kontrolü kaldırıldı - sadece timer bittiğinde duracak
    }
    
    private void HandleRetreating()
    {
        // Geri hareket - mesafe kontrolü olmadan sürekli geri git
        float speedMultiplier = GetSpeedMultiplier() * enemy.backwardSpeedMultiplier;
        MoveAwayFromPlayer(baseMovementSpeed * speedMultiplier);
        
        Debug.Log($"Retreating with speed: {baseMovementSpeed * speedMultiplier}, Distance: {GetDistanceToPlayer()}");
        
        // NOT: Mesafe kontrolü kaldırıldı - sadece timer bittiğinde duracak
    }
    
    private void HandleWaiting()
    {
        // Yerinde dur, sadece oyuncuyu izle
        FacePlayer();
        
        Debug.Log("Waiting and watching player");
    }
    
    private void StartPause()
    {
        isPaused = true;
        pauseTimer = 0f;
        SetNextPauseDuration();
        
        // Pause animasyonu
        enemy.animator?.SetFloat("MoveSpeed", 0f);
        
        Debug.Log("Enemy paused - thinking...");
    }
    
    private void HandlePause()
    {
        pauseTimer += Time.deltaTime;
        
        // Pause sırasında sadece player'ı izle
        FacePlayer();
        
        // Pause bittiğinde yeni hareket planı yap
        if (pauseTimer >= currentPauseDuration)
        {
            EndPause();
            DecideNextMovement();
        }
    }
    
    private void EndPause()
    {
        isPaused = false;
        movementTimer = 0f;
        SetNextMovementDuration();
        
        enemy.animator?.SetFloat("MoveSpeed", 0.5f);
        
        Debug.Log($"Pause ended. Deciding next movement...");
    }
    
    private void DecideNextMovement()
    {
        float currentDistance = GetDistanceToPlayer();
        float randomChoice = Random.Range(0f, 1f);
        
        // Hareket geçmişini kontrol et
        bool canAdvance = consecutiveAdvanceCount < 1; // Max 1 kez üst üste ileri
        bool canRetreat = consecutiveRetreatCount < 2; // Max 2 kez üst üste geri
        
        Debug.Log($"Current distance: {currentDistance}, Comfort min: {comfortZoneMin}, max: {comfortZoneMax}");
        
        // RASTGELE GERİ ÇEKİLME - mesafe bakmadan %40 şansla geri git
        if (randomChoice < 0.4f && canRetreat)
        {
            SwitchToRetreat();
            Debug.Log("Random retreat decision!");
            return;
        }
        
        // Zorunlu durumlar
        if (currentDistance < comfortZoneMin)
        {
            // Çok yakın - kesinlikle geri çekil
            SwitchToRetreat();
            Debug.Log("Too close, forced retreat!");
        }
        else if (currentDistance > comfortZoneMax && canAdvance)
        {
            // Çok uzak - yaklaş
            SwitchToAdvance();
            Debug.Log("Too far, advancing!");
        }
        else
        {
            // Comfort zone içinde - GERİ ÇEKİLMEYİ ÇOK FAVORİLE
            if (lastPhase == MovementPhase.Advancing)
            {
                // Son hareket ileriyse, %90 şansla geri git
                if (randomChoice < 0.9f && canRetreat)
                {
                    SwitchToRetreat();
                    Debug.Log("After advance, retreating!");
                }
                else
                {
                    SwitchToWaiting();
                    Debug.Log("After advance, waiting!");
                }
            }
            else if (lastPhase == MovementPhase.Retreating)
            {
                // Son hareket geriyse, %30 şansla ileri git, %50 şansla tekrar geri git
                if (randomChoice < 0.3f && canAdvance)
                {
                    SwitchToAdvance();
                    Debug.Log("After retreat, advancing!");
                }
                else if (randomChoice < 0.8f && canRetreat)
                {
                    SwitchToRetreat();
                    Debug.Log("After retreat, retreating again!");
                }
                else
                {
                    SwitchToWaiting();
                    Debug.Log("After retreat, waiting!");
                }
            }
            else
            {
                // Diğer durumlar - GERİ ÇEKİLMEYİ ÇOK FAVORİLE
                if (randomChoice < 0.7f && canRetreat)
                {
                    SwitchToRetreat(); // %70 geri
                    Debug.Log("Default case, retreating!");
                }
                else if (randomChoice < 0.85f && canAdvance)
                {
                    SwitchToAdvance(); // %15 ileri
                    Debug.Log("Default case, advancing!");
                }
                else
                {
                    SwitchToWaiting(); // %15 bekle
                    Debug.Log("Default case, waiting!");
                }
            }
        }
    }
    
    private void CheckStateTransitions()
    {
        float currentDistance = GetDistanceToPlayer();
        
        // Belli süre sonra saldırı kararı
        if (pressureTimer >= maxPressureTime)
        {
            if (currentDistance <= 1.5f && HasEnoughStamina(enemy.grabStaminaCost))
            {
                nextState = "Attack"; // Sonunda saldır
                return;
            }
        }
        
        // Çok yakın mesafede saldırı şansı
        if (currentDistance <= 1f && HasEnoughStamina(enemy.grabStaminaCost))
        {
            float attackChance = GetAttackDecisionChance();
            if (Random.Range(0f, 1f) < attackChance)
            {
                nextState = "Attack";
                return;
            }
        }
        
        // Stamina çok düşükse geri çekilmeye odaklan
        if (enemy.currentStamina < enemy.maxStamina * 0.2f)
        {
            if (currentPhase != MovementPhase.Retreating)
            {
                SwitchToRetreat();
            }
            return;
        }
        
        // Çok uzaklaştıysa idle'a dön
        if (currentDistance > enemy.detectionRange * 1.5f)
        {
            nextState = "Idle";
            return;
        }
    }
    
    private float GetAttackDecisionChance()
    {
        // Zorluk seviyesine ve geçen zamana göre saldırı kararı
        float baseChance;
        switch (enemy.difficultyLevel)
        {
            case 1: baseChance = 0.3f; break;
            case 2: baseChance = 0.5f; break;
            case 3: baseChance = 0.7f; break;
            default: baseChance = 0.3f; break;
        }
        
        // Zaman geçtikçe saldırı şansı artar
        float timeBonus = (pressureTimer / maxPressureTime) * 0.4f;
        return Mathf.Clamp01(baseChance + timeBonus);
    }
    
    private float GetSpeedMultiplier()
    {
        // Mesafeye göre hız ayarı - yaklaştıkça yavaşla
        float distance = GetDistanceToPlayer();
        float speedMultiplier = Mathf.Clamp(distance / idealDistance, 0.4f, 1f);
        
        // Stamina düşükse daha yavaş
        if (enemy.currentStamina < enemy.maxStamina * 0.3f)
            speedMultiplier *= 0.7f;
            
        return speedMultiplier;
    }
    
    private void UpdateAnimations()
    {
        // Hareket yönüne göre animasyon
        enemy.animator?.SetFloat("MoveDirection", currentMoveDirection);
        enemy.animator?.SetBool("IsRetreating", currentPhase == MovementPhase.Retreating);
        enemy.animator?.SetBool("IsAdvancing", currentPhase == MovementPhase.Advancing);
    }
    
    private void FacePlayer()
    {
        Vector3 direction = GetDirectionToPlayer();
        
        // SpriteRenderer kullanarak yönlendirme
        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    #region Phase Switching
    
    private void SwitchToAdvance()
    {
        lastPhase = currentPhase;
        currentPhase = MovementPhase.Advancing;
        currentMoveDirection = 1f;
        
        // Advance sayacını güncelle
        if (lastPhase == MovementPhase.Advancing)
            consecutiveAdvanceCount++;
        else
            consecutiveAdvanceCount = 1;
            
        consecutiveRetreatCount = 0;
        
        Debug.Log($"Switching to Advance (consecutive: {consecutiveAdvanceCount})");
    }
    
    private void SwitchToRetreat()
    {
        lastPhase = currentPhase;
        currentPhase = MovementPhase.Retreating;
        currentMoveDirection = -1f;
        
        // Retreat sayacını güncelle
        if (lastPhase == MovementPhase.Retreating)
            consecutiveRetreatCount++;
        else
            consecutiveRetreatCount = 1;
            
        consecutiveAdvanceCount = 0;
        
        Debug.Log($"Switching to Retreat (consecutive: {consecutiveRetreatCount})");
    }
    
    private void SwitchToWaiting()
    {
        lastPhase = currentPhase;
        currentPhase = MovementPhase.Waiting;
        currentMoveDirection = 0f;
        
        // Sayaçları sıfırla
        consecutiveAdvanceCount = 0;
        consecutiveRetreatCount = 0;
        
        Debug.Log("Switching to Waiting");
    }
    
    #endregion
    
    #region Utility Methods
    
    private void SetNextMovementDuration()
    {
        // Hareket süreleri - net ileri-geri hareketler için
        switch (enemy.difficultyLevel)
        {
            case 1:
                currentMovementDuration = Random.Range(1.5f, 3f);
                break;
            case 2:
                currentMovementDuration = Random.Range(1.2f, 2.5f);
                break;
            case 3:
                currentMovementDuration = Random.Range(1f, 2f);
                break;
        }
    }
    
    private void SetNextPauseDuration()
    {
        // Kısa pause'lar - sürekli hareket halinde
        switch (enemy.difficultyLevel)
        {
            case 1:
                currentPauseDuration = Random.Range(0.3f, 0.8f);
                break;
            case 2:
                currentPauseDuration = Random.Range(0.2f, 0.6f);
                break;
            case 3:
                currentPauseDuration = Random.Range(0.1f, 0.4f);
                break;
        }
    }
    
    #endregion
    
    public override void Exit()
    {
        base.Exit();
        
        enemy.animator?.SetBool("IsMoving", false);
        enemy.animator?.SetFloat("MoveSpeed", 0f);
        
        // Hareket geçmişini temizle
        consecutiveAdvanceCount = 0;
        consecutiveRetreatCount = 0;
        
        Debug.Log("Enemy exited Move state");
    }
    
    // Debug bilgileri için
    public float GetDodgeCooldownRemaining()
    {
        return Mathf.Max(0f, dodgeCooldown - (Time.time - lastDodgeTime));
    }
    
    public float GetAttackCircleTime()
    {
        return attackCircleEntryTime;
    }
}