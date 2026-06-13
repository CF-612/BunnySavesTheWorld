using UnityEngine;

public class Wire : EntityWire
{
    private HingeJoint2D hinge;

    protected override void Awake()
    {
        base.Awake();
        hinge = GetComponent<HingeJoint2D>();
    }

    protected override void HandleDestruction()
    {
        // 物理断开铰链
        if (hinge != null)
        {
            hinge.enabled = false;
        }

        // 基类处理：标记破损 + 断裂火花 + 触发事件
        base.HandleDestruction();
    }
}
