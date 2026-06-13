using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Blower : EntityEle
{
    [Header("吹风机独占设置")]
    [SerializeField] private ParticleSystem windVFX;

    private AreaEffector2D windZone;

    protected override void Awake()
    {
        // 调用基类 Awake 获取动画器
        base.Awake();
        
        windZone = GetComponentInChildren<AreaEffector2D>();
    }

    public override void TurnOn()
    {
        base.TurnOn();
            
        if (windVFX != null && !windVFX.isPlaying) 
            windVFX.Play();

        windZone.enabled = true;
    }

    public override void TurnOff()
    {
        base.TurnOff();
            
        if (windVFX != null && windVFX.isPlaying) 
            windVFX.Stop();

        windZone.enabled = false;
    }
}