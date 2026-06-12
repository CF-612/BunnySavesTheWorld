using UnityEngine;

public class Player_BiteState : Player_GroundedState
{
    private bool biteInputQueued;

    public Player_BiteState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        biteInputQueued = false;
        
        // 进入啃咬状态时，剥夺移动能力，角色原地停住
        player.SetVelocity(0, rb.linearVelocity.y);
    }

    public override void Update()
    {
        base.Update();

        // 在动画播放期间，如果玩家再次按下啃咬键，则缓存该指令
        if (input.Player.Bite.WasPressedThisFrame())
        {
            biteInputQueued = true;
        }

        // triggerCalled 由动画最后一帧的 Animation Event 修改为 true
        if (triggerCalled)
        {
            if (biteInputQueued)
            {
                // 如果有排队的输入，重新进入啃咬状态实现连咬
                stateMachine.ChangeState(player.biteState);
            }
            else
            {
                // 如果没有新指令，返回闲置状态
                stateMachine.ChangeState(player.idleState);
            }
        }
    }

    // 重写基类的通用动作触发方法
    public override void AnimationActionTrigger()
    {
        base.AnimationActionTrigger();
        
        // 在 Animator 中调用 ActionTrigger() 的那一帧，会自动执行这里的物理啃咬检测
        player.DetectBiteable();
    }
}
