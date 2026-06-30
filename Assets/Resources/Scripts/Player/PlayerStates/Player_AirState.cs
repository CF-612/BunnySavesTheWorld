using UnityEngine;

public class Player_AirState : PlayerState
{
    public Player_AirState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Update()
    {
        base.Update();

        // 基础空中输入速度
        float inputVelocity = player.moveInput.x * player.MoveSpd * player.InAirMoveMultiplier;

        // —— 风力影响：空中玩家被吹飞，输入无法有效抵抗 ——
        float finalX = inputVelocity;
        if (player.windReceiver != null && player.windReceiver.IsBeingBlown)
        {
            Vector2 wind = player.windReceiver.GetWindVelocity();
            if (Mathf.Abs(wind.x) > 0.01f)
            {
                WindZoneData windZone = player.windReceiver.GetStrongestZone();
                float airMultiplier = windZone != null ? windZone.AirForceMultiplier : 1.5f;

                // 空中风力直接叠加到玩家速度上
                finalX = inputVelocity + wind.x * airMultiplier;

                // 风与输入对抗时防闪动处理：
                // 风力主导移动方向但玩家在反向操作 → 朝向优先使用玩家输入方向，
                // 避免 net velocity 被风力与输入抵消时因微小波动导致的朝向鬼畜闪动
                if (Mathf.Abs(player.moveInput.x) > 0.01f
                    && Mathf.Sign(player.moveInput.x) != Mathf.Sign(wind.x))
                {
                    // 直接赋值速度 + 手动以输入方向作为朝向（单次 Flip，无抖动）
                    rb.linearVelocity = new Vector2(finalX, rb.linearVelocity.y);
                    player.HandleFlip(player.moveInput.x);
                    return;
                }
            }
        }

        player.SetVelocity(finalX, rb.linearVelocity.y);
    }
}
