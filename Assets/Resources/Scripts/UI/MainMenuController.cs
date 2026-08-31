using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("场景设置")]
    public string gameSceneName = "GameScene";
    [Tooltip("存在进度时，开始按钮继续上次游戏；可通过 StartNewGame 供单独的新游戏按钮调用。")]
    public bool resumeSavedGame = true;

    [Header("按钮")]
    public Button startButton;
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
        SetupButton(startButton, HandleStartClicked, out startButtonImage, out startNormalSprite);
        SetupButton(quitButton, HandleQuitClicked, out quitButtonImage, out quitNormalSprite);

        AddHoverEvent(startButton, startButtonImage, startNormalSprite);
        AddHoverEvent(quitButton, quitButtonImage, quitNormalSprite);
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
            startButton.onClick.RemoveListener(HandleStartClicked);
        if (quitButton != null)
            quitButton.onClick.RemoveListener(HandleQuitClicked);
    }

    /// <summary>配置为优先续玩且存在进度时继续游戏，否则播放已配置的开场流程。</summary>
    public void StartGame()
    {
        if (isStarting)
            return;

        isStarting = true;
        SetButtonsInteractable(false);

        if (resumeSavedGame && GameProgressService.HasStarted)
        {
            GameProgressService.RequestContinue();
            LoadGameScene(GameProgressService.ContinueScene);
            return;
        }

        StartFreshGame();
    }

    /// <summary>供今后单独的“新游戏”按钮调用的明确入口。</summary>
    public void StartNewGame()
    {
        if (isStarting)
            return;

        isStarting = true;
        SetButtonsInteractable(false);
        StartFreshGame();
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

    private void HandleStartClicked()
    {
        StartCoroutine(PressButtonEffect(startButton, StartGame));
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
        if (quitButton != null)
            quitButton.interactable = interactable;
    }

    private static void ChangeImageSprite(Image image, Sprite sprite)
    {
        if (image != null && sprite != null)
            image.sprite = sprite;
    }
}
