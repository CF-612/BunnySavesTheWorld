using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 啃咬目标追踪器：追踪多根电线的啃咬进度，全部咬断后触发事件。
/// 挂载在场景空物体上，每根电线的 onWireBroken 事件连线到 ReportBroken()。
/// </summary>
public class BiteObjectiveTracker : MonoBehaviour
{
    [Header("目标数量")]
    [Tooltip("需要咬断的电线总数（必须与实际连线的电线数量一致）")]
    public int totalObjectives = 1;

    [Header("全部完成回调")]
    [Tooltip("所有电线咬断后触发，可连线到 ScenePortal.TriggerNow() 等")]
    public UnityEvent OnAllObjectivesComplete;

    private int brokenCount;

    private void Start()
    {
        brokenCount = 0;
    }

    /// <summary>由每根电线的 onWireBroken 事件调用，内部计数</summary>
    public void ReportBroken()
    {
        brokenCount++;
        Debug.Log($"[BiteObjectiveTracker] 电线断裂：{brokenCount}/{totalObjectives}");

        if (brokenCount >= totalObjectives)
        {
            Debug.Log("[BiteObjectiveTracker] 全部目标完成！触发 OnAllObjectivesComplete");
            OnAllObjectivesComplete?.Invoke();
        }
    }
}
