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

    [Header("剧情翻页防误触延迟 Input Delay")]
    public float inputDelay = 0.5f; // 每页出现后多久才允许按任意键

    [Header("悬停换图 Hover Sprite（可选 Optional）")]
    public Sprite hoverSprite; 

    [Header("点击效果 Click Effect")]
    public float pressScale = 0.9f;  // 点击时缩小比例
    public float effectTime = 0.08f; // 点击动画时间

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
        HideAllStartStoryUIs(); // 一开始先隐藏所有剧情 UI，避免它们直接显示在主菜单上

        if (startButton != null)
        {
            startButtonImage = startButton.GetComponent<Image>();
            if (startButtonImage != null) startNormalSprite = startButtonImage.sprite;

            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(() => StartCoroutine(PressButtonEffect(startButton, StartGame))); // 点击开始按钮时，先播放点击缩放效果，再执行 StartGame

            AddHoverEvent(
                startButton.gameObject,
                () => ChangeImageSprite(startButtonImage, hoverSprite), // 鼠标移入时，换开始按钮图片
                () => ChangeImageSprite(startButtonImage, startNormalSprite)  // 鼠标移出时，把开始按钮图片还原成原图
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
                () => ChangeImageSprite(quitButtonImage, hoverSprite),
                () => ChangeImageSprite(quitButtonImage, quitNormalSprite)
            );
        }
    }

    void Update()
    {
        if (!playingStartStory) return;  // 如果没有播放开始剧情，就不检测按键翻页

        timer += Time.deltaTime; // 让计时器随着时间增加

        if (timer >= inputDelay && Input.anyKeyDown) // 如果超过防误触时间，并且玩家按下任意键
        {
            ShowNextStartStoryUI(); // 显示下一页剧情 UI
        }
    }

    public void StartGame()
    {
        if (isStarting) return;
        isStarting = true;

        if (startButton != null) startButton.interactable = false; //防止重复点击
        if (quitButton != null) quitButton.interactable = false;

        if (HasStartStoryUI()) // 如果有可播放的开始剧情 UI
        {
            playingStartStory = true; // 标记正在播放开始剧情
            currentStoryIndex = -1; 
            ShowNextStartStoryUI();
        }
        else // 如果没有拖任何剧情 UI
        {
            SceneManager.LoadScene(gameSceneName); // 直接进入游戏场景
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

    IEnumerator PressButtonEffect(Button button, System.Action action) // 点击按钮时的缩放动画，动画结束后执行 action（可以是 StartGame 或 QuitGame）
    {
        if (button == null) yield break;

        Transform buttonTransform = button.transform;
        Vector3 originalScale = buttonTransform.localScale;

        buttonTransform.localScale = originalScale * pressScale; // 按下缩小
        yield return new WaitForSeconds(effectTime);

        buttonTransform.localScale = originalScale; // 恢复
        yield return new WaitForSeconds(effectTime);

        action?.Invoke(); // 执行功能
    }

    void ShowNextStartStoryUI()  // 显示下一页开始剧情 UI
    {
        int nextIndex = currentStoryIndex + 1; // 下一页索引等于当前页加 1

        while (nextIndex < startStoryUIs.Length && startStoryUIs[nextIndex] == null)
        {
            nextIndex++;
        }

        // 没有下一页了，直接进入游戏场景
        // 这里不要关闭最后一页 UI，避免切场景前露出主菜单
        if (nextIndex >= startStoryUIs.Length)
        {
            SceneManager.LoadScene(gameSceneName);
            return;
        }

        // 有下一页时，才关闭当前页
        if (currentStoryIndex >= 0 && currentStoryIndex < startStoryUIs.Length)
        {
            if (startStoryUIs[currentStoryIndex] != null)  // 如果当前页 UI 不为空
                startStoryUIs[currentStoryIndex].SetActive(false); // 关闭当前页 UI
        }

        currentStoryIndex = nextIndex;
        timer = 0f;

        startStoryUIs[currentStoryIndex].SetActive(true);
    }

    bool HasStartStoryUI() // 检查是否有可播放的开始剧情 UI，返回 true 就会进入剧情翻页流程，返回 false 就会直接进入游戏场景
    {
        if (startStoryUIs == null || startStoryUIs.Length == 0) return false; // 如果数组为空或长度为 0，说明没有剧情 UI

        for (int i = 0; i < startStoryUIs.Length; i++) // 遍历数组，检查是否有非空的 UI
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