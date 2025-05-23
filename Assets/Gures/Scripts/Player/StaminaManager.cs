using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaminaManager : MonoBehaviour
{
    [SerializeField] private StaminaSettings settings;

    [Header("Current Status (Read Only)")]
    [SerializeField] private float currentStamina;
    [SerializeField] private bool isRegeneratingStamina = false;
    [SerializeField] private bool isStaminaDepleted = false;

    // Yeni - Idle durumunda olup olmadığını takip etmek için
    [SerializeField] private bool isPlayerIdle = false;
    [SerializeField] private bool isPlayerMoving = false;  // Hareket halinde olup olmadığını takip etmek için

    // Timer values
    private float staminaRegenTimer;
    private float lastDebugTime;

    // Events
    public System.Action<float, float> OnStaminaChanged; // current, max
    public System.Action OnStaminaDepleted;
    public System.Action OnStaminaRecovered;

    // Properties
    public float CurrentStamina => currentStamina;
    public float MaxStamina => settings.maxStamina;
    public float StaminaPercentage => currentStamina / settings.maxStamina;
    public bool IsStaminaDepleted => isStaminaDepleted;

    void Awake()
    {
        // Initialize stamina to maximum
        currentStamina = settings.maxStamina;
        lastDebugTime = Time.time;

        Debug.Log($"[Stamina] Initialized with {currentStamina}/{settings.maxStamina} stamina");
    }

    void Start()
    {
        // Double check stamina initialization
        if (currentStamina <= 0)
        {
            currentStamina = settings.maxStamina;
            Debug.Log($"[Stamina] Fixed initialization - Set to {currentStamina}");
        }
    }

    void Update()
    {
        HandleStaminaRegeneration();
        CheckStaminaDepletion();
    }

    void HandleStaminaRegeneration()
    {
        // Eğer stamina zaten maksimumsa, işlem yapma
        if (currentStamina >= settings.maxStamina)
        {
            if (isRegeneratingStamina)
            {
                isRegeneratingStamina = false;
                if (settings.enableDebugLogs)
                    Debug.Log("[Stamina] Regeneration stopped - Full stamina");
            }
            return;
        }

        // Her zaman rejenere olma özelliği kapalıysa ve hareket halindeyse, rejenerasyon yapma
        if (!settings.alwaysRegenerate && isPlayerMoving)
        {
            isRegeneratingStamina = false;
            staminaRegenTimer = 0f;
            return;
        }

        // Stamina bittiğinde regeneration kontrolü
        if (isStaminaDepleted && !settings.allowRegenerationWhenDepleted)
        {
            // Eğer stamina bittiyse ve bittiğinde rejenerasyon kapalıysa, rejenerasyonu durdur
            return;
        }

        if (!isRegeneratingStamina)
        {
            staminaRegenTimer += Time.deltaTime;
            if (staminaRegenTimer >= settings.staminaRegenDelay)
            {
                isRegeneratingStamina = true;
                if (settings.enableDebugLogs)
                    Debug.Log("[Stamina] Regeneration started");
            }
        }
        else
        {
            float regenRate = settings.staminaRegenRate;

            // Duruma göre rejenerasyon çarpanını uygula
            if (isPlayerIdle)
            {
                regenRate *= settings.idleRegenMultiplier;
            }
            else if (isPlayerMoving && settings.alwaysRegenerate)
            {
                regenRate *= settings.movingRegenMultiplier;
            }

            // Stamina bittiğinde rejenerasyon hızını azalt
            if (isStaminaDepleted)
            {
                regenRate *= settings.depletedRegenMultiplier;
            }

            float oldStamina = currentStamina;
            currentStamina += regenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, settings.maxStamina);

            // Trigger event if stamina changed significantly
            if (Mathf.Abs(currentStamina - oldStamina) > 0.1f)
            {
                OnStaminaChanged?.Invoke(currentStamina, settings.maxStamina);

                if (settings.enableDebugLogs && Time.time - lastDebugTime >= 0.5f)
                {
                    Debug.Log($"[Stamina] REGENERATING: Rate={regenRate}/sec | Current={currentStamina:F1}/{settings.maxStamina}");
                    lastDebugTime = Time.time;
                }
            }
        }
    }

    void CheckStaminaDepletion()
    {
        // Check for depletion
        if (currentStamina <= settings.depletionThreshold && !isStaminaDepleted)
        {
            isStaminaDepleted = true;
            OnStaminaDepleted?.Invoke();
            if (settings.enableDebugLogs)
                Debug.Log("[Stamina] DEPLETED! Player exhausted!");
        }
        // Check for recovery
        else if (currentStamina >= settings.recoveryThreshold && isStaminaDepleted)
        {
            isStaminaDepleted = false;
            OnStaminaRecovered?.Invoke();
            if (settings.enableDebugLogs)
                Debug.Log("[Stamina] Recovered from depletion");
        }
    }

    // Public Methods
    public bool CanUseStamina(float cost)
    {
        bool canUse = currentStamina >= cost && !isStaminaDepleted;
        if (!canUse && settings.enableDebugLogs)
        {
            Debug.Log($"[Stamina] Cannot use {cost} stamina - Current: {currentStamina:F1}, Depleted: {isStaminaDepleted}");
        }
        return canUse;
    }

    public bool CanMove()
    {
        bool canMove = currentStamina >= settings.minStaminaForMovement && !isStaminaDepleted;
        if (!canMove && settings.enableDebugLogs)
        {
            Debug.Log($"[Stamina] Cannot move - Need {settings.minStaminaForMovement}, Have: {currentStamina:F1}");
        }
        return canMove;
    }

    public bool CanPerformAction(float actionCost)
    {
        bool canPerform = currentStamina >= actionCost && currentStamina >= settings.minStaminaForActions && !isStaminaDepleted;
        if (!canPerform && settings.enableDebugLogs)
        {
            Debug.Log($"[Stamina] Cannot perform action - Need {actionCost}, Have: {currentStamina:F1}, Min Required: {settings.minStaminaForActions}");
        }
        return canPerform;
    }

    public bool ConsumeStamina(float cost)
    {
        if (!CanUseStamina(cost))
            return false;

        float oldStamina = currentStamina;
        currentStamina -= cost;
        currentStamina = Mathf.Clamp(currentStamina, 0, settings.maxStamina);

        // Reset regeneration
        ResetRegeneration();

        OnStaminaChanged?.Invoke(currentStamina, settings.maxStamina);

        if (settings.enableDebugLogs)
            Debug.Log($"[Stamina] Used {cost:F1} stamina. Remaining: {currentStamina:F1}/{settings.maxStamina}");

        return true;
    }

    public void ConsumeStaminaOverTime(float consumptionRate)
    {
        if (currentStamina <= 0)
        {
            return;
        }

        if (consumptionRate <= 0)
        {
            return;
        }

        float consumption = consumptionRate * Time.deltaTime;
        float oldStamina = currentStamina;
        currentStamina -= consumption;
        currentStamina = Mathf.Clamp(currentStamina, 0, settings.maxStamina);

        // Reset regeneration timer when consuming
        if (consumption > 0)
        {
            ResetRegeneration();
        }

        // Debug log for consumption - sadece belirgin değişikliklerde
        if (settings.enableDebugLogs && consumption > 0 && Time.time - lastDebugTime >= 0.5f)
        {
            Debug.Log($"[Stamina] CONSUMING: Rate={consumptionRate}/sec | Current={currentStamina:F1}/{settings.maxStamina}");
            lastDebugTime = Time.time;
        }

        // Trigger event if stamina changed
        OnStaminaChanged?.Invoke(currentStamina, settings.maxStamina);
    }

    public void RestoreStamina(float amount)
    {
        float oldStamina = currentStamina;
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, settings.maxStamina);

        OnStaminaChanged?.Invoke(currentStamina, settings.maxStamina);

        if (settings.enableDebugLogs)
            Debug.Log($"[Stamina] Restored {amount:F1} stamina. Current: {currentStamina:F1}/{settings.maxStamina}");
    }

    public void SetStamina(float value)
    {
        currentStamina = Mathf.Clamp(value, 0, settings.maxStamina);
        OnStaminaChanged?.Invoke(currentStamina, settings.maxStamina);
    }

    // Yeni metod - Idle durumunu bildirmek için
    public void SetPlayerIdle(bool isIdle)
    {
        isPlayerIdle = isIdle;

        // Idle ise moving değil
        if (isIdle)
        {
            isPlayerMoving = false;
        }

        if (isIdle && settings.enableDebugLogs)
        {
            Debug.Log("[Stamina] Player is idle - Enhanced regeneration active");
        }
    }

    // Yeni metod - Hareket durumunu bildirmek için
    public void SetPlayerMoving(bool isMoving)
    {
        isPlayerMoving = isMoving;

        // Moving ise idle değil
        if (isMoving)
        {
            isPlayerIdle = false;
        }

        if (isMoving && settings.enableDebugLogs)
        {
            Debug.Log("[Stamina] Player is moving - Reduced regeneration active");
        }
    }

    void ResetRegeneration()
    {
        staminaRegenTimer = 0f;
        isRegeneratingStamina = false;
    }

    // Getter methods for consumption rates
    public float GetIdleConsumption() => settings.idleConsumption;
    public float GetMoveConsumption() => settings.moveConsumption;
    public float GetDodgeConsumption() => settings.dodgeConsumption;
    public float GetGrabConsumption() => settings.grabConsumption;

    // Getter methods for action costs
    public float GetDodgeActionCost() => settings.dodgeActionCost;
    public float GetGrabActionCost() => settings.grabActionCost;

    // Debug method
    [ContextMenu("Debug Stamina Info")]
    void DebugStaminaInfo()
    {
        Debug.Log($"=== STAMINA DEBUG INFO ===");
        Debug.Log($"Current: {currentStamina:F1}/{settings.maxStamina}");
        Debug.Log($"Percentage: {StaminaPercentage:P1}");
        Debug.Log($"Is Depleted: {isStaminaDepleted}");
        Debug.Log($"Is Regenerating: {isRegeneratingStamina}");
        Debug.Log($"Is Player Idle: {isPlayerIdle}");
        Debug.Log($"Is Player Moving: {isPlayerMoving}");
        Debug.Log($"Regen Timer: {staminaRegenTimer:F1}s");
    }
}
