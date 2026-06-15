using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckPoint : MonoBehaviour
{
    private LevelManager levelManager;

    private void Awake()
    {
        levelManager = FindObjectOfType<LevelManager>();

        // 确保碰撞体为触发器
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && levelManager != null)
        {
            levelManager.SetCheckpoint(transform.position);
        }
    }

    private void OnDrawGizmos()
    {
        // 编辑器中绘制可见的检查点标记
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawCube(transform.position, new Vector3(1f, 2f, 0f));

        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 0f));

        // 绘制旗标图标指示方向
        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.5f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 1.5f, 0.2f);
    }
}
