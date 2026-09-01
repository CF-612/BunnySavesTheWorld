using UnityEngine;

public class Player_idleState : Player_GroundedState
{
    public Player_idleState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
            return;

        player.ApplyHorizontalVelocity(
            0f,
            player.GroundAccel,
            player.GroundDecel,
            player.ReverseBrake);

        // 防止角色贴着墙壁时，继续按同一方向键导致播放移动动画
        if (player.moveInput.x == player.facingDir && player.isWall)
            return;
            
        // 检测到水平输入，切换为移动状态
        if (player.moveInput.x != 0)
            stateMachine.ChangeState(player.moveState);
    }
}
