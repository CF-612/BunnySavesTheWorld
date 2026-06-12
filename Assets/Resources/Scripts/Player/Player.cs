using System;
using UnityEngine;

public class Player : Entity
{
    public PlayerInputSet input { get; private set; }
    
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

    public bool isInsidePipe => stateMachine.CurrentState == pipeState;

    [Header("基础运动参数")]
    public float MoveSpd = 8f;
    public float JumpForce = 15f;
    [Range(0,1)]
    public float InAirMoveMultiplier = 0.8f;
    
    [Header("特殊能力参数")]
    public float GlideGravityScale = 0.5f;
    public float AirStompVelocity = 25f;
    public float DigSpeed = 2f;

    [Header("啃咬检测")]
    public Transform BiteCheck;
    public float BiteCheckRadius = 0.7f;
    public LayerMask WhatIsBiteable;

    [Header("环境受力")]
    public Vector2 WindVelocity;
    
    [Header("相机控制")]
    public GameObject pipeCamera;

    public Vector2 moveInput { get; private set; }

    protected override void Awake()
    {
        base.Awake();

        input = new PlayerInputSet();
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
    }

    protected override void Start()
    {
        base.Start();
        stateMachine.Initialize(idleState);
    }

    private void OnEnable()
    {
        input.Enable();

        input.Player.Movement.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        input.Player.Movement.canceled += ctx => moveInput = Vector2.zero;
    }

    private void OnDisable()
    {
        input.Disable();
    }

    public void SetWindVelocity(Vector2 wind)
    {
        WindVelocity = wind;
    }

    // 隐藏基类的 SetVelocity，在原有速度基础上叠加外部风力
    public new void SetVelocity(float xVelocity, float yVelocity)
    {
        float finalY = yVelocity;
        
        // 如果存在垂直风力，让Y轴速度平滑向风力上限靠拢，模拟浮空/风洞效果避免速度爆炸
        if (WindVelocity.y != 0)
        {
            finalY = Mathf.Lerp(yVelocity, WindVelocity.y, Time.deltaTime * 5f);
        }

        base.SetVelocity(xVelocity + WindVelocity.x, finalY);
    }

    public void DetectBiteable()
    {
        // 执行 OverlapCircle 寻找前方可啃咬层级的物体
        Collider2D[] detectedObjects = Physics2D.OverlapCircleAll(BiteCheck.position, BiteCheckRadius, WhatIsBiteable);

        foreach (var hit in detectedObjects)
        {
            IBiteable biteable = hit.GetComponent<IBiteable>();

            // 如果找到实现接口的对象，且未被彻底破坏，则执行啃咬逻辑
            if (biteable != null && !biteable.GetIsBroken())
            {
                biteable.OnBitten();
                
                // 一次啃咬指令通常只对一个物体生效，咬到即跳出循环
                break;
            }
        }
    }

    public void EnterPipe(Transform[] path, bool isStart, float speed)
    {
        pipeState.SetupPipe(path, isStart, speed);
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
}
