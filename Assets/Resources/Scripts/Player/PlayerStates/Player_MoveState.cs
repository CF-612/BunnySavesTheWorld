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

        // —— 计算风力影响后的水平速度 ——
        float desiredVelocity = player.moveInput.x * player.MoveSpd;

        if (player.windReceiver != null && player.windReceiver.IsBeingBlown)
        {
            Vector2 wind = player.windReceiver.GetWindVelocity();
            if (Mathf.Abs(wind.x) > 0.01f)
            {
                WindZoneData windZone = player.windReceiver.GetStrongestZone();
                float groundResist = windZone != null ? windZone.GroundResistMultiplier : 0.4f;
                float windContribution = wind.x * groundResist;

                if (Mathf.Abs(player.moveInput.x) < 0.01f)
                {
                    // 无输入 → 风力缓慢推动玩家
                    desiredVelocity = windContribution;
                }
                else
                {
                    float inputDir = Mathf.Sign(player.moveInput.x);
                    float windDir = Mathf.Sign(wind.x);

                    if (inputDir == windDir)
                    {
                        // 顺风 → 速度加成
                        desiredVelocity += windContribution;
                    }
                    else
                    {
                        // 逆风 → 阻力，越靠近风源阻力越大
                        if (windZone != null)
                        {
                            float normalizedDist = windZone.GetNormalizedDistance(
                                player.transform.position);

                            if (normalizedDist <= windZone.MinApproachNormalized)
                            {
                                // 过于接近风源，完全无法前进
                                desiredVelocity = 0f;
                            }
                            else
                            {
                                desiredVelocity -= Mathf.Abs(windContribution);
                                // 保证风力不会把玩家推反向
                                if (Mathf.Sign(desiredVelocity) != inputDir)
                                    desiredVelocity = 0f;
                            }
                        }
                        else
                        {
                            desiredVelocity -= Mathf.Abs(windContribution);
                            if (Mathf.Sign(desiredVelocity) != inputDir)
                                desiredVelocity = 0f;
                        }
                    }
                }
            }
        }

        // 执行水平移动
        player.SetVelocity(desiredVelocity, rb.linearVelocity.y);

        // 风力阻止前进时，玩家仍应面向输入方向
        if (desiredVelocity == 0f && player.moveInput.x != 0)
            player.HandleFlip(player.moveInput.x);

        // 脚步声：倒计时归零后随机播放并重置间隔
        footstepTimer -= Time.deltaTime;
        if (footstepTimer <= 0f)
        {
            AudioManager.Instance?.PlayRandomSFX(RUN_SFX_PATHS, 0.7f, 0.9f, 1.1f);
            footstepTimer = Random.Range(FOOTSTEP_INTERVAL_MIN, FOOTSTEP_INTERVAL_MAX);
        }
    }
}
