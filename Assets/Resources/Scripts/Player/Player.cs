using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class Player : Entity
{
    public static event Action OnPlayerDeath;
    public PlayerInputSet input { get; private set; }
    public bool IsDead { get; private set; }
    
    // 基础状态
    public Player_idleState idleState { get; private set; }
    public Player_MoveState moveState { get; private set; }
    public Player_JumpState jumpState { get; private set; }
    public Player_FallState fallState { get; private set; }
    
    // 特殊能力状态
    public Player_BiteState biteState { get; private set; }
    public Player_GroundStompState groundStompState { get; private set; }
    public Player_DigState digState { get; private set; }
    public Player_AirStompState airStompState { get; private set; }
    public Player_GlideState glideState { get; private set; }
    public Player_PipeState pipeState { get; private set; }
    public Player_DeadState deadState { get; private set; }

    public bool isInsidePipe => stateMachine.CurrentState == pipeState;

    [Header("基础运动参数")]
    public float MoveSpd = 8f;
    [Tooltip("地面起步加速度（Accel = Acceleration）。")]
    [Min(0f)]
    public float GroundAccel = 80f;
    [Tooltip("地面松开输入后的减速度（Decel = Deceleration）。")]
    [Min(0f)]
    public float GroundDecel = 100f;
    [Tooltip("地面反向输入时使用的制动加速度。")]
    [Min(0f)]
    public float ReverseBrake = 140f;
    [Tooltip("空中水平加速度（Accel = Acceleration）。")]
    [Min(0f)]
    public float AirAccel = 50f;
    [Tooltip("空中松开输入后的减速度（Decel = Deceleration）。")]
    [Min(0f)]
    public float AirDecel = 30f;

    [Header("跳跃参数")]
    public float JumpForce = 15f;
    [Range(0,1)]
    public float InAirMoveMultiplier = 0.8f;
    [Tooltip("落地前允许提前缓存跳跃输入的时长（秒）。")]
    [Min(0f)]
    public float JumpBufferDuration = 0.12f;
    [Tooltip("离开平台后仍允许起跳的宽限时长（秒）。")]
    [Min(0f)]
    public float CoyoteTimeDuration = 0.1f;
    [Tooltip("上升阶段松开跳跃键后保留的纵向速度比例。")]
    [Range(0f, 1f)]
    public float JumpCutMultiplier = 0.5f;
    
    [Header("特殊能力参数")]
    public float GlideGravityScale = 0.5f;
    public float AirStompVelocity = 25f;
    public float DigSpeed = 2f;

    [Header("风力交互")]
    public PlayerWindReceiver windReceiver { get; private set; }

    [Header("单向平台下落")]
    [Tooltip("下落穿过平台后恢复碰撞的延迟（秒）")]
    public float dropThroughDuration = 0.4f;

    private Coroutine dropThroughCoroutine;

    [Header("啃咬检测")]
    public Transform BiteCheck;
    public float BiteCheckRadius = 0.7f;
    public LayerMask WhatIsBiteable;

    [Header("啃咬长按与拖拽")]
    [Tooltip("判定为长按的按住时间阈值（秒）")]
    public float BiteHoldThreshold = 0.3f;
    [Tooltip("拖拽时水平移动速度倍率")]
    public float BiteDragSpeedMultiplier = 1.0f;

    /// <summary>最近一次啃咬命中的目标（用于长按拖拽）</summary>
    public IBiteable lastBiteTarget { get; private set; }

    [Header("死亡设置")]
    [Tooltip("致死坠落高度阈值（世界单位）")]
    public float fallDeathHeight = 10f;
    [Tooltip("落地死亡时生成的烟雾特效预制体")]
    public GameObject deathSmokeVFX;
    [Tooltip("死亡动画播放完毕后等待销毁的时间（秒）")]
    public float RespawnDelay = 2f;

    [Header("相机控制")]
    public GameObject pipeCamera;

    public Vector2 moveInput { get; private set; }

    private SpriteRenderer spriteRenderer;
    private readonly HashSet<object> inputLockOwners = new HashSet<object>();
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float lastGroundedTime = float.NegativeInfinity;
    private bool canUseGroundJump;

    protected override void Awake()
    {
        base.Awake();

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        windReceiver = GetComponent<PlayerWindReceiver>();

        input = new PlayerInputSet();
        input.Player.Movement.performed += OnMovementPerformed;
        input.Player.Movement.canceled += OnMovementCanceled;
        groundCheck = transform;

        // 实例化基础状态
        idleState = new Player_idleState(this, stateMachine, "idle");
        moveState = new Player_MoveState(this, stateMachine, "move");
        jumpState = new Player_JumpState(this, stateMachine, "jumpFall");
        fallState = new Player_FallState(this, stateMachine, "jumpFall");

        // 实例化特殊能力状态
        biteState = new Player_BiteState(this, stateMachine, "bite");
        groundStompState = new Player_GroundStompState(this, stateMachine, "groundStomp");
        digState = new Player_DigState(this, stateMachine, "dig");
        airStompState = new Player_AirStompState(this, stateMachine, "airStomp");
        glideState = new Player_GlideState(this, stateMachine, "glide");
        pipeState = new Player_PipeState(this, stateMachine, "");
        deadState = new Player_DeadState(this, stateMachine, "dead");
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    public override void EntityDeath()
    {
        if (IsDead)
            return;

        IsDead = true;
        AcquireInputLock(this);
        base.EntityDeath();

        OnPlayerDeath?.Invoke();
        stateMachine.ChangeState(deadState);
    }

    private void OnEnable()
    {
        RefreshInputState();
    }

    private void OnDisable()
    {
        ResetJumpContext();
        if (input != null)
            input.Disable();
    }

    private void OnDestroy()
    {
        if (input == null)
            return;

        input.Player.Movement.performed -= OnMovementPerformed;
        input.Player.Movement.canceled -= OnMovementCanceled;
        input.Dispose();
        input = null;
        inputLockOwners.Clear();
    }

    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void OnMovementCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }

    /// <summary>
    /// 为指定持有者添加输入锁。只有所有持有者都释放输入锁后，玩家输入才会恢复，
    /// 从而避免对话结束时错误解除死亡或过场剧情仍在使用的输入锁。
    /// </summary>
    public void AcquireInputLock(object owner)
    {
        if (this == null || owner == null || input == null)
            return;

        ResetJumpContext();
        inputLockOwners.Add(owner);
        RefreshInputState();
    }

    /// <summary>只释放由指定持有者取得的输入锁。</summary>
    public void ReleaseInputLock(object owner)
    {
        if (this == null || owner == null || input == null)
            return;

        inputLockOwners.Remove(owner);
        RefreshInputState();
    }

    private void RefreshInputState()
    {
        // Unity 对象销毁后，C# 引用可能仍存在；先拦截生命周期回调。
        if (this == null || input == null)
            return;

        if (isActiveAndEnabled && inputLockOwners.Count == 0)
            input.Enable();
        else
        {
            moveInput = Vector2.zero;
            input.Disable();
        }
    }
    public void DetectBiteable()
    {
        lastBiteTarget = null;

        // 执行 OverlapCircle 寻找前方可啃咬层级的物体
        Collider2D[] detectedObjects = Physics2D.OverlapCircleAll(BiteCheck.position, BiteCheckRadius, WhatIsBiteable);

        foreach (var hit in detectedObjects)
        {
            IBiteable biteable = hit.GetComponent<IBiteable>();

            // 如果找到实现接口的对象，且未被彻底破坏，则执行啃咬逻辑
            if (biteable != null && !biteable.GetIsBroken())
            {
                biteable.OnBitten();
                lastBiteTarget = biteable;

                // 一次啃咬指令通常只对一个物体生效，咬到即跳出循环
                break;
            }
        }
    }

    public void EnterPipe(Transform[] path, bool isStart, float speed, Vector2 entryDir)
    {
        ResetJumpContext();
        pipeState.SetupPipe(path, isStart, speed, entryDir);
        stateMachine.ChangeState(pipeState);
    }

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        if (BiteCheck != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(BiteCheck.position, BiteCheckRadius);
        }
    }

    /// <summary>隐藏玩家视觉（死亡动画播放完毕后由 LevelManager 调用）</summary>
    public void HideVisual()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
    }

    /// <summary>恢复玩家视觉（重生传送后由 LevelManager 调用）</summary>
    public void ShowVisual()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    /// <summary>在检查点重生：传送 + 恢复物理 + 恢复输入 + 切回 Idle</summary>
    public void Respawn(Vector3 spawnPosition)
    {
        transform.position = spawnPosition;
        ShowVisual();
        IsDead = false;
        ResetJumpContext();

        // 恢复物理模拟
        rb.simulated = true;
        rb.linearVelocity = Vector2.zero;

        // 恢复碰撞体
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.enabled = true;

        ReleaseInputLock(this);

        // 解锁状态机并切回闲置状态
        stateMachine.CanChangeState = true;
        stateMachine.ChangeState(idleState);
    }

    /// <summary>在场景初始化阶段移动玩家，但不改变当前状态机状态。</summary>
    public void TeleportTo(Vector3 position)
    {
        transform.position = position;
        ResetJumpContext();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    /// <summary>按当前移动阶段的加减速参数，将水平速度平滑逼近目标值。</summary>
    public void ApplyHorizontalVelocity(float targetVelocity, float accel, float decel, float reverseBrake)
    {
        float currentVelocity = rb.linearVelocity.x;
        float rate;

        if (Mathf.Abs(targetVelocity) < 0.01f)
        {
            rate = decel;
        }
        else if (Mathf.Abs(currentVelocity) >= 0.01f
                 && Mathf.Sign(currentVelocity) != Mathf.Sign(targetVelocity))
        {
            rate = reverseBrake;
        }
        else
        {
            rate = accel;
        }

        float nextVelocity = Mathf.MoveTowards(
            currentVelocity,
            targetVelocity,
            Mathf.Max(0f, rate) * Time.deltaTime);

        SetVelocity(nextVelocity, rb.linearVelocity.y);
    }

    /// <summary>记录一次跳跃按下输入，供土狼时间或落地缓冲消费。</summary>
    public void RecordJumpInput()
    {
        lastJumpPressedTime = Time.time;
    }

    /// <summary>刷新最近接地时间，并恢复一次地面跳跃资格。</summary>
    public void RefreshGroundJumpWindow()
    {
        lastGroundedTime = Time.time;
        canUseGroundJump = true;
    }

    /// <summary>在跳跃缓冲和接地/土狼时间均有效时，消费一次地面跳跃。</summary>
    public bool TryConsumeGroundJump()
    {
        if (!canUseGroundJump)
            return false;

        bool hasBufferedInput = Time.time - lastJumpPressedTime <= Mathf.Max(0f, JumpBufferDuration);
        bool isWithinGroundWindow = isGround
            || Time.time - lastGroundedTime <= Mathf.Max(0f, CoyoteTimeDuration);

        if (!hasBufferedInput || !isWithinGroundWindow)
            return false;

        canUseGroundJump = false;
        lastJumpPressedTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        return true;
    }

    /// <summary>清除未消费的跳跃输入和地面跳跃资格。</summary>
    public void ResetJumpContext()
    {
        lastJumpPressedTime = float.NegativeInfinity;
        lastGroundedTime = float.NegativeInfinity;
        canUseGroundJump = false;
    }

    public void Revive()
    {}

    /// <summary>尝试从单向平台下落。由 GroundedState 在 JumpDown 按下时调用。</summary>
    public void TryDropThroughPlatform()
    {
        // 向下射线检测脚下的平台
        float checkDist = 1.5f;
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position, Vector2.down, checkDist, whatIsGround);

        if (hit.collider == null) return;

        // 确认是单向平台（有 PlatformEffector2D 且 useOneWay 启用）
        PlatformEffector2D effector = hit.collider.GetComponent<PlatformEffector2D>();
        if (effector == null || !effector.useOneWay) return;

        if (dropThroughCoroutine != null)
            StopCoroutine(dropThroughCoroutine);

        dropThroughCoroutine = StartCoroutine(DropThroughCo(hit.collider));
    }

    private System.Collections.IEnumerator DropThroughCo(Collider2D platformCollider)
    {
        // 对玩家所有非 Trigger 碰撞体忽略与平台的碰撞
        Collider2D[] cols = GetComponentsInChildren<Collider2D>();
        foreach (var col in cols)
        {
            if (col != null && !col.isTrigger)
                Physics2D.IgnoreCollision(col, platformCollider, true);
        }

        yield return new WaitForSeconds(dropThroughDuration);

        // 恢复碰撞
        foreach (var col in cols)
        {
            if (col != null && !col.isTrigger)
                Physics2D.IgnoreCollision(col, platformCollider, false);
        }

        dropThroughCoroutine = null;
    }
}
