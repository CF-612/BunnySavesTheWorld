using UnityEngine;

public class Player_FallState : Player_AirState
{
    private float fallStartY;

    public Player_FallState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 记录坠落起始高度
        fallStartY = player.transform.position.y;
    }

    public override void Update()
    {
        base.Update();

        if (player.isGround)
        {
            // 计算坠落距离
            float fallDistance = fallStartY - player.transform.position.y;

            if (fallDistance >= player.fallDeathHeight)
            {
                // 过高坠落 → 触发死亡 → EntityDeath() 中调用 OnPlayerDeath 事件并切换 DeadState
                player.EntityDeath();
            }
            else
            {
                // 正常高度 → 返回闲置状态
                stateMachine.ChangeState(player.idleState);
            }
        }
    }
}
