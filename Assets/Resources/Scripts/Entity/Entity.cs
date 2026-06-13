using System;
using UnityEngine;
using System.Collections;

public class Entity : MonoBehaviour
{
    public event Action OnFlipped;
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }

    protected StateMachine stateMachine;

    public int facingDir { get; private set; } = 1;

    [Header("碰撞检测")]
    [SerializeField] protected LayerMask whatIsGround;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private float wallCheckDistance;
    [SerializeField] protected Transform groundCheck;
    [SerializeField] protected Transform wallCheck;
    public bool isGround { get; private set; }
    public bool isWall { get; private set; }
    public bool hasWall { get; private set; }

    //[Header("击退变量")]
    private Coroutine knockbackCo;
    private bool isKnocked;

    protected virtual void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();

        stateMachine = new StateMachine();
    }


    protected virtual void Start()
    {
        
    }

    protected virtual void Update()
    {
        HandleCollisionDetection();
        stateMachine.UpdateActiveState();
    }

    public virtual void EntityDeath()
    {
        
    }

    public void ReciveKnockback(Vector2 knockback,float duration)
    {
        if(knockbackCo != null)
            StopCoroutine(knockbackCo);

        knockbackCo = StartCoroutine(KnockbackCo(knockback,duration));
    }

    private IEnumerator KnockbackCo(Vector2 knockback,float duration)
    {
        isKnocked = true;
        rb.linearVelocity = knockback;

        yield return new WaitForSeconds(duration);
        rb.linearVelocity = Vector2.zero;
        isKnocked = false;
    }

    public void CurrentStateAnimationTrigger()
    {
        stateMachine.CurrentState.AnimationTrigger();
    }

    // 调用当前状态的动作执行逻辑
    public void CurrentStateActionTrigger()
    {
        stateMachine.CurrentState.AnimationActionTrigger();
    }

    public virtual void SetVelocity(float xVelocity,float yVelocity)
    {
        if(isKnocked)
            return;

        rb.linearVelocity = new Vector2(xVelocity,yVelocity);

        HandleFlip(xVelocity);
    }

    public void HandleFlip(float xVelocity)
    {
        if (xVelocity > 0 && facingDir != 1)
            Flip();
        else if (xVelocity < 0 && facingDir != -1)
            Flip();
    }

    public void Flip()
    {
        facingDir = facingDir * -1;
        transform.localScale = new Vector3(facingDir * Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);

        OnFlipped?.Invoke();
    }

    private void HandleCollisionDetection()
    {
        isGround = Physics2D.Raycast(groundCheck.position,Vector2.down,groundCheckDistance,whatIsGround);

        isWall = Physics2D.Raycast(wallCheck.position,Vector2.right * facingDir,wallCheckDistance,whatIsGround);
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(groundCheck.position,groundCheck.position + new Vector3(0,-groundCheckDistance,0));
        Gizmos.DrawLine(wallCheck.position,wallCheck.position + new Vector3(wallCheckDistance * facingDir,0,0));
    }
}
