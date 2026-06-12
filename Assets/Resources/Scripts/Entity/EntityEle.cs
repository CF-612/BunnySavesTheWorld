using UnityEngine;

public class EntityEle : MonoBehaviour
{
    [Header("基础电器设置")]
    public bool isOn = true;

    [Header("基础组件引用")]
    [SerializeField] protected Animator anim;
    [SerializeField] protected string animBoolName = "isOn";

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