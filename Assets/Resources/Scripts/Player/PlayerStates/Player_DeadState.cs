using UnityEngine;

public class Player_DeadState : PlayerState
{
    public Player_DeadState(Player player, StateMachine stateMachine, string animBoolName)
        : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 禁用输入，防止死亡动画期间玩家操作
        input.Disable();

        // 停止物理模拟，让死亡动画原地播放
        rb.simulated = false;

        // 锁定状态机，阻止其他状态切入
        stateMachine.SwitchOffStateMachine();
    }

    public override void Update()
    {
        base.Update();
        // 动画播放由 Animator 控制，重生流程由 LevelManager 接管
    }

    public override void AnimationActionTrigger()
    {
        base.AnimationActionTrigger();

        // 动画关键帧触发：在玩家位置生成落地烟雾特效
        if (player.deathSmokeVFX != null)
        {
            Object.Instantiate(player.deathSmokeVFX, player.transform.position, Quaternion.identity);
        }
    }
}
