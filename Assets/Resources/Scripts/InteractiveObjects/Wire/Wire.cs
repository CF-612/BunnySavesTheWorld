using System;
using UnityEngine;

public class Wire : MonoBehaviour, IBiteable
{
    [Header("啃咬属性")]
    [SerializeField] private int maxBiteResistance = 1;
    [SerializeField] private Sprite[] damageSprites;
    
    private int currentResistance;
    private bool isBroken;

    private SpriteRenderer sr;
    private HingeJoint2D hinge;
    
    // 用于保存父级管理器注入的回调函数
    private Action managerCallback;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        hinge = GetComponent<HingeJoint2D>();
        
        currentResistance = maxBiteResistance;
        isBroken = false;
    }

    // 由 WireManager 在 Start 时调用注入
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
            UpdateDamageSprite();
        }
    }

    public bool GetIsBroken() => isBroken;

    private void UpdateDamageSprite()
    {
        int spriteIndex = maxBiteResistance - currentResistance - 1;
        
        if (damageSprites != null && spriteIndex >= 0 && spriteIndex < damageSprites.Length)
        {
            sr.sprite = damageSprites[spriteIndex];
        }
    }

    private void HandleDestruction()
    {
        isBroken = true;

        if (hinge != null)
        {
            hinge.enabled = false;
        }

        // 呼叫父级管理器，报告自己已断裂
        managerCallback?.Invoke();
    }
}