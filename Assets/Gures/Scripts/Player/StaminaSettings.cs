using UnityEngine;

[System.Serializable]
public class StaminaSettings
{
    [Header("Stamina Stats")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 15f;
    public float staminaRegenDelay = 1f;  // Rejenerasyon gecikmesini azalttık

    [Header("Regeneration Multipliers")]
    public float idleRegenMultiplier = 1.5f;     // Idle durumunda rejenerasyon çarpanı
    public float movingRegenMultiplier = 0.3f;   // Hareket halindeyken rejenerasyon çarpanı
    public bool alwaysRegenerate = true;         // Her durumda stamina rejenere olsun

    [Header("Consumption Rates (Per Second)")]
    public float idleConsumption = 0f;     // Idle'da stamina tüketim kaldırıldı
    public float moveConsumption = 1f;
    public float dodgeConsumption = 8f;
    public float grabConsumption = 3f;

    [Header("Action Costs (Instant)")]
    public float dodgeActionCost = 25f;
    public float grabActionCost = 30f;

    [Header("Action Requirements")]
    public float minStaminaForMovement = 5f;
    public float minStaminaForActions = 25f;

    [Header("Settings")]
    public float depletionThreshold = 0f;
    public float recoveryThreshold = 15f;
    public bool enableDebugLogs = true;

    [Header("Depletion Recovery")]
    public bool allowRegenerationWhenDepleted = true; // Bittiğinde de rejenerasyon olabilsin
    public float depletedRegenMultiplier = 0.5f; // Stamina bittiğinde rejenerasyon hızını azaltma çarpanı
}

