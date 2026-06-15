using UnityEngine;

public class Player_MoveState : Player_GroundedState
{
    // Resources 路径：跑步脚步声（与跳跃共用 BunnyJump/Jump1~4）
    private static readonly string[] RUN_SFX_PATHS =
    {
        "Audio/SFX/BunnyJump/Jump1", "Audio/SFX/BunnyJump/Jump2", "Audio/SFX/BunnyJump/Jump3", "Audio/SFX/BunnyJump/Jump4"
    };

    private float footstepTimer;
    private const float FOOTSTEP_INTERVAL_MIN = 0.25f;
    private const float FOOTSTEP_INTERVAL_MAX = 0.4f;

    public Player_MoveState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
        footstepTimer = Random.Range(FOOTSTEP_INTERVAL_MIN, FOOTSTEP_INTERVAL_MAX);
    }

    public override void Update()
    {
        base.Update();

        // 没有水平输入，或者碰到墙壁时，切换回闲置状态
        if (player.moveInput.x == 0 || player.isWall)
            stateMachine.ChangeState(player.idleState);

        // 执行水平移动
        player.SetVelocity(player.moveInput.x * player.MoveSpd, rb.linearVelocity.y);

        // 脚步声：倒计时归零后随机播放并重置间隔
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            AudioManager.Instance?.PlayRandomSFX(RUN_SFX_PATHS, 0.7f, 0.9f, 1.1f);
            footstepTimer = Random.Range(FOOTSTEP_INTERVAL_MIN, FOOTSTEP_INTERVAL_MAX);
        }
    }
}
