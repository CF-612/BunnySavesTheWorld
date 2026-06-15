using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(AudioSource))]
public class Blower : EntityEle
{
    [Header("吹风机独占设置")]
    [SerializeField] private ParticleSystem windVFX;

    private AudioSource fanAudioSource;
    private AreaEffector2D windZone;

    protected override void Awake()
    {
        // 调用基类 Awake 获取动画器
        base.Awake();

        windZone = GetComponentInChildren<AreaEffector2D>();

        fanAudioSource = GetComponent<AudioSource>();
        if (fanAudioSource != null)
        {
            fanAudioSource.loop = true;
            fanAudioSource.playOnAwake = false;
        }
    }

    public override void TurnOn()
    {
        base.TurnOn();

        // 播放风扇运转音效（本地 AudioSource，自动距离衰减）
        if (fanAudioSource != null && !fanAudioSource.isPlaying)
            fanAudioSource.Play();

        if (windVFX != null && !windVFX.isPlaying)
            windVFX.Play();

        windZone.enabled = true;
    }

    public override void TurnOff()
    {
        base.TurnOff();

        // 停止风扇音效
        if (fanAudioSource != null)
            fanAudioSource.Stop();

        if (windVFX != null && windVFX.isPlaying)
            windVFX.Stop();

        windZone.enabled = false;
    }
}