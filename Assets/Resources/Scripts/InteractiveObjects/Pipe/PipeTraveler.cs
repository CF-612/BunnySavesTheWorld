using System;
using System.Collections;
using UnityEngine;

public class PipeTraveler : MonoBehaviour
{
    [Header("弹性缩放设置")]
    [Tooltip("吸入和吐出时的弹性挤压形变时间")]
    [SerializeField] private float squeezeDuration = 0.2f;
    [Tooltip("压缩和拉伸本地缩放（Scale）的比例幅度")]
    [SerializeField] private float squeezeAmount = 0.4f;

    [Header("特定表现贴图")]
    [Tooltip("管道内及吐出瞬间强制显示的特定静态贴图（如：兔条）。若为空则保持进入前的最后一帧不变")]
    [SerializeField] private Sprite travelSprite;

    [Header("物理力学设置")]
    [Tooltip("在管道出口喷出时的物理初速度")]
    [SerializeField] private float exitEjectForce = 12f;

    private Rigidbody2D rb;
    private SpriteRenderer[] renderers;
    private Animator[] animators;
    
    // 专门用于存放视觉表现所在的物体层级，避开父类脚本的干扰
    private Transform visualTransform; 
    private Vector3 originalVisualScale;
    private bool isTraveling;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void StartTravel(Transform[] path, float speed, bool isStart, Vector2 entryDir, Action<Vector2> onPrepareToExit, Action onComplete)
    {
        if (isTraveling) return;
        StartCoroutine(TravelCoroutine(path, speed, isStart, entryDir, onPrepareToExit, onComplete));
    }

    private IEnumerator TravelCoroutine(Transform[] path, float speed, bool isStart, Vector2 entryDir, Action<Vector2> onPrepareToExit, Action onComplete)
    {
        isTraveling = true;

        // 1. 深度检索所有的视觉组件并锁定视觉子物体
        FindVisualComponents();
        if (visualTransform != null)
        {
            originalVisualScale = visualTransform.localScale;
        }
        else
        {
            originalVisualScale = Vector3.one;
        }

        // 2. 缓存并禁用所有的碰撞体
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        bool[] originalColliderStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            originalColliderStates[i] = colliders[i].enabled;
            colliders[i].enabled = false;
        }

        // 3. 托管刚体
        RigidbodyType2D originalBodyType = RigidbodyType2D.Dynamic;
        if (rb != null)
        {
            originalBodyType = rb.bodyType;
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.linearVelocity = Vector2.zero;
        }

        // 4. 吸附至起始管口坐标
        int currentIndex = isStart ? 0 : path.Length - 1;
        transform.position = path[currentIndex].position;

        // 5. 吸入阶段：暂停 Animator，对独立的视觉子物体进行形变
        SetAnimatorsActive(false);

        // 如果配置了专用管道贴图，在隐身前强制替换，规避原有的动作帧残影
        if (travelSprite != null && renderers != null && renderers.Length > 0)
        {
            renderers[0].sprite = travelSprite;
        }
        
        if (visualTransform != null)
        {
            Vector3 squeezeScale = new Vector3(
                originalVisualScale.x * (1f - squeezeAmount),
                originalVisualScale.y * (1f + squeezeAmount),
                originalVisualScale.z
            );
            yield return StartCoroutine(LerpScale(visualTransform, originalVisualScale, squeezeScale, squeezeDuration * 0.5f));
            yield return StartCoroutine(LerpScale(visualTransform, squeezeScale, originalVisualScale, squeezeDuration * 0.5f));
        }

        // 6. 隐身防穿帮（通过彻底关闭子节点，杜绝被其他脚本强制重置可见性的可能）
        SetVisualsActive(false);

        // 7. 沿预设路径节点平滑滑行
        int step = isStart ? 1 : -1;
        int endIndex = isStart ? path.Length : -1;

        while (currentIndex != endIndex)
        {
            Vector3 targetPoint = path[currentIndex].position;
            while (Vector3.Distance(transform.position, targetPoint) > 0.05f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, speed * Time.deltaTime);
                yield return null;
            }
            currentIndex += step;
        }

        // 8. 获取最终吐出时的方向向量
        int penLastIndex = isStart ? path.Length - 2 : 1;
        int lastIndex = isStart ? path.Length - 1 : 0;
        Vector2 exitDirection = ((Vector2)path[lastIndex].position - (Vector2)path[penLastIndex].position).normalized;

        // 9. 触发预退出回调。让外部（如玩家）有机会在此刻提前设置朝向
        // 注意：此时 Animator 依然是关闭的！这保证了无论玩家怎么修改面向，即将显示的画面始终是 travelSprite 兔条
        onPrepareToExit?.Invoke(exitDirection);

        // 10. 到达终点准备喷出，重新显形（此时显形的就是转向正确的静态兔条）
        SetVisualsActive(true);

        // 11. 吐出阶段：弹性压扁拉伸形变
        if (visualTransform != null)
        {
            Vector3 stretchScale = new Vector3(
                originalVisualScale.x * (1f + squeezeAmount),
                originalVisualScale.y * (1f - squeezeAmount),
                originalVisualScale.z
            );
            yield return StartCoroutine(LerpScale(visualTransform, originalVisualScale, stretchScale, squeezeDuration * 0.5f));
            yield return StartCoroutine(LerpScale(visualTransform, stretchScale, originalVisualScale, squeezeDuration * 0.5f));
        }

        // 12. 恢复碰撞与刚体物理属性，并给予喷出惯性
        for (int i = 0; i < colliders.Length; i++)
        {
            colliders[i].enabled = originalColliderStates[i];
        }
        if (rb != null)
        {
            rb.bodyType = originalBodyType;
            rb.linearVelocity = exitDirection * exitEjectForce;
        }

        // 13. 彻底退出管道，形变结束，此时恢复 Animator 运转，由动画器接管后续正常动作
        SetAnimatorsActive(true);

        isTraveling = false;
        onComplete?.Invoke();
    }

    private void FindVisualComponents()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        animators = GetComponentsInChildren<Animator>(true);

        // 提取视觉层的主节点
        if (renderers != null && renderers.Length > 0)
        {
            visualTransform = renderers[0].transform;
        }
        else
        {
            visualTransform = this.transform;
        }
    }

    private void SetAnimatorsActive(bool active)
    {
        if (animators == null) return;
        foreach (var anim in animators)
        {
            if (anim != null) anim.enabled = active;
        }
    }

    private void SetVisualsActive(bool active)
    {
        if (renderers == null) return;
        foreach (var sr in renderers)
        {
            if (sr != null)
            {
                // 如果图片位于子物体，直接停用整个 GameObject，这是最稳妥的防干扰隐身策略
                if (sr.gameObject != this.gameObject)
                {
                    sr.gameObject.SetActive(active);
                }
                else
                {
                    // 退底方案：如果图片直接挂在本体上，则只关闭渲染组件
                    sr.enabled = active;
                }
            }
        }
    }

    private IEnumerator LerpScale(Transform targetTr, Vector3 startScale, Vector3 targetScale, float duration)
    {
        float time = 0;
        while (time < duration)
        {
            targetTr.localScale = Vector3.Lerp(startScale, targetScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        targetTr.localScale = targetScale;
    }
}