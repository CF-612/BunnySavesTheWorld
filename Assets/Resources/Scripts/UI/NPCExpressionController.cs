using UnityEngine;

/// <summary>
/// NPC 表情控制器：通过 Animator bool 参数切换表情动画。
/// 挂在 NPC 的 Animator 所在 GameObject 上，公开方法供 UnityEvent / NodeCanvas 调用。
/// </summary>
public class NPCExpressionController : MonoBehaviour
{
    [Header("Animator")]
    public Animator animator;

    [Header("表情参数名")]
    public string smileParam = "smile";
    public string seeParam = "see";
    public string seesmileParam = "seesmile";

    /// <summary>切换到 Smile 表情</summary>
    public void SetSmile()
    {
        if (animator == null) return;
        animator.SetBool(smileParam, true);
        animator.SetBool(seeParam, false);
        animator.SetBool(seesmileParam, false);
    }

    /// <summary>切换到 See 表情</summary>
    public void SetSee()
    {
        if (animator == null) return;
        animator.SetBool(smileParam, false);
        animator.SetBool(seeParam, true);
        animator.SetBool(seesmileParam, false);
    }

    /// <summary>切换到 SeeSmile 表情</summary>
    public void SetSeeSmile()
    {
        if (animator == null) return;
        animator.SetBool(smileParam, false);
        animator.SetBool(seeParam, false);
        animator.SetBool(seesmileParam, true);
    }

    /// <summary>重置为 Normal（全部 false）</summary>
    public void ResetExpression()
    {
        if (animator == null) return;
        animator.SetBool(smileParam, false);
        animator.SetBool(seeParam, false);
        animator.SetBool(seesmileParam, false);
    }
}
