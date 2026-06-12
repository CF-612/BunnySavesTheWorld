using UnityEngine;

public abstract class EntityState
{
    protected StateMachine stateMachine;
    protected string animBoolName;
    
    protected Animator anim;
    protected Rigidbody2D rb;
    protected float stateTimer;
    protected bool triggerCalled;

    public EntityState(StateMachine stateMachine,string animBoolName)
    {
        this.stateMachine = stateMachine;
        this.animBoolName = animBoolName;
    }

    public virtual void Enter()
    {
        if (!string.IsNullOrEmpty(animBoolName))
        {
            anim.SetBool(animBoolName, true);
        }

        triggerCalled = false;
    }

    public virtual void Update()
    {
        stateTimer -= Time.deltaTime;

        UpdateAnimationParameters();
    }

    public virtual void Exit()
    {
        if (!string.IsNullOrWhiteSpace(animBoolName))
        {
            anim.SetBool(animBoolName, false);
        }
    }

    public void AnimationTrigger()
    {
        triggerCalled = true;
    }

    // 通用的动作关键帧触发方法，供具体的子状态重写
    public virtual void AnimationActionTrigger()
    {
        
    }

    public virtual void UpdateAnimationParameters()
    {
        
    }
}
