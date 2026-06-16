using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 通用触发器事件：玩家进入/离开 Trigger 时触发 UnityEvent。
/// 挂载在任意带 Collider2D (IsTrigger) 的 GameObject 上。
/// </summary>
public class TriggerEvent : MonoBehaviour
{
    [Header("玩家标签")]
    public string playerTag = "Player";

    [Header("触发一次")]
    [Tooltip("勾选后只触发一次，之后组件自动禁用")]
    public bool triggerOnce;

    public UnityEvent OnPlayerEnter;
    public UnityEvent OnPlayerExit;

    private bool hasTriggered;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggerOnce && hasTriggered) return;

        hasTriggered = true;
        OnPlayerEnter?.Invoke();

        if (triggerOnce) enabled = false;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        OnPlayerExit?.Invoke();
    }
}
