using UnityEngine;

/// <summary>Scene authoring adapter for the shared persistent fade service.</summary>
public class SceneEntrance : MonoBehaviour
{
    [Header("淡入设置")]
    [Tooltip("黑屏淡出时长（秒），建议和 ScenePortal 的 fadeDuration 保持一致")]
    public float fadeDuration = 1f;

    private void Start()
    {
        SceneTransitionService.FadeIn(fadeDuration);
    }
}
