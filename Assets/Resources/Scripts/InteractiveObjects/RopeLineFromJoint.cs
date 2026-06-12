using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class RopeLineFromJoint : MonoBehaviour
{
    [Header("木板物体（挂有两个 Distance Joint 2D）")]
    public GameObject platform;          // 拖入木板

    [Header("选择使用第几个 Joint（0 = 第一个, 1 = 第二个）")]
    public int jointIndex = 0;           // 左侧绳子填 0，右侧绳子填 1

    private LineRenderer lineRenderer;
    private DistanceJoint2D selectedJoint;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;

        if (platform == null)
        {
            Debug.LogError("请指定木板物体！");
            return;
        }

        DistanceJoint2D[] joints = platform.GetComponents<DistanceJoint2D>();
        if (joints.Length < jointIndex + 1)
        {
            Debug.LogError($"木板上的 DistanceJoint2D 数量不足 {jointIndex + 1}，请检查！");
            return;
        }

        selectedJoint = joints[jointIndex];
    }

    void Update()
    {
        if (selectedJoint == null) return;

        // 起点：connectedBody（固定锚点）位置 + connectedAnchor 偏移
        Transform connectedBody = selectedJoint.connectedBody.transform;
        Vector2 startWorld = connectedBody.TransformPoint(selectedJoint.connectedAnchor);

        // 终点：挂载 Joint 的物体（木板）位置 + anchor 偏移
        Transform thisBody = selectedJoint.transform;
        Vector2 endWorld = thisBody.TransformPoint(selectedJoint.anchor);

        lineRenderer.SetPosition(0, startWorld);
        lineRenderer.SetPosition(1, endWorld);
    }
}