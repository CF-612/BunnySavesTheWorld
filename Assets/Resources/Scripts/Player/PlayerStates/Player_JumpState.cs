using UnityEngine;

public class Player_JumpState : Player_AirState
{
    // Resources 路径：BunnyJump 文件夹下的跳跃音效（前 4 个用于普通跳跃）
    private static readonly string[] JUMP_SFX_PATHS =
    {
        "Audio/SFX/BunnyJump/Jump1", "Audio/SFX/BunnyJump/Jump2", "Audio/SFX/BunnyJump/Jump3", "Audio/SFX/BunnyJump/Jump4"
    };

    /// <summary>外部传入的自定义跳跃力度，-1 表示使用默认 JumpForce</summary>
    private float customJumpForce = -1f;

    public Player_JumpState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    /// <summary>由 ChargedJumpState 调用，设置本次起跳的力度</summary>
    public void SetCustomJumpForce(float force)
    {
        customJumpForce = force;
    }

    public override void Enter()
    {
        base.Enter();

        float jumpForce = customJumpForce > 0f ? customJumpForce : player.JumpForce;
        float jumpInitialX = player.moveInput.x * player.MoveSpd * player.InAirMoveMultiplier;
        player.SetVelocity(jumpInitialX, jumpForce);

        // 立即同步 yVelocity 动画参数，防止 Jump/Air/Fall BlendTree 在进入瞬间
        // 因 yVelocity 仍为 0（上一帧残值）而短暂显示 Air 动画
        anim.SetFloat("yVelocity", rb.linearVelocity.y);

        customJumpForce = -1f;

        // 随机播放一个跳跃音效，音高随机变化
        AudioManager.Instance?.PlayRandomSFX(JUMP_SFX_PATHS, 0.6f, 0.95f, 1.05f);
    }

    public override void Update()
    {
        base.Update();

        if(rb.linearVelocity.y < 0)
            stateMachine.ChangeState(player.fallState);
    }
}
