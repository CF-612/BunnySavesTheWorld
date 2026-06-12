using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(PipeController))]
public class PipeVisualGenerator : MonoBehaviour
{
    public enum StraightDrawMode
    {
        Tiled,
        Sliced
    }

    [Header("视觉元素预制体")]
    [Tooltip("起点入口预制体")]
    public GameObject startEntrancePrefab;
    [Tooltip("终点出口预制体")]
    public GameObject endExitPrefab;
    [Tooltip("直道管段预制体（其SpriteRenderer需要支持Tiled或Sliced）")]
    public GameObject straightPipePrefab;
    [Tooltip("90度弯头/拐弯预制体（默认图片开启朝向需为: 右 和 上，呈L形状）")]
    public GameObject cornerPipePrefab;

    [Header("直道拉伸配置")]
    [Tooltip("直道的绘制模式：Tiled（平铺，适合重复拼接的纹理）或 Sliced（九宫格，适合带边缘保护的拉伸纹理）")]
    public StraightDrawMode straightDrawMode = StraightDrawMode.Tiled;

    [Header("间距与对齐设置")]
    [Tooltip("端点或弯头中心到物理接口处的距离补偿，用于缩进直道长度，避免图片重叠")]
    public float segmentOffset = 0.5f;

    [Tooltip("用于归纳和存放所有生成管道贴图的子容器对象")]
    public Transform visualsContainer;

