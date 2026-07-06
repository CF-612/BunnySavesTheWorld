using UnityEngine;

public class Player_ChargedJumpState : Player_GroundedState
{
    private float chargeTimer;
    private float maxChargeTime;

    public Player_ChargedJumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        chargeTimer = 0f;
        maxChargeTime = player.MaxChargeTime;

        // 蓄力期间水平速度归零，仅保留朝向输入用于决定起跳方向
        player.SetVelocity(0f, rb.linearVelocity.y);
    }

    public override void Update()
    {
        // 不调用 base.Update()，因为蓄力期间不需要下落检测和其他地面操作
        // 仅更新计时器和动画参数
        stateTimer -= Time.deltaTime;
        UpdateAnimationParameters();

        chargeTimer += Time.deltaTime;

        // 蓄力期间角色保持静止（不响应移动输入）
        // 但翻转朝向以匹配玩家意图的起跳方向
        if (player.moveInput.x != 0)
            player.HandleFlip(player.moveInput.x);

        // 物理下落检测 — 蓄力中离地则取消蓄力
        if (rb.linearVelocity.y < 0 && !player.isGround)
        {
            stateMachine.ChangeState(player.fallState);
            return;
        }

        // 松手 → 执行蓄力跳跃
        if (!input.Player.Jump.IsPressed())
        {
            float chargeProgress = Mathf.Clamp01(chargeTimer / maxChargeTime);
            float force = Mathf.Lerp(player.JumpForce, player.MaxChargeJumpForce, chargeProgress);

            player.jumpState.SetCustomJumpForce(force);
            stateMachine.ChangeState(player.jumpState);
        }
    }
}
