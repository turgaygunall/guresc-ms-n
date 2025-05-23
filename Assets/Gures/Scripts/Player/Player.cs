public abstract class PlayerState
{
    protected PlayerStateMachine player;
    
    public PlayerState(PlayerStateMachine playerStateMachine)
    {
        player = playerStateMachine;
    }
    
    public abstract void EnterState();
    public abstract void UpdateState();
    public abstract void ExitState();
}