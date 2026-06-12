using UnityEngine;

public class StateMachine
{
    public EntityState CurrentState { get; private set; } 
    public bool CanChangeState = true;

    public void Initialize(EntityState startState)
    {
        CurrentState = startState;
        CurrentState.Enter();
    }

    public void ChangeState(EntityState nowState)
    {
        if(CanChangeState == false)
            return;

        CurrentState.Exit();
        CurrentState = nowState;
        CurrentState.Enter();
    }

    public void UpdateActiveState()
    {
        CurrentState.Update();
    }

    public void SwitchOffStateMachine() => CanChangeState = false;
}
