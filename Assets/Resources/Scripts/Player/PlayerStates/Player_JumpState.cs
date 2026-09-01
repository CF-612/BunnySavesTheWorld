using UnityEngine;

public class Player_JumpState : Player_AirState
{
    // Resources 路径：BunnyJump 文件夹下的跳跃音效（前 4 个用于普通跳跃）
    private static readonly string[] JUMP_SFX_PATHS =
    {
        "Audio/SFX/BunnyJump/Jump1", "Audio/SFX/BunnyJump/Jump2", "Audio/SFX/BunnyJump/Jump3", "Audio/SFX/BunnyJump/Jump4"
    };

    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        player.SetVelocity(rb.linearVelocity.x, player.JumpForce);

        // 立即同步 yVelocity 动画参数，防止 Jump/Air/Fall BlendTree 在进入瞬间
        // 因 yVelocity 仍为 0（上一帧残值）而短暂显示 Air 动画
        anim.SetFloat("yVelocity", rb.linearVelocity.y);

        // 缓冲跳触发时玩家可能已经松开按键，此时直接按小跳处理。
        if (!input.Player.Jump.IsPressed())
            ApplyJumpCut();

        // 随机播放一个跳跃音效，音高随机变化
        AudioManager.Instance?.PlayRandomSFX(JUMP_SFX_PATHS, 0.6f, 0.95f, 1.05f);
    }

    public override void Update()
    {
        base.Update();

        if (stateMachine.CurrentState != this)
            return;

        if (input.Player.Jump.WasReleasedThisFrame())
            ApplyJumpCut();

        if (rb.linearVelocity.y < 0)
            stateMachine.ChangeState(player.fallState);
    }

    private void ApplyJumpCut()
    {
        if (rb.linearVelocity.y <= 0f)
            return;

        float cutVelocity = rb.linearVelocity.y * Mathf.Clamp01(player.JumpCutMultiplier);
        player.SetVelocity(rb.linearVelocity.x, cutVelocity);
        anim.SetFloat("yVelocity", rb.linearVelocity.y);
    }
}
