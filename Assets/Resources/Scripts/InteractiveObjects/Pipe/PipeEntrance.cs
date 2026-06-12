using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PipeEntrance : MonoBehaviour
{
    [Header("入口配置")]
    public PipeController parentPipe;
    
    [Tooltip("勾选代表这是 waypoints 数组的第一个节点入口，不勾选代表是最后一个节点出口")]
    public bool isStartEntrance = true;
    
    [Tooltip("玩家需要按下的方向键才能触发吸入（如朝上的管口填 0, -1）")]
    public Vector2 requiredInputDirection = Vector2.down;

    private void Awake()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (parentPipe == null || parentPipe.waypoints == null || parentPipe.waypoints.Length < 2) return;

        Player player = collision.GetComponent<Player>();
        
        // 确保玩家存在，且当前不在管道状态中
        if (player != null && !player.isInsidePipe)
        {
            // 检测玩家是否有移动输入
            if (player.moveInput.magnitude > 0.1f)
            {
                // 使用点乘判断玩家按下的方向与要求方向是否大致一致（容错度 0.5f 代表夹角在 60 度以内皆可）
                float dot = Vector2.Dot(player.moveInput.normalized, requiredInputDirection.normalized);
                if (dot > 0.5f)
                {
                    player.EnterPipe(parentPipe.waypoints, isStartEntrance, parentPipe.moveSpeed, requiredInputDirection);
                }
            }
            return;
        }

        // 2. 作为普通物理物件处理（如箱子，无按键，靠近中心即自动吸入）
        PipeTraveler traveler = collision.GetComponent<PipeTraveler>();
        if (traveler != null)
        {
            // 移除了不合理的物理中心点距离限制
            // 只要物体能碰到入口处的 Trigger，就被直接吸入
            // 为普通物件传入 null 占据 onPrepareToExit 和 onComplete 参数的位置
            traveler.StartTravel(parentPipe.waypoints, parentPipe.moveSpeed, isStartEntrance, requiredInputDirection, null, null);
        }
    }
}