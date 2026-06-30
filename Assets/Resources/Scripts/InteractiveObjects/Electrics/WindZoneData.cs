using UnityEngine;

/// <summary>
/// 风力区域数据组件，挂载在 Blower 的风力触发器子物体上。
/// 风力区域由本物体上的 Collider2D（IsTrigger）定义边界，
/// 风力方向由物体的实际朝向（包含旋转与缩放翻转）自动决定。
/// 风力仅在该 Trigger Collider 范围内生效。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WindZoneData : MonoBehaviour
{
    [Header("风力强度与衰减")]
    [Tooltip("风源处的最大推力（速度单位）。")]
    [SerializeField] private float maxWindForce = 10f;
    [Tooltip("风力沿风向的衰减曲线。X轴：0=上风边缘（风源，最大风力），1=下风边缘（风力最小）。")]
    [SerializeField] private AnimationCurve windForceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);

    [Header("地面限制")]
    [Tooltip("地面时玩家对抗风力的系数（0~1）。越小越容易顶风前进。")]
    [Range(0f, 1f)]
    [SerializeField] private float groundResistMultiplier = 0.4f;
    [Tooltip("玩家逆风时能接近风源的最小归一化距离（0=风源边缘）。达到此距离时地面速度强制归零。")]
    [Range(0f, 0.5f)]
    [SerializeField] private float minApproachNormalized = 0.1f;

    [Header("空中倍率")]
    [Tooltip("空中时风力放大系数（>=1）。")]
    [SerializeField] private float airForceMultiplier = 1.5f;

    [Header("手动风向覆盖（可选）")]
    [Tooltip("勾选后使用下方自定义风向，否则自动从物体朝向推导。")]
    [SerializeField] private bool overrideDirection = false;
    [Tooltip("自定义风力方向（世界空间）。仅在 Override Direction 勾选时生效。")]
    [SerializeField] private Vector2 customWindDirection = Vector2.left;

    // —— 运行时缓存 ——
    private Transform cachedTransform;
    private Collider2D windCollider;
    private Vector2 resolvedWindDirection;

    // —— 公开只读属性 ——
    public Vector2 WindDirection => resolvedWindDirection;
    public float MaxWindForce => maxWindForce;
    public AnimationCurve WindForceCurve => windForceCurve;
    public float GroundResistMultiplier => groundResistMultiplier;
    public float AirForceMultiplier => airForceMultiplier;
    public float MinApproachNormalized => minApproachNormalized;
    /// <summary>本风区的 Collider2D 引用（供外部做碰撞体级重叠检测）。</summary>
    public Collider2D WindCollider => windCollider;

    private void Awake()
    {
        cachedTransform = transform;
        windCollider = GetComponent<Collider2D>();

        if (!windCollider.isTrigger)
        {
            Debug.LogWarning($"[WindZoneData] {name} 上的 Collider2D 未设置为 IsTrigger，已自动修正。", this);
            windCollider.isTrigger = true;
        }

        if (windForceCurve == null || windForceCurve.keys.Length == 0)
        {
            windForceCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        }

        ResolveWindDirection();
    }

    /// <summary>
    /// 解析风力方向。
    /// 自动模式使用 lossyScale.x 补偿缩放翻转（正确处理 scale.x=-1 的物体）。
    /// </summary>
    private void ResolveWindDirection()
    {
        if (overrideDirection)
        {
            resolvedWindDirection = customWindDirection.normalized;
        }
        else
        {
            float scaleSign = Mathf.Sign(cachedTransform.lossyScale.x);
            Vector3 right = cachedTransform.right;
            resolvedWindDirection = new Vector2(right.x, right.y) * scaleSign;
        }

        if (resolvedWindDirection == Vector2.zero)
            resolvedWindDirection = Vector2.right;
    }

    // ——— 风力查询（每次调用动态计算，不使用缓存的 WindSourcePosition） ———

    /// <summary>
    /// 实时计算上风边缘的世界位置（风源点）。
    /// 每次调用都从当前 collider.bounds 重新计算，确保位置始终正确。
    /// </summary>
    private Vector2 GetWindSourcePosition()
    {
        if (windCollider == null) return (Vector2)cachedTransform.position;

        Bounds bounds = windCollider.bounds;
        Vector2 center2D = new Vector2(bounds.center.x, bounds.center.y);
        Vector2 extents2D = new Vector2(bounds.extents.x, bounds.extents.y);

        float halfExtentOnWind = Mathf.Abs(extents2D.x * resolvedWindDirection.x)
                               + Mathf.Abs(extents2D.y * resolvedWindDirection.y);

        return center2D - resolvedWindDirection * halfExtentOnWind;
    }

    /// <summary>
    /// 使用目标碰撞体的包围盒与风力区的包围盒做重叠检测，
    /// 与物理引擎的 Trigger 检测方式一致，避免 pivot 点检测导致的边界振荡。
    /// </summary>
    public bool IsInWindZone(Collider2D targetCollider)
    {
        if (windCollider == null || targetCollider == null) return false;
        return windCollider.bounds.Intersects(targetCollider.bounds);
    }

    /// <summary>
    /// 计算某世界位置沿风向的归一化距离。
    /// 0 = 上风边缘（风源，最大风力），1 = 下风边缘（风力最小）。
    /// 每次调用动态计算风源位置，不依赖缓存。
    /// </summary>
    public float GetNormalizedDistance(Vector2 worldPosition)
    {
        if (windCollider == null) return float.MaxValue;

        Vector2 windSource = GetWindSourcePosition();
        Vector2 toPosition = worldPosition - windSource;
        float projectionOnWind = Vector2.Dot(toPosition, resolvedWindDirection);

        Bounds bounds = windCollider.bounds;
        Vector2 extents2D = new Vector2(bounds.extents.x, bounds.extents.y);
        float extentOnWind = Mathf.Abs(extents2D.x * resolvedWindDirection.x)
                           + Mathf.Abs(extents2D.y * resolvedWindDirection.y);
        float totalLength = extentOnWind * 2f;

        if (totalLength < 0.001f) return 0f;

        return Mathf.Clamp01(projectionOnWind / totalLength);
    }

    /// <summary>
    /// 获取某世界位置的风力向量（已组合方向和强度）。
    /// 调用方应确保传入的位置已通过 IsInWindZone 验证在风区内。
    /// </summary>
    public Vector2 GetWindForceAt(Vector2 worldPosition)
    {
        float normalizedDist = GetNormalizedDistance(worldPosition);
        normalizedDist = Mathf.Clamp01(normalizedDist);

        float forceMultiplier = windForceCurve.Evaluate(normalizedDist);
        return resolvedWindDirection * maxWindForce * forceMultiplier;
    }

    // ——— 编辑器可视化 ———

    private void OnDrawGizmos()
    {
        if (windCollider == null)
            windCollider = GetComponent<Collider2D>();

        Vector3 dir;
        if (Application.isPlaying)
        {
            dir = resolvedWindDirection;
        }
        else
        {
            if (overrideDirection)
                dir = customWindDirection.normalized;
            else
            {
                float sign = Mathf.Sign(transform.lossyScale.x);
                dir = transform.right * sign;
            }
        }

        if (dir == Vector3.zero) dir = Vector3.right;

        Vector3 center = transform.position;
        Bounds bounds;
        if (windCollider != null)
        {
            bounds = windCollider.bounds;
            center = bounds.center;
        }
        else
        {
            bounds = new Bounds(center, Vector3.one);
        }

        // —— 1. 触发器边界（白色虚线） ——
        Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        // —— 2. 半透明填充（风力衰减预览） ——
        int sampleCount = 20;
        Vector3 perpDir = Vector3.Cross(dir, Vector3.forward).normalized;
        float halfWidth = Mathf.Max(bounds.extents.y, bounds.extents.x) * 0.8f;
        Vector2 extents2D = new Vector2(bounds.extents.x, bounds.extents.y);
        float extentOnWind = Mathf.Abs(extents2D.x * dir.x) + Mathf.Abs(extents2D.y * dir.y);
        Vector3 windStart = center - dir * extentOnWind;
        Vector3 windEnd = center + dir * extentOnWind;

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)(sampleCount - 1);
            float forceT = windForceCurve != null && windForceCurve.keys.Length > 0
                ? windForceCurve.Evaluate(t)
                : 1f - t;
            Vector3 samplePos = Vector3.Lerp(windStart, windEnd, t);
            Gizmos.color = new Color(1f - forceT * 0.7f, 0.3f + forceT * 0.4f, 0.3f + forceT * 0.7f, 0.25f);
            Gizmos.DrawLine(samplePos - perpDir * halfWidth, samplePos + perpDir * halfWidth);
        }

        // —— 3. 风力方向箭头（品红色大箭头） ——
        Gizmos.color = new Color(1f, 0f, 0.6f, 0.9f);
        float arrowLength = extentOnWind * 2f;
        Vector3 arrowStart = windStart;
        Vector3 arrowEnd = windStart + dir * arrowLength;
        Gizmos.DrawLine(arrowStart, arrowEnd);

        Vector3 arrowPerp = Vector3.Cross(dir, Vector3.forward) * 0.5f;
        Gizmos.DrawLine(arrowEnd, arrowEnd - dir * 1.2f + arrowPerp);
        Gizmos.DrawLine(arrowEnd, arrowEnd - dir * 1.2f - arrowPerp);

        int flowArrowCount = Mathf.FloorToInt(arrowLength / 0.7f);
        for (int i = 0; i < flowArrowCount; i++)
        {
            float t = (i + 0.5f) / flowArrowCount;
            Vector3 flowPos = Vector3.Lerp(arrowStart, windStart + dir * arrowLength, t);
            Gizmos.DrawLine(flowPos - arrowPerp * 0.2f, flowPos + dir * 0.3f);
            Gizmos.DrawLine(flowPos + arrowPerp * 0.2f, flowPos + dir * 0.3f);
        }

        // —— 4. 风源标记点（红色小球） ——
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(arrowStart, 0.12f);

        // —— 5. 上风/下风标签辅助线 ——
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f);
        Gizmos.DrawLine(arrowStart - perpDir * halfWidth * 0.7f,
                        arrowStart + perpDir * halfWidth * 0.7f);

        Gizmos.color = new Color(0f, 0.5f, 1f, 0.6f);
        Gizmos.DrawLine(windEnd - perpDir * halfWidth * 0.7f,
                        windEnd + perpDir * halfWidth * 0.7f);
    }
}
