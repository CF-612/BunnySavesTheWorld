using UnityEngine;

public class Player_PipeState : PlayerState
{
    private const string PIPE_SLIDE_SFX_PATH = "Audio/SFX/InteractiveObjects/pipe";

    private Transform[] currentPipePath;
    private bool isStart;
    private float pipeMoveSpeed;
    private Vector2 entryDirection;
    
    private PipeTraveler traveler;
    private bool isTravelComplete;

    public Player_PipeState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public void SetupPipe(Transform[] path, bool isStart, float speed, Vector2 entryDir)
    {
        currentPipePath = path;
        pipeMoveSpeed = speed;
        this.isStart = isStart;
        entryDirection = entryDir;
    }

    public override void Enter()
    {
        base.Enter();

        // 播放进入管道音效
        AudioManager.Instance?.PlaySFX("Audio/SFX/Bunny/Inhale");

        // 开始管道滑行循环音效
        AudioManager.Instance?.PlayLoopingSFX(PIPE_SLIDE_SFX_PATH);

        isTravelComplete = false;

        traveler = player.GetComponent<PipeTraveler>();
        if (traveler == null)
        {
            traveler = player.gameObject.AddComponent<PipeTraveler>();
        }

        if (player.pipeCamera != null)
        {
            player.pipeCamera.SetActive(true);
        }

        // 传入 OnPrepareToExit 回调来提前处理朝向和动画
        traveler.StartTravel(currentPipePath, pipeMoveSpeed, isStart, entryDirection, 
            OnPrepareToExit, 
            () => { isTravelComplete = true; }
        );
    }

    // 新增：预吐出时的回调逻辑
    private void OnPrepareToExit(Vector2 exitDir)
    {
        // 1. 提前处理横向朝向对齐
        if (Mathf.Abs(exitDir.x) > 0.1f)
        {
            int targetFacingDir = exitDir.x > 0 ? 1 : -1;
            if (player.facingDir != targetFacingDir)
            {
                player.Flip(); 
            }
        }

        // 播放退出管道音效
        AudioManager.Instance?.PlaySFX("Audio/SFX/Bunny/Exhale");

        // 2. 强制抹除进入管道前的残留动画状态（例如下落），让动画器切回最中性的闲置姿态
        player.anim.Play("idle"); 
        
        // 顺便把玩家用于控制动画切换的垂直速度参数归零，防止它后续又切回跳跃/下落动画
        player.anim.SetFloat("yVelocity", 0f);
    }

    public override void Update()
    {
        base.Update();

        // 当通用组件返回完成回调时，切换回闲置状态
        if (isTravelComplete)
        {
            stateMachine.ChangeState(player.idleState);
        }
    }

    public override void Exit()
    {
        base.Exit();

        // 停止管道滑行音效
        AudioManager.Instance?.StopLoopingSFX(PIPE_SLIDE_SFX_PATH);

        if (player.pipeCamera != null)
        {
            player.pipeCamera.SetActive(false);
        }
    }
}