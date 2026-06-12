using System;
using UnityEngine;
using UnityEngine.Events;

public class WireManager : MonoBehaviour
{
    public enum WireLinkType
    {
        Blower,
        UnityEvent
    }

    [Header("生成配置")]
    public GameObject wirePrefab;
    public int segmentCount = 5;
    public float segmentSpacing = 0.5f;

    [Header("联动配置")]
    public WireLinkType linkType = WireLinkType.Blower;
    public Blower linkedBlower;
    public UnityEvent onChainBroken;

    private bool hasBroken = false;

    private void Start()
    {
        BindEvents();
    }

    // 在游戏启动时，遍历所有子节点，将管理器的触发方法注入给它们
    private void BindEvents()
    {
        Wire[] wires = GetComponentsInChildren<Wire>();
        foreach (Wire w in wires)
        {
            w.SetManagerCallback(OnAnyWireBroken);
        }
    }

    // 接收子节点断裂汇报的核心方法
    private void OnAnyWireBroken()
    {
        // 全局单次触发保护，防止多节电线断裂引发重复触发
        if (hasBroken) return;
        
        hasBroken = true;

        // 根据选择的多态类型执行对应的联动逻辑
        switch (linkType)
        {
            case WireLinkType.Blower:
                if (linkedBlower != null)
                {
                    linkedBlower.TurnOff();
                }
                break;
            case WireLinkType.UnityEvent:
                onChainBroken?.Invoke();
                break;
        }
    }

    // 在 Inspector 面板右键点击该脚本，选择此项即可在编辑器中自动生成
    [ContextMenu("生成物理电线链条")]
    private void GenerateWires()
    {
        if (wirePrefab == null)
        {
            Debug.LogError("未指派电线预制体，无法生成！");
            return;
        }

        if (wirePrefab.GetComponent<Rigidbody2D>() == null || wirePrefab.GetComponent<HingeJoint2D>() == null)
        {
            Debug.LogError("电线预制体缺少 Rigidbody2D 或 HingeJoint2D 组件！");
            return;
        }

        // 清除现有的所有子物体（倒序删除防止索引越界）
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        Rigidbody2D parentRb = GetComponent<Rigidbody2D>();
        Rigidbody2D previousRb = parentRb;

        // 依次生成节点并链接
        for (int i = 0; i < segmentCount; i++)
        {
            // 实例化并设置父物体
            GameObject segment = Instantiate(wirePrefab, transform);
            segment.name = "WireSegment_" + i;
            
            // 计算向下的偏移位置
            segment.transform.localPosition = new Vector3(0, -segmentSpacing * (i + 1), 0);

            HingeJoint2D hinge = segment.GetComponent<HingeJoint2D>();
            Rigidbody2D currentRb = segment.GetComponent<Rigidbody2D>();

            // 连接到上一个刚体（如果是第0个，且父物体没有刚体，previousRb为null，此时铰链会固定在世界坐标）
            hinge.connectedBody = previousRb;
            
            // 自动对齐铰链锚点（根据你的贴图和设计，这里可能需要微调）
            hinge.autoConfigureConnectedAnchor = true;

            // 当前刚体成为下一个节点的连接目标
            previousRb = currentRb;
        }
        
        Debug.Log($"成功生成 {segmentCount} 节电线！");
    }
}
