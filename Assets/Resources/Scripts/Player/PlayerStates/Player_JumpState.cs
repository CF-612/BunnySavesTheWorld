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

        float jumpInitialX = player.moveInput.x * player.MoveSpd * player.InAirMoveMultiplier;
        player.SetVelocity(jumpInitialX, player.JumpForce);

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
