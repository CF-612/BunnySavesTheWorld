using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(AudioSource))]
public class Blower : EntityEle
{
    [Header("视觉特效")]
    [SerializeField] private ParticleSystem windVFX;

    [Header("物理辅助（可选）")]
    [Tooltip("AreaEffector2D 仅用于推动非玩家的轻物体（如碎片），不对玩家生效。留空则忽略。")]
    [SerializeField] private AreaEffector2D windZone;

    private AudioSource fanAudioSource;
    private WindZoneData windZoneData;
    private Collider2D windTriggerCollider;

    protected override void Awake()
    {
        base.Awake();

        // 获取风力数据组件（挂载在本物体下的子物体上，与 Trigger Collider 同体）
        windZoneData = GetComponentInChildren<WindZoneData>();
        if (windZoneData != null)
        {
            windTriggerCollider = windZoneData.GetComponent<Collider2D>();
        }

        // 自动查找子物体上的 AreaEffector2D（如果没有手动指定）
        if (windZone == null)
            windZone = GetComponentInChildren<AreaEffector2D>();

        // 同步 AreaEffector2D 的风力方向与 WindZoneData 一致
        SyncAreaEffectorDirection();

        fanAudioSource = GetComponent<AudioSource>();
        if (fanAudioSource != null)
        {
            fanAudioSource.loop = true;
            fanAudioSource.playOnAwake = false;
        }
    }

    /// <summary>
    /// 将 AreaEffector2D 的 forceAngle 同步为 WindZoneData 的风向，
    /// 避免两套力方向不一致导致的混乱。
    /// </summary>
    private void SyncAreaEffectorDirection()
    {
        if (windZone == null || windZoneData == null) return;

        Vector2 windDir = windZoneData.WindDirection;
        float angle = Mathf.Atan2(windDir.y, windDir.x) * Mathf.Rad2Deg;
        windZone.forceAngle = angle;
    }

    public override void TurnOn()
    {
        base.TurnOn();

        // 播放风扇运转音效
        if (fanAudioSource != null && !fanAudioSource.isPlaying)
            fanAudioSource.Play();

        // 粒子特效
        if (windVFX != null && !windVFX.isPlaying)
            windVFX.Play();

        // 启用代码层风力数据 + 触发器（玩家交互的核心）
        if (windZoneData != null)
        {
            windZoneData.enabled = true;
            if (windTriggerCollider != null)
                windTriggerCollider.enabled = true;
        }

        // 启用 AreaEffector2D（仅影响非玩家轻物体，可选）
        if (windZone != null)
            windZone.enabled = true;
    }

    public override void TurnOff()
    {
        base.TurnOff();

        if (fanAudioSource != null)
            fanAudioSource.Stop();

        if (windVFX != null && windVFX.isPlaying)
            windVFX.Stop();

        if (windZoneData != null)
        {
            windZoneData.enabled = false;
            if (windTriggerCollider != null)
                windTriggerCollider.enabled = false;
        }

        if (windZone != null)
            windZone.enabled = false;
    }
}
