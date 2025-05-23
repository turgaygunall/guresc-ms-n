using UnityEngine;

public class EnemyDodgeState : EnemyStateBase
{
    [Header("Dodge Execution")]
    private float dodgeSpeed;
    private float targetDodgeDistance;
    private float maxDodgeDuration;
    private float postDodgeWait;
    
    // Dodge tracking
    private float dodgeTimer = 0f;
    private Vector3 dodgeStartPosition;
    private Vector3 dodgeTargetPosition;
    private float totalDodgeDistance = 0f;
    
    // Dodge phases
    private bool isExecutingDodge = true;
    private bool isWaitingAfterDodge = false;
    private float waitTimer = 0f;
    
    // Movement tracking
    private float lastDistanceToPlayer = 0f;
    private int stuckFrameCount = 0;
    private const int maxStuckFrames = 10; // Eğer 10 frame takılırsa zorla bitir
    
    public EnemyDodgeState(Enemy enemy) : base(enemy)
    {
        // Inspector'dan parametreleri al
        dodgeSpeed = enemy.dodgeSpeed * enemy.dodgeSpeedMultiplier;
        targetDodgeDistance = enemy.dodgeDistance * enemy.dodgeDistanceMultiplier;
        maxDodgeDuration = enemy.dodgeDuration;
        postDodgeWait = enemy.postDodgeWaitTime;
        
        // Zorluk seviyesine göre fine-tuning
        switch (enemy.difficultyLevel)
        {
            case 1: // Kolay - biraz daha yavaş
                dodgeSpeed *= 0.85f;
                targetDodgeDistance *= 0.9f;
                postDodgeWait *= 1.2f;
                break;
                
            case 2: // Orta - normal
                // Inspector değerlerini kullan
                break;
                
            case 3: // Zor - daha hızlı ve agresif
                dodgeSpeed *= 1.15f;
                targetDodgeDistance *= 1.1f;
                postDodgeWait *= 0.8f;
                break;
        }
        
        // Backward speed multiplier'ı uygula
        dodgeSpeed *= enemy.backwardSpeedMultiplier;
        
        Debug.Log($"Dodge configured - Speed: {dodgeSpeed:F1}, Distance: {targetDodgeDistance:F1}, Duration: {maxDodgeDuration:F1}");
    }
    
    public override void Enter()
    {
        base.Enter();
        
        // Başlangıç pozisyonunu kaydet
        dodgeStartPosition = enemy.transform.position;
        lastDistanceToPlayer = GetDistanceToPlayer();
        
        // Hedef pozisyonu hesapla (player'dan uzaklaşma yönünde)
        CalculateDodgeTarget();
        
        // Timer'ları sıfırla
        dodgeTimer = 0f;
        waitTimer = 0f;
        totalDodgeDistance = 0f;
        stuckFrameCount = 0;
        
        // Flags
        isExecutingDodge = true;
        isWaitingAfterDodge = false;
        
        // Animasyonları başlat
        enemy.animator?.SetBool("IsDodging", true);
        enemy.animator?.SetBool("IsMoving", false);
        enemy.animator?.SetBool("IsIdle", false);
        enemy.animator?.SetTrigger("StartDodge");
        
        // Stamina tüket
        if (enemy.currentStamina >= enemy.dodgeStaminaCost)
        {
            enemy.currentStamina -= enemy.dodgeStaminaCost;
            enemy.OnStaminaChanged?.Invoke();
        }
        
        // Player'a bak
        FacePlayer();
        
        Debug.Log($"DODGE STARTED! From: {dodgeStartPosition} To: {dodgeTargetPosition}, Target Distance: {targetDodgeDistance:F2}");
    }
    
    public override void Update()
    {
        // Ölü veya tutulmuş kontrolü
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
        
        // Ana dodge mantığı
        if (isExecutingDodge)
        {
            HandleDodgeExecution();
        }
        else if (isWaitingAfterDodge)
        {
            HandlePostDodgeWait();
        }
        
        // Player'a sürekli bak
        FacePlayer();
        
        // Animasyon güncelleme
        UpdateDodgeAnimations();
    }
    
    private void CalculateDodgeTarget()
    {
        // Player'dan uzaklaşma yönünü hesapla
        Vector3 playerDirection = GetDirectionToPlayer();
        Vector3 awayDirection = -playerDirection;
        
        // Sadece X ekseninde hareket (2D sidescroller)
        awayDirection.y = 0f;
        awayDirection.z = 0f;
        awayDirection = awayDirection.normalized;
        
        // Hedef pozisyon
        dodgeTargetPosition = dodgeStartPosition + (awayDirection * targetDodgeDistance);
        
        Debug.Log($"Dodge direction calculated - Away: {awayDirection}, Target: {dodgeTargetPosition}");
    }
    
