using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenePortal : MonoBehaviour
{
    [Header("要进入的场景 Scene")]
    public string targetSceneName;

    [Header("玩家标签 Player Tag")]
    public string playerTag = "Player";

    [Header("交互提示 UI Interact UI")]
    public GameObject interactUI; // 比如“按 E 进入”

    [Header("剧情 UI 列表 Story UI List")]
    public GameObject[] storyUIs; // 不拖 = 按 E 后直接切场景；拖多个 = 按顺序播放剧情

    [Header("防误触延迟 Input Delay")]
    public float inputDelay = 0.5f;

    private bool playerInRange = false;
    private bool isTeleporting = false;
    private bool playingStory = false;
    private int currentStoryIndex = -1;
    private float timer = 0f;

    private void Start()
    {
        if (interactUI != null)
            interactUI.SetActive(false);

        HideAllStoryUIs(); // 一开始隐藏所有剧情 UI，避免它们提前显示
    }

    private void Update()
    {
        // 正在播放剧情UI任意键翻页
        if (playingStory)
        {
            timer += Time.deltaTime;

            if (timer >= inputDelay && Input.anyKeyDown)
            {
                ShowNextStoryUI();
            }

            return;
        }

        // 没在播剧情时，玩家在范围内按 E 才触发转场
        if (playerInRange && !isTeleporting && Input.GetKeyDown(KeyCode.E))
        {
            StartPortal();
        }
    }

    private void OnTriggerEnter2D(Collider2D other) // 当有 2D 碰撞体进入这个触发器时自动执行
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = true; // 标记玩家已经进入传送门范围

        if (!isTeleporting && interactUI != null)
            interactUI.SetActive(true); // 显示交互提示 UI
    }

    private void OnTriggerExit2D(Collider2D other) // 当有 2D 碰撞体离开这个触发器时自动执行
    {
        if (!other.CompareTag(playerTag)) return;

        playerInRange = false; // 标记玩家已经离开传送门范围

        if (!isTeleporting && interactUI != null)
            interactUI.SetActive(false); // 隐藏交互提示 UI
    }

    private void StartPortal() // 开始传送流程
    {
        isTeleporting = true; // 标记已经开始传送，防止重复触发

        if (interactUI != null)
            interactUI.SetActive(false);

        if (HasStoryUI()) // 如果有剧情 UI
        {
            playingStory = true;
            currentStoryIndex = -1;
            ShowNextStoryUI();
        }
        else // 如果没有剧情 UI
        {
            SceneManager.LoadScene(targetSceneName); // 直接切换到目标场景
        }
    }

    private void ShowNextStoryUI() // 显示下一页剧情 UI 的方法
    {
        int nextIndex = currentStoryIndex + 1; // 下一页索引等于当前页索引加 1

        while (nextIndex < storyUIs.Length && storyUIs[nextIndex] == null)
        {
            nextIndex++;
        }

        // 没有下一页了，直接切场景，不关闭当前剧情 UI，避免露出原场景
        if (nextIndex >= storyUIs.Length)
        {
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        // 有下一页时，才关闭当前页
        if (currentStoryIndex >= 0 && currentStoryIndex < storyUIs.Length)
        {
            if (storyUIs[currentStoryIndex] != null)
                storyUIs[currentStoryIndex].SetActive(false);
        }

        currentStoryIndex = nextIndex;
        timer = 0f;

        storyUIs[currentStoryIndex].SetActive(true);
    }

    private bool HasStoryUI() // 判断是否有剧情 UI 的方法
    {
        if (storyUIs == null || storyUIs.Length == 0) return false; // 如果数组为空或长度为 0，说明没有剧情 UI

        for (int i = 0; i < storyUIs.Length; i++) // 遍历剧情 UI 数组
        {
            if (storyUIs[i] != null) // 如果找到一个不是空的剧情 UI
                return true; // 说明有剧情 UI
        }

        return false; // 如果全部都是空的，说明没有剧情 UI
    }

    private void HideAllStoryUIs() // 一开始隐藏所有剧情 UI 的方法
    {
        if (storyUIs == null) return;

        for (int i = 0; i < storyUIs.Length; i++)
        {
            if (storyUIs[i] != null)
                storyUIs[i].SetActive(false);
        }
    }
}