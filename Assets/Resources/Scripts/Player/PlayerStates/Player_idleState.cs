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

        // 防止角色贴着墙壁时，继续按同一方向键导致播放移动动画
        bool isBlockedByWall = player.moveInput.x == player.facingDir && player.isWall;

        // 检测到水平输入，切换为移动状态
        if (player.moveInput.x != 0 && !isBlockedByWall)
        {
            stateMachine.ChangeState(player.moveState);
            return;
        }

        // Idle 只表示没有有效移动输入；活动风区仍可以推动角色。
        float targetSpd = player.GetGroundTargetSpd(0f);
        if (player.windReceiver != null && player.windReceiver.IsBeingBlown)
            player.SetVelocity(targetSpd, rb.linearVelocity.y);
        else
            player.ApplyHorizontalVelocity(
                targetSpd,
                player.GroundAccel,
                player.GroundDecel,
                player.ReverseBrake);
    }
}
