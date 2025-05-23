using System.Collections.Generic;
using UnityEngine;

public class EnemyStateMachine
{
    private Dictionary<string, IEnemyState> states;
    private IEnemyState currentState;
    private string currentStateName;
    
    public string CurrentStateName => currentStateName;
    public IEnemyState CurrentState => currentState;
    
    public EnemyStateMachine()
    {
        states = new Dictionary<string, IEnemyState>();
    }
    
    public void AddState(string stateName, IEnemyState state)
    {
        if (!states.ContainsKey(stateName))
        {
            states.Add(stateName, state);
        }
        else
        {
            Debug.LogWarning($"State '{stateName}' already exists in the state machine!");
        }
    }
    
    public void ChangeState(string newStateName)
    {
        // Mevcut state'den çık
        if (currentState != null)
        {
            currentState.Exit();
        }
        
        // Yeni state'e geç
        if (states.ContainsKey(newStateName))
        {
            currentState = states[newStateName];
            currentStateName = newStateName;
            currentState.Enter();
            
            Debug.Log($"State changed to: {newStateName}");
        }
        else
        {
            Debug.LogError($"State '{newStateName}' not found in state machine!");
        }
    }
    
    public void Update()
    {
        if (currentState != null)
        {
            // Mevcut state'i güncelle
            currentState.Update();
            
            // State geçiş kontrolü
            string nextState = currentState.GetNextState();
            if (!string.IsNullOrEmpty(nextState) && nextState != currentStateName)
            {
                ChangeState(nextState);
            }
        }
    }
    
    public void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.FixedUpdate();
        }
    }
    
    public bool IsInState(string stateName)
    {
        return currentStateName == stateName;
    }
    
    public void RemoveState(string stateName)
    {
        if (states.ContainsKey(stateName))
        {
            // Eğer şu anki state siliniyorsa, idle'a geç
            if (currentStateName == stateName)
            {
                ChangeState("Idle");
            }
            
            states.Remove(stateName);
        }
    }
    
    public void ClearAllStates()
    {
        if (currentState != null)
        {
            currentState.Exit();
        }
        
        states.Clear();
        currentState = null;
        currentStateName = string.Empty;
    }
}

// State interface - tüm state'ler bunu implement eder
public interface IEnemyState
{
    void Enter();
    void Update();
    void FixedUpdate();
    void Exit();
    string GetNextState();
}

// Base abstract state class - ortak özellikleri içerir
public abstract class EnemyStateBase : IEnemyState
{
    protected Enemy enemy;
    protected string nextState = "";
    
    public EnemyStateBase(Enemy enemy)
    {
        this.enemy = enemy;
    }
    
    public virtual void Enter()
    {
        nextState = "";
    }
    
    public abstract void Update();
    
    public virtual void FixedUpdate()
    {
        // Base implementation - boş
    }
    
    public virtual void Exit()
    {
        // Base implementation - boş
    }
    
    public string GetNextState()
    {
        return nextState;
    }
    
    // Utility methods - tüm state'ler kullanabilir
    protected bool IsPlayerInRange(float range)
    {
        return enemy.IsPlayerInRange(range);
    }
    
    protected float GetDistanceToPlayer()
    {
        return enemy.GetDistanceToPlayer();
    }
    
    protected Vector3 GetDirectionToPlayer()
    {
        return enemy.GetDirectionToPlayer();
    }
    
    protected bool HasEnoughStamina(float requiredStamina)
    {
        return enemy.HasEnoughStamina(requiredStamina);
    }
    
    
    
    protected void MoveTowardsPlayer(float speed)
    {
        if (enemy.player == null) return;
        
        Vector3 direction = GetDirectionToPlayer();
        enemy.transform.position += direction * speed * Time.deltaTime;
        
        // Yönlendirme için SpriteRenderer kullan
        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = direction.x < 0;
        }
    }
    
    protected void MoveAwayFromPlayer(float speed)
    {
        if (enemy.player == null) return;
        
        Vector3 playerDirection = GetDirectionToPlayer();
        Vector3 awayDirection = -playerDirection; // Player'dan uzaklaşma yönü
        
        // Pozisyonu güncelle - uzaklaşma yönünde hareket et
        enemy.transform.position += awayDirection * speed * Time.deltaTime;
        
        // Yönlendirme - player'a baksın ama geri geri gitsin
        SpriteRenderer spriteRenderer = enemy.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = playerDirection.x < 0;
        }
        
        // Debug - geri gitme kontrolü
        Debug.Log($"Moving away from player. Away direction: {awayDirection}, Speed: {speed}");
    }
}