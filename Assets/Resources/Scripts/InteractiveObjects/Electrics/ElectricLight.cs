using UnityEngine;

public class ElectricLight : EntityEle
{
    [Header("电灯光源")]
    [Tooltip("可以拖入光源物体，断电时会自动隐藏该物体")]
    [SerializeField] private GameObject lightSourceObj;

    public override void TurnOn()
    {
        // 先执行基类的状态变更与动画切换
        base.TurnOn();
        
        // 再执行电灯独有的逻辑
        if (lightSourceObj != null)
            lightSourceObj.SetActive(true);
    }

    public override void TurnOff()
    {
        base.TurnOff();
        
        if (lightSourceObj != null)
            lightSourceObj.SetActive(false);
    }
}