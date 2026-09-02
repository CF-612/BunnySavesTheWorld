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

        if (stateMachine.CurrentState != this)
            return;

        // 没有水平输入，或者碰到墙壁时，切换回闲置状态
        if (player.moveInput.x == 0 || player.isWall)
        {
            stateMachine.ChangeState(player.idleState);
            return;
        }

        float targetSpd = player.GetGroundTargetSpd(player.moveInput.x);

        // 风区保持既有直接速度逻辑；普通移动使用加速、减速和反向制动。
        if (player.windReceiver != null && player.windReceiver.IsBeingBlown)
            player.SetVelocity(targetSpd, rb.linearVelocity.y);
        else
            player.ApplyHorizontalVelocity(
                targetSpd,
                player.GroundAccel,
                player.GroundDecel,
                player.ReverseBrake);

        // 风力阻止前进时，玩家仍应面向输入方向
        if (targetSpd == 0f && player.moveInput.x != 0)
            player.HandleFlip(player.moveInput.x);

        // 脚步声使用与移动动画相同的速度倍率；原地受阻时不播放。
        if (Mathf.Abs(rb.linearVelocity.x) > 0.01f)
        {
            footstepTimer -= Time.deltaTime * player.GetMoveAnimSpdMultiplier();
            if (footstepTimer <= 0f)
            {
                AudioManager.Instance?.PlayRandomSFX(RUN_SFX_PATHS, 0.7f, 0.9f, 1.1f);
                footstepTimer = Random.Range(FOOTSTEP_INTERVAL_MIN, FOOTSTEP_INTERVAL_MAX);
            }
        }
    }
}
