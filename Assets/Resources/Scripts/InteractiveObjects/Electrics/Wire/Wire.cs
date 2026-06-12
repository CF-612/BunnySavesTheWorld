using System;
using UnityEngine;

public class Wire : MonoBehaviour, IBiteable
{
    [Header("啃咬属性")]
    [SerializeField] private int maxBiteResistance = 1;
    
    [Header("视觉与特效")]
    [Tooltip("电线被咬断前，每次受到啃咬造成的阶段性损伤特效（如：小电火花）")]
    [SerializeField] private GameObject[] damageVFXPrefabs;
    [Tooltip("电线被完全咬断时生成的强烈火花粒子特效预制体")]
    [SerializeField] private GameObject sparkVFXPrefab;
    
    private int currentResistance;
    private bool isBroken;
    private HingeJoint2D hinge;
    
    // 用于保存父级管理器注入的回调函数
    private Action managerCallback;

    private void Awake()
    {
        hinge = GetComponent<HingeJoint2D>();
        
        currentResistance = maxBiteResistance;
        isBroken = false;
        
        // 移除了冗余的 SpriteRenderer 隐藏代码，因为物理节点预制体不再包含该组件
    }

    public void SetManagerCallback(Action callback)
    {
        managerCallback = callback;
    }

    public void OnBitten()
    {
        if (isBroken) return;

        currentResistance--;

        if (currentResistance <= 0)
        {
            HandleDestruction();
        }
        else
        {
            // 破损阶段的视觉反馈
            PlayDamageVFX();
        }
    }

    public bool GetIsBroken() => isBroken;

    private void PlayDamageVFX()
    {
        // 根据当前的扣血阶段，播放对应的破损特效
        int vfxIndex = maxBiteResistance - currentResistance - 1;
        
        if (damageVFXPrefabs != null && vfxIndex >= 0 && vfxIndex < damageVFXPrefabs.Length)
        {
            if (damageVFXPrefabs[vfxIndex] != null)
            {
                // 在当前电线节点位置生成受损火花/电弧
                Instantiate(damageVFXPrefabs[vfxIndex], transform.position, Quaternion.identity);
            }
        }
    }

    private void HandleDestruction()
    {
        isBroken = true;

        if (hinge != null)
        {
            hinge.enabled = false; // 物理断开
        }

        // 实例化彻底断裂的火花特效
        if (sparkVFXPrefab != null)
        {
            Instantiate(sparkVFXPrefab, transform.position, Quaternion.identity);
        }

        managerCallback?.Invoke();
    }
}