using UnityEngine;

public class Player_BiteState : Player_GroundedState
{
    private bool biteInputQueued;

    // 长按拖拽相关
    private float biteHoldTimer;
    private bool isHolding;
    private IBiteable currentBiteTarget;
    private Transform currentBiteTargetTransform;

    public Player_BiteState(Player player, StateMachine stateMachine, string animBoolName) : base(player, stateMachine, animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        // 重置所有状态
        biteInputQueued = false;
        biteHoldTimer = 0f;
        isHolding = false;
        currentBiteTarget = null;
        currentBiteTargetTransform = null;

        // 进入啃咬状态时，剥夺移动能力，角色原地停住
        player.SetVelocity(0, rb.linearVelocity.y);
    }

    public override void Update()
    {
        base.Update();

        // 持续按住啃咬键则累加计时器
        if (input.Player.Bite.IsPressed())
        {
            biteHoldTimer += Time.deltaTime;
        }

        // 在动画播放期间，如果玩家再次按下啃咬键，则缓存该指令（连咬）
        if (input.Player.Bite.WasPressedThisFrame())
        {
            biteInputQueued = true;
        }

        // ====== 长按保持态：定格动画 + 拖拽处理 ======
        if (isHolding)
        {
            HandleHoldDrag();
            return;
        }

        // triggerCalled 由动画最后一帧的 Animation Event 修改为 true
        if (triggerCalled)
        {
            // 啃咬键仍按住且超过长按阈值 → 进入长按保持态
            if (input.Player.Bite.IsPressed() && biteHoldTimer >= player.BiteHoldThreshold)
            {
                EnterHoldState();
            }
            else if (biteInputQueued)
            {
                // 有排队的输入，重新进入啃咬状态实现连咬
                stateMachine.ChangeState(player.biteState);
            }
            else
            {
                // 无新指令，返回闲置状态
                stateMachine.ChangeState(player.idleState);
            }
        }
    }

    /// <summary>进入长按保持态：定格动画、记录被咬目标</summary>
    private void EnterHoldState()
    {
        isHolding = true;

        currentBiteTarget = player.lastBiteTarget;
        if (currentBiteTarget != null)
        {
            currentBiteTargetTransform = ((MonoBehaviour)currentBiteTarget).transform;
        }

        // 定格动画在最后一帧
        anim.speed = 0f;
    }

    /// <summary>长按保持期间每帧处理拖拽输入与松手检测</summary>
    private void HandleHoldDrag()
    {
        // 啃咬键松开 或 目标丢失 → 释放拖拽
        if (!input.Player.Bite.IsPressed() || currentBiteTargetTransform == null)
        {
            ReleaseHold();
            stateMachine.ChangeState(player.idleState);
            return;
        }

        // 水平拖拽：有输入时位移 + 播放拖拽音效；无输入时停止音效
        float moveX = player.moveInput.x;
        if (Mathf.Abs(moveX) > 0.01f)
        {
            AudioManager.Instance?.PlayLoopingSFX("Audio/SFX/InteractiveObjects/MovingBox");

            Vector3 displacement = new Vector3(
                moveX * player.MoveSpd * player.BiteDragSpeedMultiplier * Time.deltaTime,
                0f,
                0f
            );

            currentBiteTargetTransform.position += displacement;
            player.transform.position += displacement;
        }
        else
        {
            AudioManager.Instance?.StopLoopingSFX("Audio/SFX/InteractiveObjects/MovingBox");
        }
    }

    /// <summary>释放拖拽：恢复动画速度、清除目标引用</summary>
    private void ReleaseHold()
    {
        isHolding = false;
        currentBiteTarget = null;
        currentBiteTargetTransform = null;
        anim.speed = 1f;

        // 停止拖拽音效
        AudioManager.Instance?.StopLoopingSFX("Audio/SFX/InteractiveObjects/MovingBox");
    }

    public override void Exit()
    {
        ReleaseHold();
        base.Exit();
    }

    // 重写基类的通用动作触发方法
    public override void AnimationActionTrigger()
    {
        base.AnimationActionTrigger();

        // 播放啃咬音效
        AudioManager.Instance?.PlaySFX("Audio/SFX/Bunny/Bite");

        // 在 Animator 中调用 ActionTrigger() 的那一帧，自动执行物理啃咬检测
        player.DetectBiteable();
    }
}