    // 清空当前生成的管道视觉外观
    [ContextMenu("清除管道外观")]
    public void ClearPipelineVisuals()
    {
        EnsureVisualsContainer();

        // 倒序删除子容器下的所有视觉节点，防止漏删
        for (int i = visualsContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = visualsContainer.GetChild(i).gameObject;
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }

    // 主入口：解析路径并生成完整的管道拼接外观
    [ContextMenu("生成/刷新管道外观")]
    public void GeneratePipelineVisuals()
    {
        // 1. 获取同级管道控制器的节点数据
        PipeController controller = GetComponent<PipeController>();
        if (controller == null)
        {
            Debug.LogError("未找到同级的 PipeController 组件！生成终止。");
            return;
        }

        Transform[] waypoints = controller.waypoints;
        if (waypoints == null || waypoints.Length < 2)
        {
            Debug.LogWarning("管道路径节点（Waypoints）数量少于2，无法生成外观。");
            return;
        }

        // 2. 清理旧数据，并准备好容器
        ClearPipelineVisuals();

        // 3. 逐个节点分析并实例化对应组件
        int lastIndex = waypoints.Length - 1;

        // 生成起点端点
        PlaceEndpoint(waypoints[0], startEntrancePrefab);

        // 生成终点端点
        PlaceEndpoint(waypoints[lastIndex], endExitPrefab);

        // 生成中间的所有拐角弯头
        for (int i = 1; i < lastIndex; i++)
        {
            Vector2 prevPos = waypoints[i - 1].position;
            Vector2 currentPos = waypoints[i].position;
            Vector2 nextPos = waypoints[i + 1].position;

            Vector2 inDir = SnapToCardinal((currentPos - prevPos).normalized);
            Vector2 outDir = SnapToCardinal((nextPos - currentPos).normalized);

            // 只有当进入方向和流出方向发生改变时，才需要在此处放置弯头
            if (Vector2.Dot(inDir, outDir) < 0.95f)
            {
                PlaceCorner(currentPos, inDir, outDir);
            }
        }

        // 在相邻节点之间生成平铺的直管段
        for (int i = 0; i < lastIndex; i++)
        {
            Vector2 start = waypoints[i].position;
            Vector2 end = waypoints[i + 1].position;
            Vector2 dir = SnapToCardinal((end - start).normalized);

            PlaceStraightSegment(start, end, dir);
        }

        Debug.Log("管道外观拼接完成！");
    }

    // 确保 visualsContainer 引用有效
    private void EnsureVisualsContainer()
    {
        if (visualsContainer == null)
        {
            Transform existing = transform.Find("Visuals");
            if (existing != null)
            {
                visualsContainer = existing;
            }
            else
            {
                GameObject newContainer = new GameObject("Visuals");
                newContainer.transform.SetParent(transform);
                newContainer.transform.localPosition = Vector3.zero;
                visualsContainer = newContainer.transform;
            }
        }
    }

    // 生成并旋转首尾端点的方法
    private void PlaceEndpoint(Transform node, GameObject prefab)
    {
        if (prefab == null) return;

        GameObject endpointInstance = Instantiate(prefab, node.position, Quaternion.identity, visualsContainer);
        endpointInstance.name = prefab.name;

        // 获取该端点触发器配置的方向，自动计算旋转角度
        Vector2 inputDir = Vector2.down;
        PipeEntrance entrance = endpointInstance.GetComponent<PipeEntrance>();
        if (entrance == null)
        {
            entrance = node.GetComponent<PipeEntrance>();
        }

        if (entrance != null)
        {
            inputDir = entrance.requiredInputDirection;
        }

        if (inputDir.magnitude > 0.01f)
        {
            // 管道口的视觉朝向应该与吸入按键方向相反（例如玩家按向下，管口向上开合）
            Vector2 openDir = -inputDir.normalized;
            float angle = Mathf.Atan2(openDir.y, openDir.x) * Mathf.Rad2Deg;
            
            // 假设端点美术资源默认开口朝右（X轴正方向为0度）
            endpointInstance.transform.rotation = Quaternion.Euler(0, 0, angle);
        }
    }

    // 生成并自动旋转拐角弯头的方法
    private void PlaceCorner(Vector2 position, Vector2 inDirection, Vector2 outDirection)
    {
        if (cornerPipePrefab == null) return;

        GameObject cornerInstance = Instantiate(cornerPipePrefab, position, Quaternion.identity, visualsContainer);
        cornerInstance.name = "Corner_Segment";

        // 弯头连接的两个开放口：一个指向前一个节点，一个指向下一个节点
        Vector2 port1 = -inDirection;
        Vector2 port2 = outDirection;

        // 计算弯头朝向的合成中心向量
        Vector2 averageDir = (port1 + port2).normalized;
        float targetAngle = Mathf.Atan2(averageDir.y, averageDir.x) * Mathf.Rad2Deg;

        // 数学推导：
        // 假设默认的L型弯头贴图开口朝向为“右”(1, 0)和“上”(0, 1)。
        // 此时，开口的合成中心向量为(1, 1).normalized，在2D极坐标中对应的角度为45度。
        // 当需要将其旋转至实际需要的 averageDir 角度 targetAngle 时：
        // 需要旋转的角度 rotationAngle = targetAngle - 45度。
        float rotationAngle = targetAngle - 45f;

        cornerInstance.transform.rotation = Quaternion.Euler(0, 0, rotationAngle);
    }

    // 生成自适应长度且平铺的直管段的方法
    private void PlaceStraightSegment(Vector2 start, Vector2 end, Vector2 direction)
    {
        if (straightPipePrefab == null) return;

        float distance = Vector2.Distance(start, end);
        float actualLength = distance - (segmentOffset * 2f);

        // 如果距离太近导致直道长度过短（小于或等于两端缩进值之和），则跳过该段生成，防止缩放报错
        if (actualLength <= 0.01f) return;

        Vector2 centerPos = start + direction * (distance / 2f);
        GameObject straightInstance = Instantiate(straightPipePrefab, centerPos, Quaternion.identity, visualsContainer);
        straightInstance.name = "Straight_Segment";

        // 修改直道的旋转角：
        // 假设直道预制体的默认切图方向是垂直向上的（即默认沿Y轴延伸，对应的极坐标角度为90度）。
        // 当我们需要它朝上（0,1）时，计算出的 Atan2 角度是90度，此时旋转应为 90 - 90 = 0度（保持原样）。
        // 当我们需要它朝右（1,0）时，计算出的 Atan2 角度是0度，此时旋转应为 0 - 90 = -90度（即顺时针转90度）。
        float angle = (Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg) - 90f;
        straightInstance.transform.rotation = Quaternion.Euler(0, 0, angle);

        // 修改直道 SpriteRenderer 的 size.y 来控制长度，配合 Tiled/Sliced 模式无畸变拉伸
        SpriteRenderer sr = straightInstance.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            // 根据面板中的配置，动态应用 Tiled（平铺重复）或 Sliced（九宫格缩放）
            sr.drawMode = (straightDrawMode == StraightDrawMode.Tiled) ? SpriteDrawMode.Tiled : SpriteDrawMode.Sliced;
            
            // 由于贴图默认是竖着的，其拉伸轴应该对应 Y 轴（size.y），宽度（size.x）保持原有设定不变
            sr.size = new Vector2(sr.size.x, actualLength);
        }
    }

    // 辅助函数：将方向向量吸附至纯横向或纯纵向，过滤拖拽节点时的微小浮点误差
    private Vector2 SnapToCardinal(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y))
        {
            return new Vector2(Mathf.Sign(v.x), 0f);
        }
        else
        {
            return new Vector2(0f, Mathf.Sign(v.y));
        }
    }
}