using UnityEngine;

public class Player_GroundedState : PlayerState
{
    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        // 物理下落检测
        if (rb.linearVelocity.y < 0 && !player.isGround)
            stateMachine.ChangeState(player.fallState);

        // 基础跳跃指令
        if (input.Player.Jump.WasPressedThisFrame())
            stateMachine.ChangeState(player.jumpState);
       
        // 啃咬
        if (input.Player.Bite.WasPressedThisFrame())
            stateMachine.ChangeState(player.biteState);

        // 跺脚
        if (input.Player.Stomp.WasPressedThisFrame())
            stateMachine.ChangeState(player.groundStompState);

        // 刨坑 (此处可能需要结合向下方向键或特定输入)
        if (input.Player.Dig.IsPressed())
            stateMachine.ChangeState(player.digState);
    }
}
