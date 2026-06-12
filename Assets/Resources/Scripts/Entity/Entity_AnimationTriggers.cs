using UnityEngine;

public class Entity_AnimationTriggers : MonoBehaviour
{
    private Entity entity;

    protected virtual void Awake()
    {
        entity = GetComponentInParent<Entity>();
    }

    // 动画结束那一帧的 Animation Event 调用，用于结束当前状态
    protected virtual void CurrentStateTrigger()
    {
        entity.CurrentStateAnimationTrigger();
    }

    // 动作发力/命中那一帧的 Animation Event 调用，触发具体行为（如啃咬、跺脚）
    protected virtual void ActionTrigger()
    {
        entity.CurrentStateActionTrigger();
    }
}
