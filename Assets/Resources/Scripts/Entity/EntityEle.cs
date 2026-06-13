using UnityEngine;

public class EntityEle : MonoBehaviour
{
    [Header("基础电器设置")]
    public bool isOn = true;

    [Header("电线控制设置")]
    [Tooltip("关闭/开启此电器所需啃咬的电线数量。达到此数量后电器自动切换状态。")]
    [SerializeField] private int requiredWires = 1;

    [Header("基础组件引用")]
    [SerializeField] protected Animator anim;
    [SerializeField] protected string animBoolName = "isOn";

    private int brokenWireCount = 0;

    protected virtual void Awake()
    {
        if (anim == null)
            anim = GetComponent<Animator>();
    }

    protected virtual void Start()
    {
        // 根据初始状态进行激活或关闭
        if (isOn)
            TurnOn();
        else
            TurnOff();
    }

    /// <summary>
    /// 由 WireManager.onChainBroken UnityEvent 调用。
    /// 每咬断一根电线时计数+1，达到 requiredWires 后自动切换电器状态。
    /// </summary>
    public void OnControllingWireBroken()
    {
        brokenWireCount++;

        if (brokenWireCount >= requiredWires)
        {
            if (isOn)
                TurnOff();
            else
                TurnOn();
        }
    }

    // 声明为虚方法，允许子类重写并添加自己的独有逻辑
    public virtual void TurnOn()
    {
        isOn = true;

        if (anim != null && !string.IsNullOrEmpty(animBoolName))
            anim.SetBool(animBoolName, true);
    }

    // 声明为虚方法，允许子类重写并添加自己的独有逻辑
    public virtual void TurnOff()
    {
        isOn = false;

        if (anim != null && !string.IsNullOrEmpty(animBoolName))
            anim.SetBool(animBoolName, false);
    }
}