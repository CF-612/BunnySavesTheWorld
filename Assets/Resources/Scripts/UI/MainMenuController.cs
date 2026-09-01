using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("场景设置")]
    public string gameSceneName = "GameScene";

    [Header("按钮")]
    [Tooltip("开始新游戏按钮：清空当前进度并播放开场剧情。")]
    public Button startButton;
    [Tooltip("继续游玩按钮：加载当前存档场景；没有存档时会自动禁用。")]
    public Button continueButton;
    public Button quitButton;

    [Header("开始游戏剧情 UI 列表")]
    public GameObject[] startStoryUIs;

    [Header("开场背景音乐")]
    public AudioClip openingBGM;

    [Header("剧情翻页防误触延迟")]
    public float inputDelay = 0.5f;

    [Header("场景过渡")]
    public float transitionDuration = 1f;

    [Header("悬停换图（可选）")]
    public Sprite hoverSprite;

    [Header("点击效果")]
    public float pressScale = 0.9f;
    public float effectTime = 0.08f;

    private const string HoverSfxPath = "Audio/SFX/UI/ChooseBotton";
    private const string ClickSfxPath = "Audio/SFX/UI/pop3";

    private Image startButtonImage;
    private Image quitButtonImage;
    private Sprite startNormalSprite;
    private Sprite quitNormalSprite;
    private bool isStarting;
    private bool isQuitting;
    private bool playingStartStory;
    private float inputTimer;
    private StorySequence storySequence;

    private void Start()
    {
        storySequence = new StorySequence(startStoryUIs);
        storySequence.ResetAndHide();
        SetupButton(startButton, HandleNewGameClicked, out startButtonImage, out startNormalSprite);
        SetupButton(continueButton, HandleContinueClicked, out Image continueButtonImage, out Sprite continueNormalSprite);
        SetupButton(quitButton, HandleQuitClicked, out quitButtonImage, out quitNormalSprite);

        AddHoverEvent(startButton, startButtonImage, startNormalSprite);
        AddHoverEvent(continueButton, continueButtonImage, continueNormalSprite);
        AddHoverEvent(quitButton, quitButtonImage, quitNormalSprite);
        SetContinueButtonAvailability();
    }

    private void Update()
    {
        if (!playingStartStory)
            return;

        inputTimer += Time.unscaledDeltaTime;
        if (inputTimer >= inputDelay && Input.anyKeyDown)
            ShowNextStartStoryPage();
    }

    private void OnDestroy()
    {
        if (startButton != null)
            startButton.onClick.RemoveListener(HandleNewGameClicked);
        if (continueButton != null)
            continueButton.onClick.RemoveListener(HandleContinueClicked);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(HandleQuitClicked);
    }

    /// <summary>兼容旧 UnityEvent 的开始入口；现在等同于开始新游戏。</summary>
    public void StartGame()
    {
        StartNewGame();
    }

    /// <summary>开始新游戏：清空当前窄范围进度，播放开场剧情并进入首个场景。</summary>
    public void StartNewGame()
    {
        if (isStarting)
            return;

        isStarting = true;
        SetButtonsInteractable(false);
        StartFreshGame();
    }

    /// <summary>继续游玩：请求一次检查点续玩定位，并加载存档中的最近场景。</summary>
    public void ContinueGame()
    {
        if (isStarting || !GameProgressService.HasStarted)
            return;

        isStarting = true;
        SetButtonsInteractable(false);
        GameProgressService.RequestContinue();
        LoadGameScene(GameProgressService.ContinueScene);
    }

    public void QuitGame()
    {
        if (isQuitting || playingStartStory)
            return;

        isQuitting = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void StartFreshGame()
    {
        GameProgressService.BeginNewGame(gameSceneName);

        if (openingBGM != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(openingBGM);

        if (storySequence != null && storySequence.HasPages)
        {
            playingStartStory = true;
            ShowNextStartStoryPage();
        }
        else
        {
            LoadGameScene(gameSceneName);
        }
    }

    private void ShowNextStartStoryPage()
    {
        inputTimer = 0f;
        if (storySequence.ShowNext())
            return;

        playingStartStory = false;
        LoadGameScene(gameSceneName);
    }

    private void LoadGameScene(string sceneName)
    {
        SceneTransitionService.LoadScene(sceneName, transitionDuration, 0.2f, transitionDuration);
    }

    private void HandleNewGameClicked()
    {
        StartCoroutine(PressButtonEffect(startButton, StartNewGame));
    }

    private void HandleContinueClicked()
    {
        StartCoroutine(PressButtonEffect(continueButton, ContinueGame));
    }

    private void HandleQuitClicked()
    {
        StartCoroutine(PressButtonEffect(quitButton, QuitGame));
    }

    private IEnumerator PressButtonEffect(Button button, System.Action action)
    {
        if (button == null)
            yield break;

        AudioManager.Instance?.PlaySFX(ClickSfxPath);
        Transform buttonTransform = button.transform;
        Vector3 originalScale = buttonTransform.localScale;
        buttonTransform.localScale = originalScale * pressScale;

        if (effectTime > 0f)
            yield return new WaitForSecondsRealtime(effectTime);

        buttonTransform.localScale = originalScale;

        if (effectTime > 0f)
            yield return new WaitForSecondsRealtime(effectTime);

        action?.Invoke();
    }

    private static void SetupButton(
        Button button,
        UnityEngine.Events.UnityAction action,
        out Image image,
        out Sprite normalSprite)
    {
        image = button != null ? button.GetComponent<Image>() : null;
        normalSprite = image != null ? image.sprite : null;
        button?.onClick.AddListener(action);
    }

    private void AddHoverEvent(Button button, Image image, Sprite normalSprite)
    {
        if (button == null)
            return;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();
        trigger.triggers ??= new System.Collections.Generic.List<EventTrigger.Entry>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            ChangeImageSprite(image, hoverSprite);
            AudioManager.Instance?.PlaySFX(HoverSfxPath);
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => ChangeImageSprite(image, normalSprite));
        trigger.triggers.Add(exit);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (startButton != null)
            startButton.interactable = interactable;
        if (continueButton != null)
            continueButton.interactable = interactable && GameProgressService.HasStarted;
        if (quitButton != null)
            quitButton.interactable = interactable;
    }

    private void SetContinueButtonAvailability()
    {
        if (continueButton != null)
            continueButton.interactable = GameProgressService.HasStarted;
    }

    private static void ChangeImageSprite(Image image, Sprite sprite)
    {
        if (image != null && sprite != null)
            image.sprite = sprite;
    }
}
