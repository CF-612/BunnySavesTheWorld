using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class DynamicRope : MonoBehaviour
{
    [Header("挂点")]
    public Transform fixedPoint;    // 绳子上端固定点（天花板）
    public Transform bulbHook;      // 灯泡上的挂点（会随灯泡移动）

    [Header("绳子渲染")]
    public int segmentCount = 20;   // 绳子分段数（越高越平滑）
    public float ropeWidth = 0.1f;
    
    [Header("弧形控制")]
    public float curveIntensity = 0.5f;   // 弧形强度（受重力偏移）
    public bool usePhysicsBending = true; // 根据灯泡速度动态弯曲

    private LineRenderer lineRenderer;
    private Vector3 previousBulbPos;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = segmentCount;
        lineRenderer.startWidth = ropeWidth;
        lineRenderer.endWidth = ropeWidth;
        previousBulbPos = bulbHook.position;
    }

    void Update()
    {
        UpdateRope();
    }

    void UpdateRope()
    {
        Vector3 start = fixedPoint.position;
        Vector3 end = bulbHook.position;

        // 计算弧形偏移方向（自然下垂或摆动）
        Vector3 bendDir = GetBendDirection();

        Vector3[] points = new Vector3[segmentCount];
        for (int i = 0; i < segmentCount; i++)
        {
            float t = i / (float)(segmentCount - 1); // 0~1
            // 线性插值位置
            Vector3 linearPos = Vector3.Lerp(start, end, t);
            
            // 抛物线偏移：中间最大偏移量，两端为0
            float parabolicT = 4 * t * (1 - t);   // 抛物线形状，峰值在 t=0.5
            Vector3 offset = bendDir * curveIntensity * parabolicT;
            
            points[i] = linearPos + offset;
        }
        lineRenderer.SetPositions(points);
    }

    Vector3 GetBendDirection()
    {
        if (usePhysicsBending && bulbHook != null)
        {
            // 根据灯泡速度方向作为偏移方向（模拟惯性摆动）
            Vector3 velocity = (bulbHook.position - previousBulbPos) / Time.deltaTime;
            previousBulbPos = bulbHook.position;
            // 速度水平分量影响弯曲方向，加上恒定向下重力
            Vector3 bend = new Vector3(velocity.x * 0.1f, -0.5f, 0);
            return bend.normalized;
        }
        else
        {
            // 恒定向下弯曲（模拟重力）
            return Vector3.down;
        }
    }
}