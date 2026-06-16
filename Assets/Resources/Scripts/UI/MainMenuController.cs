using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class MainMenuController : MonoBehaviour
{
    [Header("场景设置 Scene Setting")]
    public string gameSceneName = "GameScene"; // 要进入的新场景名字，必须和 Build Settings 里的名字一致

    [Header("按钮 Button")]
    public Button startButton; // 开始游戏按钮
    public Button quitButton;  // 退出游戏按钮

    [Header("开始游戏剧情 UI 列表 Start Story UI List")]
    public GameObject[] startStoryUIs; // 不拖 = 直接开始游戏；拖多个 = 按顺序一页页播放

    [Header("开场 BGM Opening BGM")]
    [Tooltip("开场剧情播放时同步播放的 BGM（通常拖入场景1的 BGM）。留空不播放。")]
    public AudioClip openingBGM;

    [Header("剧情翻页防误触延迟 Input Delay")]
    public float inputDelay = 0.5f; // 每页出现后多久才允许按任意键

    [Header("悬停换图 Hover Sprite（可选 Optional）")]
    public Sprite hoverSprite; 

    [Header("点击效果 Click Effect")]
    public float pressScale = 0.9f;  // 点击时缩小比例
    public float effectTime = 0.08f; // 点击动画时间

    // 按钮音效 Resources 路径常量
    private const string HOVER_SFX_PATH = "Audio/SFX/UI/ChooseBotton";
    private const string CLICK_SFX_PATH = "Audio/SFX/UI/pop3";

    private Image startButtonImage; // 开始按钮自己的 Image
    private Image quitButtonImage;  
    private Sprite startNormalSprite; // 开始按钮原图，用于鼠标移开后恢复
    private Sprite quitNormalSprite;  

    private bool isStarting = false;  // 记录是否已经点击开始，防止重复点击
    private bool isQuitting = false;  // 记录是否已经点击退出，防止重复点击
    private bool playingStartStory = false;  // 记录是否正在播放开始前剧情 UI

    private int currentStoryIndex = -1;
    private float timer = 0f;

    void Start()
    {
        HideAllStartStoryUIs();

        if (startButton != null)
        {
            startButtonImage = startButton.GetComponent<Image>();
            if (startButtonImage != null) startNormalSprite = startButtonImage.sprite;

            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => StartCoroutine(PressButtonEffect(startButton, StartGame))); // 点击开始按钮时，先播放点击缩放效果，再执行 StartGame

            AddHoverEvent(
                startButton.gameObject,
                () => {
                    ChangeImageSprite(startButtonImage, hoverSprite);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(HOVER_SFX_PATH);
                },
                () => ChangeImageSprite(startButtonImage, startNormalSprite)
            );
        }

        if (quitButton != null)
        {
            quitButtonImage = quitButton.GetComponent<Image>();
            if (quitButtonImage != null) quitNormalSprite = quitButtonImage.sprite;

            quitButton.onClick.RemoveAllListeners();
            quitButton.onClick.AddListener(() => StartCoroutine(PressButtonEffect(quitButton, QuitGame)));

            AddHoverEvent(
                quitButton.gameObject,
                () => {
                    ChangeImageSprite(quitButtonImage, hoverSprite);
                    if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(HOVER_SFX_PATH);
                },
                () => ChangeImageSprite(quitButtonImage, quitNormalSprite)
            );
        }
    }

    void Update()
    {
        if (!playingStartStory) return;

        timer += Time.deltaTime;

        if (timer >= inputDelay && Input.anyKeyDown)
        {
            ShowNextStartStoryUI();
        }
    }

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;

        if (startButton != null) startButton.interactable = false;
        if (quitButton != null) quitButton.interactable = false;

        // 开场剧情开始时同步播放 BGM
        if (openingBGM != null && AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM(openingBGM);

        if (HasStartStoryUI())
        {
            playingStartStory = true;
            currentStoryIndex = -1;
            ShowNextStartStoryUI();
        }
        else
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void QuitGame() //退出游戏
    {
        if (isQuitting || playingStartStory) return;
        isQuitting = true;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    IEnumerator PressButtonEffect(Button button, System.Action action)
    {
        if (button == null) yield break;

        if (AudioManager.Instance != null) AudioManager.Instance.PlaySFX(CLICK_SFX_PATH);

        Transform buttonTransform = button.transform;
        Vector3 originalScale = buttonTransform.localScale;

        buttonTransform.localScale = originalScale * pressScale;
        yield return new WaitForSeconds(effectTime);

        buttonTransform.localScale = originalScale;
        yield return new WaitForSeconds(effectTime);

        action?.Invoke();
    }

    void ShowNextStartStoryUI()
    {
        int nextIndex = currentStoryIndex + 1;

        while (nextIndex < startStoryUIs.Length && startStoryUIs[nextIndex] == null)
        {
            nextIndex++;
        }

        if (nextIndex >= startStoryUIs.Length)
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        if (currentStoryIndex >= 0 && currentStoryIndex < startStoryUIs.Length)
        {
            if (startStoryUIs[currentStoryIndex] != null)
                startStoryUIs[currentStoryIndex].SetActive(false);
        }

        currentStoryIndex = nextIndex;
        timer = 0f;

        startStoryUIs[currentStoryIndex].SetActive(true);
    }

    bool HasStartStoryUI()
    {
        if (startStoryUIs == null || startStoryUIs.Length == 0) return false;

        for (int i = 0; i < startStoryUIs.Length; i++)
        {
            if (startStoryUIs[i] != null)
                return true;
        }

        return false;
    }

    void HideAllStartStoryUIs()
    {
        if (startStoryUIs == null) return;

        for (int i = 0; i < startStoryUIs.Length; i++)
        {
            if (startStoryUIs[i] != null)
                startStoryUIs[i].SetActive(false);
        }
    }

    void ChangeImageSprite(Image targetImage, Sprite targetSprite) //// 替换指定 Image 的图片
    {
        if (targetImage == null || targetSprite == null) return;
        targetImage.sprite = targetSprite;
    }

    void AddHoverEvent(GameObject target, System.Action onEnter, System.Action onExit)  // 给目标物体添加鼠标移入和移出事件
    {
        EventTrigger trigger = target.GetComponent<EventTrigger>();
        if (trigger == null) trigger = target.AddComponent<EventTrigger>();

        trigger.triggers.Clear(); // 清空旧事件，防止重复添加导致多次触发

        EventTrigger.Entry enterEntry = new EventTrigger.Entry();  // 创建一个鼠标移入事件
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => { onEnter?.Invoke(); });
        trigger.triggers.Add(enterEntry);

        EventTrigger.Entry exitEntry = new EventTrigger.Entry(); // 创建一个鼠标移出事件
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => { onExit?.Invoke(); });
        trigger.triggers.Add(exitEntry);
    }
}