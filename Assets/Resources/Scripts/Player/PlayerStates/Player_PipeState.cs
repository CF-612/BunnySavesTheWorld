using UnityEngine;

public class Player_PipeState : PlayerState
{
    private Transform[] currentPipePath;
    private int currentPathIndex;
    private int pathDirection;
    private float pipeMoveSpeed;
    
    private bool isExiting;
    private SpriteRenderer sr;
    private Collider2D cd;

    public Player_PipeState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
        sr = player.GetComponentInChildren<SpriteRenderer>();
        cd = player.GetComponent<Collider2D>();
    }

    // 由 Player 脚本调用，用于接收入口传来的管道数据
    public void SetupPipe(Transform[] path, bool isStart, float speed)
    {
        currentPipePath = path;
        pipeMoveSpeed = speed;
        pathDirection = isStart ? 1 : -1;
        currentPathIndex = isStart ? 0 : path.Length - 1;
    }

    public override void Enter()
    {
        base.Enter();
        isExiting = false;

        // 1. 禁用物理碰撞与重力，防止穿模和受环境风力影响
        cd.enabled = false;
        rb.bodyType = RigidbodyType2D.Kinematic;
        player.SetVelocity(0, 0);

        // 2. 强制将玩家坐标吸附到管道入口正中心
        if (currentPipePath != null && currentPipePath.Length > 0)
        {
            player.transform.position = currentPipePath[currentPathIndex].position;
        }

        // 切换为管道专用相机
        if (player.pipeCamera != null)
        {
            player.pipeCamera.SetActive(true);
        }

        // 3. 播放吸入动画（需要在 Animator 中配置该 Trigger）
        player.anim.SetTrigger("inhale");
    }

    public override void Update()
    {
        base.Update();

        if (currentPipePath == null || currentPipePath.Length == 0) return;

        if (!isExiting)
        {
            // triggerCalled 由吸入动画最后一帧的 Animation Event 置为 true
            if (triggerCalled)
            {
                // 吸入动画播放完毕，隐藏实体，开始路径位移
                sr.enabled = false;

                Transform targetWaypoint = currentPipePath[currentPathIndex];
                player.transform.position = Vector2.MoveTowards(player.transform.position, targetWaypoint.position, pipeMoveSpeed * Time.deltaTime);

                // 判断是否到达当前节点
                if (Vector2.Distance(player.transform.position, targetWaypoint.position) < 0.05f)
                {
                    currentPathIndex += pathDirection;

                    // 判断是否已经越过最后一个节点（到达出口）
                    if (currentPathIndex >= currentPipePath.Length || currentPathIndex < 0)
                    {
                        isExiting = true;
                        triggerCalled = false; // 重置触发器，准备接收吐出动画的结束事件
                        
                        sr.enabled = true; // 恢复显示
                        player.anim.SetTrigger("exhale"); // 播放吐出动画
                    }
                }
            }
        }
        else
        {
            // 等待吐出动画结束
            if (triggerCalled)
            {
                stateMachine.ChangeState(player.idleState);
            }
        }
    }

    public override void Exit()
    {
        base.Exit();
        
        // 恢复玩家的常规物理状态
        sr.enabled = true;
        cd.enabled = true;
        rb.bodyType = RigidbodyType2D.Dynamic;

        // 退出管道状态，关闭专用相机，恢复主相机跟随
        if (player.pipeCamera != null)
        {
            player.pipeCamera.SetActive(false);
        }
    }
}