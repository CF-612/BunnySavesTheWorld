using UnityEngine;

public class Player_GroundedState : PlayerState
{
    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        if (player.isGround)
            player.RefreshGroundJumpWindow();

        if (input.Player.Jump.WasPressedThisFrame())
            player.RecordJumpInput();

        if (player.TryConsumeGroundJump())
        {
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        // 先让本帧输入有机会使用土狼时间，再进入下落状态。
        if (rb.linearVelocity.y < 0 && !player.isGround)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        // 啃咬
        if (input.Player.Bite.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.biteState);
            return;
        }

        // 跺脚
        if (input.Player.Stomp.WasPressedThisFrame())
        {
            stateMachine.ChangeState(player.groundStompState);
            return;
        }

        // 单向平台下落（S 键）
        if (input.Player.JumpDown.WasPressedThisFrame())
            player.TryDropThroughPlatform();

        // 刨坑 (此处可能需要结合向下方向键或特定输入)
        if (input.Player.Dig.IsPressed())
            stateMachine.ChangeState(player.digState);
    }
}
