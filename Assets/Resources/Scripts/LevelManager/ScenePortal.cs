using UnityEngine;

public class ScenePortal : MonoBehaviour
{
    [Header("要进入的场景")]
    public string targetSceneName;

    [Header("玩家标签")]
    public string playerTag = "Player";

    [Header("交互提示 UI")]
    public GameObject interactUI;

    [Header("剧情 UI 列表")]
    public GameObject[] storyUIs;

    [Header("防误触延迟")]
    public float inputDelay = 0.5f;

    [Header("激活按键")]
    [Tooltip("触发场景切换的按键，默认只有 E。可改为 ↑/W 等")]
    public KeyCode[] activationKeys = { KeyCode.E };

    [Header("黑屏过渡")]
    [Tooltip("勾选后切换场景时播放黑屏淡入淡出动画")]
    public bool useBlackTransition;
    [Tooltip("淡入/淡出时长（秒）")]
    public float fadeDuration = 1f;
    [Tooltip("全黑停留时长（秒）")]
    public float holdBlackDuration = 0.3f;
    [Tooltip("黑屏期间播放的音效（可选）")]
    public AudioClip transitionSFX;

    [Header("过场剧情")]
    [Tooltip("播放 storyUIs 期间切换为这段 BGM（可选），场景切换后由新场景 BGMPlayer 接管")]
    public AudioClip cutsceneBGM;

    [Header("手动激活模式")]
    [Tooltip("勾选后不会在 TriggerEnter 时自动响应按键，需要外部调用 EnablePortal() 激活")]
    public bool manualActivation;

    private bool playerInRange;
    private bool isTeleporting;
    private bool playingStory;
    private bool isPortalActive = true;
    private float inputTimer;
    private StorySequence storySequence;

    private void Start()
    {
        storySequence = new StorySequence(storyUIs);
        storySequence.ResetAndHide();
        SetInteractVisible(false);

        if (manualActivation)
            isPortalActive = false;
    }

    private void Update()
    {
        if (playingStory)
        {
            inputTimer += Time.unscaledDeltaTime;
            if (inputTimer >= inputDelay && Input.anyKeyDown)
                ShowNextStoryPage();
            return;
        }

        if (isPortalActive && playerInRange && !isTeleporting && IsActivationKeyDown())
            StartPortal();
    }

    private bool IsActivationKeyDown()
    {
        if (activationKeys == null || activationKeys.Length == 0)
            return false;

        for (int i = 0; i < activationKeys.Length; i++)
        {
            if (Input.GetKeyDown(activationKeys[i]))
                return true;
        }

        return false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = true;
        if (isTeleporting || manualActivation || !isPortalActive)
            return;

        if (activationKeys == null || activationKeys.Length == 0)
            StartPortal();
        else
            SetInteractVisible(true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;
        if (!isTeleporting)
            SetInteractVisible(false);
    }

    private void StartPortal()
    {
        if (isTeleporting || SceneTransitionService.IsTransitioning)
            return;

        isTeleporting = true;
        SetInteractVisible(false);

        if (storySequence != null && storySequence.HasPages)
        {
            if (cutsceneBGM != null && AudioManager.Instance != null)
                AudioManager.Instance.PlayBGM(cutsceneBGM);

            playingStory = true;
            ShowNextStoryPage();
            return;
        }

        BeginSceneTransition();
    }

    private void ShowNextStoryPage()
    {
        inputTimer = 0f;
        if (storySequence.ShowNext())
            return;

        playingStory = false;
        BeginSceneTransition();
    }

    private void BeginSceneTransition()
    {
        float duration = useBlackTransition ? fadeDuration : 0f;
        float hold = useBlackTransition ? holdBlackDuration : 0f;
        SceneTransitionService.LoadScene(targetSceneName, duration, hold, duration, transitionSFX);
    }

    private void SetInteractVisible(bool visible)
    {
        if (interactUI != null)
            interactUI.SetActive(visible);
    }

    public void EnablePortal()
    {
        isPortalActive = true;
        if (playerInRange && activationKeys != null && activationKeys.Length > 0)
            SetInteractVisible(true);
    }

    public void DisablePortal()
    {
        isPortalActive = false;
        SetInteractVisible(false);
    }

    public void TriggerNow()
    {
        if (!isTeleporting)
            StartPortal();
    }
}
