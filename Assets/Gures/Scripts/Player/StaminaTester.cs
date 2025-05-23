using UnityEngine;

public class StaminaTester : MonoBehaviour
{
    [Header("Test Controls")]
    public KeyCode testRestoreStamina = KeyCode.R;
    public KeyCode testDrainStamina = KeyCode.T;
    public float testAmount = 20f;

    private StaminaManager staminaManager;
    private PlayerStateMachine playerStateMachine;

    void Start()
    {
        staminaManager = FindObjectOfType<StaminaManager>();
        playerStateMachine = FindObjectOfType<PlayerStateMachine>();

        if (staminaManager == null)
        {
            Debug.LogError("StaminaManager not found!");
        }

        if (playerStateMachine == null)
        {
            Debug.LogError("PlayerStateMachine not found!");
        }

        Debug.Log("=== STAMINA TEST CONTROLS ===");
        Debug.Log("R - Restore 20 stamina");
        Debug.Log("T - Drain 20 stamina");
        Debug.Log("Arrow Keys - Move player");
        Debug.Log("LeftShift - Dodge");
    }

    void Update()
    {
        if (staminaManager == null) return;

        // Test controls
        if (Input.GetKeyDown(testRestoreStamina))
        {
            staminaManager.RestoreStamina(testAmount);
            Debug.Log($"[TEST] Restored {testAmount} stamina");
        }

        if (Input.GetKeyDown(testDrainStamina))
        {
            staminaManager.ConsumeStamina(testAmount);
            Debug.Log($"[TEST] Drained {testAmount} stamina");
        }
    }

    void OnGUI()
    {
        if (staminaManager == null) return;

        // Stamina bar UI
        GUI.Box(new Rect(10, 10, 200, 30), $"Stamina: {staminaManager.CurrentStamina:F1}/{staminaManager.MaxStamina}");

        // Stamina bar
        float staminaPercent = staminaManager.StaminaPercentage;
        GUI.color = Color.green;
        if (staminaPercent < 0.5f) GUI.color = Color.yellow;
        if (staminaPercent < 0.2f) GUI.color = Color.red;

        GUI.Box(new Rect(10, 50, 200 * staminaPercent, 20), "");
        GUI.color = Color.white;

        // Current state
        if (playerStateMachine != null)
        {
            string stateName = playerStateMachine.currentState.GetType().Name;
            GUI.Box(new Rect(10, 80, 200, 25), $"State: {stateName}");
        }

        // Test controls
        GUI.Box(new Rect(10, 115, 200, 60), "TEST:\nR - Restore Stamina\nT - Drain Stamina");
    }
}