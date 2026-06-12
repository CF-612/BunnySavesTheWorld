using UnityEngine;

public class PipeController : MonoBehaviour
{
    [Header("管道配置")]
    [Tooltip("按顺序拖入管道的路径节点，首尾即为进出入口")]
    public Transform[] waypoints;
    
    [Tooltip("玩家在管道内的移动速度")]
    public float moveSpeed = 15f;

    // 在编辑器中绘制绿色的引导线，方便直观地调整管道走向
    private void OnDrawGizmos()
    {
        if (waypoints == null || waypoints.Length < 2) return;
        
        Gizmos.color = Color.green;
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            if (waypoints[i] != null && waypoints[i+1] != null)
            {
                Gizmos.DrawLine(waypoints[i].position, waypoints[i+1].position);
                Gizmos.DrawWireSphere(waypoints[i].position, 0.2f);
            }
        }
        if (waypoints[waypoints.Length - 1] != null)
        {
            Gizmos.DrawWireSphere(waypoints[waypoints.Length - 1].position, 0.2f);
        }
    }
}