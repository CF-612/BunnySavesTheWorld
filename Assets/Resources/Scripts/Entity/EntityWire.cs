using UnityEngine;
using UnityEngine.Events;

public class EntityWire : MonoBehaviour, IBiteable
{
    [Header("啃咬属性")]
    [SerializeField] protected int maxBiteResistance = 1;

    [Header("视觉与特效")]
    [Tooltip("电线被咬断前，每次受到啃咬造成的阶段性损伤特效（如：小电火花）")]
    [SerializeField] protected GameObject[] damageVFXPrefabs;
    [Tooltip("电线被完全咬断时生成的强烈火花粒子特效预制体")]
    [SerializeField] protected GameObject sparkVFXPrefab;

    [Header("断裂事件")]
    [Tooltip("电线完全断裂时触发，可直接连线至 EntityEle.OnControllingWireBroken()")]
    public UnityEvent onWireBroken;

    protected int currentResistance;
    protected bool isBroken;

    protected virtual void Awake()
    {
        currentResistance = maxBiteResistance;
        isBroken = false;
    }

    public virtual void OnBitten()
    {
        if (isBroken) return;

        currentResistance--;

        if (currentResistance <= 0)
        {
            HandleDestruction();
        }
        else
        {
            PlayDamageVFX();
        }
    }

    public bool GetIsBroken() => isBroken;

    protected void PlayDamageVFX()
    {
        // 根据当前的扣血阶段，播放对应的破损特效
        int vfxIndex = maxBiteResistance - currentResistance - 1;

        if (damageVFXPrefabs != null && vfxIndex >= 0 && vfxIndex < damageVFXPrefabs.Length)
        {
            if (damageVFXPrefabs[vfxIndex] != null)
            {
                Instantiate(damageVFXPrefabs[vfxIndex], transform.position, Quaternion.identity);
            }
        }
    }

    /// <summary>
    /// 断裂处理。派生类重写时应先处理自身逻辑，再调用 base.HandleDestruction()。
    /// </summary>
    protected virtual void HandleDestruction()
    {
        isBroken = true;

        // 播放电线断裂音效
        AudioManager.Instance?.PlaySFX("Audio/SFX/InteractiveObjects/ElectricEffects/WireBroken");

        // 实例化彻底断裂的火花特效
        if (sparkVFXPrefab != null)
        {
            Instantiate(sparkVFXPrefab, transform.position, Quaternion.identity);
        }

        onWireBroken?.Invoke();
    }
}
