using UnityEngine;

public class ElectricGrid : EntityEle
{
    [Header("电网独占设置")]
    [Tooltip("断电时需要解除阻挡的物理碰撞体（不要把检测伤害的Trigger也放进来）")]
    [SerializeField] private Collider2D[] blockColliders;

    public override void TurnOn()
    {
        base.TurnOn();
        SetCollidersEnabled(true);
    }

    public override void TurnOff()
    {
        base.TurnOff();
        SetCollidersEnabled(false);
    }

    private void SetCollidersEnabled(bool state)
    {
        if (blockColliders == null) return;
        
        foreach (var col in blockColliders)
        {
            if (col != null)
            {
                col.enabled = state;
            }
        }
    }
}