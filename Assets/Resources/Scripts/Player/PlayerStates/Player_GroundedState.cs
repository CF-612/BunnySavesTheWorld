using UnityEngine;

public class Player_GroundedState : PlayerState
{
    private float jumpPressTimer;
    private bool isMonitoringJump;

    public Player_GroundedState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        isMonitoringJump = false;
        jumpPressTimer = 0f;
    }

    public override void Update()
    {
        base.Update();

        // 物理下落检测
        if (rb.linearVelocity.y < 0 && !player.isGround)
            stateMachine.ChangeState(player.fallState);

        // —— 跳跃输入：区分"轻点=普通跳"与"长按=蓄力跳" ——
        if (input.Player.Jump.WasPressedThisFrame())
        {
            jumpPressTimer = 0f;
            isMonitoringJump = true;
        }

        if (isMonitoringJump && input.Player.Jump.IsPressed())
        {
            jumpPressTimer += Time.deltaTime;
            if (jumpPressTimer >= player.ChargeThreshold)
            {
                isMonitoringJump = false;
                stateMachine.ChangeState(player.chargedJumpState);
                return;
            }
        }

        if (isMonitoringJump && !input.Player.Jump.IsPressed())
        {
            // 阈值前松手 → 普通跳跃
            isMonitoringJump = false;
            stateMachine.ChangeState(player.jumpState);
            return;
        }

        // 啃咬
        if (input.Player.Bite.WasPressedThisFrame())
            stateMachine.ChangeState(player.biteState);

        // 跺脚
        if (input.Player.Stomp.WasPressedThisFrame())
            stateMachine.ChangeState(player.groundStompState);

        // 单向平台下落（S 键）
        if (input.Player.JumpDown.WasPressedThisFrame())
            player.TryDropThroughPlatform();

        // 刨坑 (此处可能需要结合向下方向键或特定输入)
        if (input.Player.Dig.IsPressed())
            stateMachine.ChangeState(player.digState);
    }
}
