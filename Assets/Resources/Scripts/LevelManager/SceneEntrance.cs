using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 场景入场黑屏淡出：新场景加载后从全黑淡出到正常画面。
/// 挂载在场景中任意 GameObject 上即可（建议放在 SceneController 上）。
/// </summary>
public class SceneEntrance : MonoBehaviour
{
    [Header("淡入设置")]
    [Tooltip("黑屏淡出时长（秒），建议和 ScenePortal 的 fadeDuration 保持一致")]
    public float fadeDuration = 1f;

    private void Start()
    {
        StartCoroutine(FadeInCoroutine());
    }

    private IEnumerator FadeInCoroutine()
    {
        // 创建全屏黑遮罩
        GameObject canvasGO = new GameObject("FadeInCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageGO = new GameObject("BlackImage");
        imageGO.transform.SetParent(canvasGO.transform);

        Image image = imageGO.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 淡出：黑屏 → 透明
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            SetAlpha(image, alpha);
            yield return null;
        }

        // 清理
        Destroy(canvasGO);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }
}
