using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScenePortal : MonoBehaviour
{
    [Header("要进入的场景 Scene")]
    public string targetSceneName;

    [Header("玩家标签 Player Tag")]
    public string playerTag = "Player";

    [Header("交互提示 UI Interact UI")]
    public GameObject interactUI;

    [Header("剧情 UI 列表 Story UI List")]
    public GameObject[] storyUIs;

    [Header("防误触延迟 Input Delay")]
    public float inputDelay = 0.5f;

    [Header("激活按键 Activation Keys")]
    [Tooltip("触发场景切换的按键，默认只有 E。可改为 ↑/W 等")]
    public KeyCode[] activationKeys = new KeyCode[] { KeyCode.E };

    [Header("黑屏过渡")]
    [Tooltip("勾选后切换场景时播放黑屏淡入淡出动画")]
    public bool useBlackTransition = false;
    [Tooltip("淡入/淡出时长（秒）")]
    public float fadeDuration = 1f;
    [Tooltip("全黑停留时长（秒）")]
    public float holdBlackDuration = 0.3f;
    [Tooltip("黑屏期间播放的音效（可选）")]
    public AudioClip transitionSFX;

    [Header("手动激活模式 Manual Activation")]
    [Tooltip("勾选后不会在 TriggerEnter 时自动响应按键，需要外部调用 EnablePortal() 激活")]
    public bool manualActivation = false;

    private bool playerInRange = false;
    private bool isTeleporting = false;
    private bool playingStory = false;
    private bool isPortalActive = true;
    private int currentStoryIndex = -1;
    private float timer = 0f;

    private void Start()
    {
        if (interactUI != null)
            interactUI.SetActive(false);

        if (manualActivation)
            isPortalActive = false;

        HideAllStoryUIs();
    }

    private void Update()
    {
        if (playingStory)
        {
            timer += Time.deltaTime;

            if (timer >= inputDelay && Input.anyKeyDown)
            {
                ShowNextStoryUI();
            }

            return;
        }

        if (!isPortalActive) return;

        if (playerInRange && !isTeleporting && IsActivationKeyDown())
        {
            StartPortal();
        }
    }

    private bool IsActivationKeyDown()
    {
        if (activationKeys == null || activationKeys.Length == 0)
            return Input.GetKeyDown(KeyCode.E);

        foreach (var key in activationKeys)
        {
            if (Input.GetKeyDown(key))
                return true;
        }
        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true;

        if (!isTeleporting && !manualActivation && interactUI != null)
            interactUI.SetActive(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false;

        if (!isTeleporting && interactUI != null)
            interactUI.SetActive(false);
    }

    private void StartPortal()
    {
        isTeleporting = true;

        if (interactUI != null)
            interactUI.SetActive(false);

        if (HasStoryUI())
        {
            playingStory = true;
            currentStoryIndex = -1;
            ShowNextStoryUI();
        }
        else
        {
            StartCoroutine(DoTransition());
        }
    }

    private IEnumerator DoTransition()
    {
        if (useBlackTransition)
        {
            Canvas fadeCanvas = CreateFadeCanvas();
            Image fadeImage = fadeCanvas.GetComponentInChildren<Image>();

            if (fadeImage == null)
            {
                SceneManager.LoadScene(targetSceneName);
                yield break;
            }

            yield return StartCoroutine(FadeImage(fadeImage, 0f, 1f, fadeDuration));

            if (transitionSFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(transitionSFX);

            yield return new WaitForSeconds(holdBlackDuration);
        }

        SceneManager.LoadScene(targetSceneName);
    }

    private void ShowNextStoryUI()
    {
        int nextIndex = currentStoryIndex + 1;

        while (nextIndex < storyUIs.Length && storyUIs[nextIndex] == null)
        {
            nextIndex++;
        }

        if (nextIndex >= storyUIs.Length)
        {
            StartCoroutine(DoTransition());
            return;
        }

        if (currentStoryIndex >= 0 && currentStoryIndex < storyUIs.Length)
        {
            if (storyUIs[currentStoryIndex] != null)
                storyUIs[currentStoryIndex].SetActive(false);
        }

        currentStoryIndex = nextIndex;
        timer = 0f;

        storyUIs[currentStoryIndex].SetActive(true);
    }

    private bool HasStoryUI()
    {
        if (storyUIs == null || storyUIs.Length == 0) return false;

        for (int i = 0; i < storyUIs.Length; i++)
        {
            if (storyUIs[i] != null)
                return true;
        }

        return false;
    }

    private void HideAllStoryUIs()
    {
        if (storyUIs == null) return;

        for (int i = 0; i < storyUIs.Length; i++)
        {
            if (storyUIs[i] != null)
                storyUIs[i].SetActive(false);
        }
    }

    public void EnablePortal()
    {
        isPortalActive = true;
    }

    public void DisablePortal()
    {
        isPortalActive = false;
    }

    #region 黑屏过渡内部方法

    /// <summary>在当前场景动态创建一个全屏黑 Canvas（淡出前用，随场景一起销毁）</summary>
    private Canvas CreateFadeCanvas()
    {
        GameObject canvasGO = new GameObject("FadeOutCanvas");
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
        image.color = Color.clear;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return canvas;
    }

    private IEnumerator FadeImage(Image image, float from, float to, float duration)
    {
        float elapsed = 0f;

        SetAlpha(image, from);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(image, Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }

        SetAlpha(image, to);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    #endregion
}
