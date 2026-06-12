using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    public Player_MoveState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        // 没有水平输入，或者碰到墙壁时，切换回闲置状态
        if (player.moveInput.x == 0 || player.isWall)
            stateMachine.ChangeState(player.idleState);

        // 执行水平移动
        player.SetVelocity(player.moveInput.x * player.MoveSpd, rb.linearVelocity.y);
    }
}
