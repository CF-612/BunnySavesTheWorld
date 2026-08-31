using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Persistent owner for scene loading and the fullscreen fade overlay.
/// Callers no longer need to create duplicate canvases or continue coroutines after their scene is unloaded.
/// </summary>
public sealed class SceneTransitionService : MonoBehaviour
{
    private const int OverlaySortingOrder = 32000;

    private static SceneTransitionService instance;
    private Image fadeImage;
    private CanvasGroup fadeGroup;
    private Coroutine fadeCoroutine;

    public static bool IsTransitioning { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        IsTransitioning = false;
    }

    /// <summary>
    /// Fades out, loads a build scene asynchronously, records the entered scene, and fades back in.
    /// The persistent host keeps this coroutine alive while the calling scene is unloaded.
    /// </summary>
    public static void LoadScene(
        string sceneName,
        float fadeOutDuration = 1f,
        float holdBlackDuration = 0.2f,
        float fadeInDuration = 1f,
        AudioClip transitionSfx = null)
    {
        if (IsTransitioning)
            return;

        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"无法加载场景“{sceneName}”：请确认场景名称和 Build Settings。", instance);
            return;
        }

        EnsureInstance().StartCoroutine(instance.LoadSceneRoutine(
            sceneName,
            Mathf.Max(0f, fadeOutDuration),
            Mathf.Max(0f, holdBlackDuration),
            Mathf.Max(0f, fadeInDuration),
            transitionSfx));
    }

    /// <summary>Used by scenes opened directly in the Editor to reveal them from black.</summary>
    public static void FadeIn(float duration)
    {
        SceneTransitionService service = EnsureInstance();
        if (IsTransitioning)
            return;

        service.SetAlpha(1f);
        service.StartFade(0f, duration, false);
    }

    private static SceneTransitionService EnsureInstance()
    {
        if (instance != null)
            return instance;

        GameObject host = new GameObject(nameof(SceneTransitionService));
        instance = host.AddComponent<SceneTransitionService>();
        DontDestroyOnLoad(host);
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        CreateOverlay();
    }

    private void OnApplicationQuit()
    {
        Player player = FindAnyObjectByType<Player>();
        if (player != null)
            GameProgressService.RecordPlayerPosition(SceneManager.GetActiveScene().name, player.transform.position);
    }

    private IEnumerator LoadSceneRoutine(
        string sceneName,
        float fadeOutDuration,
        float holdBlackDuration,
        float fadeInDuration,
        AudioClip transitionSfx)
    {
        IsTransitioning = true;
        fadeGroup.blocksRaycasts = true;

        Player player = FindAnyObjectByType<Player>();
        if (player != null)
            GameProgressService.RecordPlayerPosition(SceneManager.GetActiveScene().name, player.transform.position);

        yield return FadeRoutine(1f, fadeOutDuration);

        if (transitionSfx != null && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(transitionSfx);

        if (holdBlackDuration > 0f)
            yield return new WaitForSecondsRealtime(holdBlackDuration);

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError($"创建场景加载操作失败：{sceneName}", this);
            yield return FadeRoutine(0f, fadeInDuration);
            fadeGroup.blocksRaycasts = false;
            IsTransitioning = false;
            yield break;
        }

        while (!operation.isDone)
            yield return null;

        GameProgressService.MarkSceneEntered(sceneName);
        yield return FadeRoutine(0f, fadeInDuration);

        fadeGroup.blocksRaycasts = false;
        IsTransitioning = false;
    }

    private void StartFade(float targetAlpha, float duration, bool blockRaycasts)
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        fadeGroup.blocksRaycasts = blockRaycasts;
        fadeCoroutine = StartCoroutine(FadeOnlyRoutine(targetAlpha, Mathf.Max(0f, duration)));
    }

    private IEnumerator FadeOnlyRoutine(float targetAlpha, float duration)
    {
        yield return FadeRoutine(targetAlpha, duration);
        fadeCoroutine = null;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        float startAlpha = fadeImage.color.a;
        if (duration <= 0f)
        {
            SetAlpha(targetAlpha);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration));
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void CreateOverlay()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        fadeGroup = gameObject.AddComponent<CanvasGroup>();
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        GameObject imageObject = new GameObject("FadeOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        fadeImage = imageObject.GetComponent<Image>();
        fadeImage.color = Color.clear;
        fadeImage.raycastTarget = true;

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void SetAlpha(float alpha)
    {
        Color color = fadeImage.color;
        color.a = Mathf.Clamp01(alpha);
        fadeImage.color = color;
    }
}