    private void HandleDodgeExecution()
    {
        dodgeTimer += Time.deltaTime;
        Vector3 currentPosition = enemy.transform.position;
        
        // Hızlı ve agresif hareket
        Vector3 moveDirection = (dodgeTargetPosition - currentPosition).normalized;
        float moveDistance = dodgeSpeed * Time.deltaTime;
        
        // Pozisyonu güncelle
        Vector3 newPosition = currentPosition + (moveDirection * moveDistance);
        enemy.transform.position = newPosition;
        
        // Toplam mesafeyi güncelle
        totalDodgeDistance += moveDistance;
        
        // Hedef mesafeye ulaştı mı?
        float remainingDistance = Vector3.Distance(currentPosition, dodgeTargetPosition);
        bool reachedTarget = remainingDistance <= 0.1f;
        bool travelledEnough = totalDodgeDistance >= targetDodgeDistance;
        
        // Takılma kontrolü
        float currentDistanceToPlayer = GetDistanceToPlayer();
        if (Mathf.Abs(currentDistanceToPlayer - lastDistanceToPlayer) < 0.01f)
        {
            stuckFrameCount++;
        }
        else
        {
            stuckFrameCount = 0;
            lastDistanceToPlayer = currentDistanceToPlayer;
        }
        
        // Bitirme koşulları
        bool timeUp = dodgeTimer >= maxDodgeDuration;
        bool stuckTooLong = stuckFrameCount >= maxStuckFrames;
        bool exitedAttackCircle = !enemy.isPlayerInAttackCircle;
        
        // Debug bilgileri
        Debug.Log($"Dodge progress - Distance: {totalDodgeDistance:F2}/{targetDodgeDistance:F2}, " +
                  $"Remaining: {remainingDistance:F2}, Time: {dodgeTimer:F2}/{maxDodgeDuration:F2}, " +
                  $"Exited Circle: {exitedAttackCircle}");
        
        if (reachedTarget || travelledEnough || timeUp || stuckTooLong || exitedAttackCircle)
        {
            CompleteDodgeExecution();
        }
    }
    
    private void CompleteDodgeExecution()
    {
        isExecutingDodge = false;
        isWaitingAfterDodge = true;
        waitTimer = 0f;
        
        // Dodge tamamlanma animasyonu
        enemy.animator?.SetTrigger("EndDodge");
        
        Debug.Log($"DODGE EXECUTION COMPLETED! Total distance: {totalDodgeDistance:F2}, Time: {dodgeTimer:F2}");
    }
    
    private void HandlePostDodgeWait()
    {
        waitTimer += Time.deltaTime;
        
        // Yerinde dur, sadece player'ı izle
        // Hiç hareket etme
        
        if (waitTimer >= postDodgeWait)
        {
            CompleteDodge();
        }
    }
    
    private void CompleteDodge()
    {
        // Dodge tamamen bitti, Move state'ine dön
        nextState = "Move";
        
        float finalDistance = GetDistanceToPlayer();
        bool successfulEscape = finalDistance > enemy.attackCircleRadius + 0.5f;
        
        Debug.Log($"DODGE COMPLETED! Final distance: {finalDistance:F2}, " +
                  $"Attack radius: {enemy.attackCircleRadius:F2}, " +
                  $"Successful escape: {successfulEscape}");
    }
    
    private void FacePlayer()
    {
        Vector3 direction = GetDirectionToPlayer();
        
        // Player'a bak ama geri geri git
        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    private void UpdateDodgeAnimations()
    {
        // Dodge hızı için animator parametresi
        float speedRatio = dodgeSpeed / enemy.dodgeSpeed;
        enemy.animator?.SetFloat("DodgeSpeed", speedRatio);
        
        // Dodge progress (0-1 arası)
        float distanceProgress = Mathf.Clamp01(totalDodgeDistance / targetDodgeDistance);
        float timeProgress = Mathf.Clamp01(dodgeTimer / maxDodgeDuration);
        float overallProgress = Mathf.Max(distanceProgress, timeProgress);
        
        enemy.animator?.SetFloat("DodgeProgress", overallProgress);
        
        // Dodge direction (her zaman geri)
        enemy.animator?.SetFloat("DodgeDirection", -1f);
        
        // Execution phase
        enemy.animator?.SetBool("DodgeExecuting", isExecutingDodge);
        enemy.animator?.SetBool("DodgeWaiting", isWaitingAfterDodge);
    }
    
    public override void Exit()
    {
        base.Exit();
        
        // Dodge animasyonlarını temizle
        enemy.animator?.SetBool("IsDodging", false);
        enemy.animator?.SetBool("DodgeExecuting", false);
        enemy.animator?.SetBool("DodgeWaiting", false);
        enemy.animator?.SetFloat("DodgeSpeed", 0f);
        enemy.animator?.SetFloat("DodgeDirection", 0f);
        enemy.animator?.SetFloat("DodgeProgress", 0f);
        
        Debug.Log("Enemy exited Dodge state");
    }
    
    // Debug ve monitoring için public methodlar
    public float GetDodgeProgress()
    {
        return Mathf.Clamp01(totalDodgeDistance / targetDodgeDistance);
    }
    
    public float GetTimeProgress()
    {
        return Mathf.Clamp01(dodgeTimer / maxDodgeDuration);
    }
    
    public bool IsExecuting()
    {
        return isExecutingDodge;
    }
    
    public bool IsWaiting()
    {
        return isWaitingAfterDodge;
    }
    
    public float GetTotalDistanceTravelled()
    {
        return totalDodgeDistance;
    }
    
    // Acil durumlar için zorla bitirme
    public void ForceCompleteDodge()
    {
        if (isExecutingDodge)
        {
            CompleteDodgeExecution();
        }
        else if (isWaitingAfterDodge)
        {
            CompleteDodge();
        }
    }
}