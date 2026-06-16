using System.Collections;
using NodeCanvas.DialogueTrees;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 场景入场自动对话：场景加载后自动锁定玩家并播放对话，对话结束后解锁。
/// 挂载在场景中的空 GameObject 上即可。
///
/// 典型用法：
/// - 新手教程：对话结束后解锁玩家自由活动
/// - 最终结局：对话结束后黑屏淡出，返回主菜单（设置 loadSceneAfterDialogue）
/// </summary>
public class AutoDialogueOnStart : MonoBehaviour
{
    [Header("对话 Dialogue")]
    [SerializeField] private DialogueTreeController dialogue;

    [Header("玩家 Player")]
    [SerializeField] private Player player;

    [Header("延迟 Delay")]
    [Tooltip("场景加载后延迟多久开始对话（秒），给场景一点初始化时间")]
    public float delayBeforeStart = 0.5f;

    [Header("对话结束回调")]
    [Tooltip("对话播放完毕后触发，可拖入 NPCExpressionController.SetSee 等方法")]
    public UnityEvent OnDialogueFinishedEvent;

    [Header("结局跳转（可选）")]
    [Tooltip("对话结束后加载的场景名。留空 = 解锁玩家自由活动（教程模式）；填写 = 黑屏后跳转（结局模式）")]
    public string loadSceneAfterDialogue;
    [Tooltip("加载场景前是否黑屏淡出")]
    public bool useBlackFade = true;
    [Tooltip("黑屏淡出时长（秒）")]
    public float fadeDuration = 1f;

    private bool dialogueFinished;

    private void Start()
    {
        if (dialogue == null)
        {
            Debug.LogWarning("AutoDialogueOnStart：DialogueTreeController 未配置，跳过。", this);
            return;
        }

        StartCoroutine(StartDialogueCoroutine());
    }

    private IEnumerator StartDialogueCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeStart);

        // 锁定玩家输入
        if (player != null)
            player.input.Disable();

        // 订阅对话结束事件
        DialogueTree.OnDialogueFinished += OnDialogueFinished;

        dialogue.StartDialogue();
    }

    private void OnDialogueFinished(DialogueTree dlg)
    {
        DialogueTree.OnDialogueFinished -= OnDialogueFinished;

        dialogueFinished = true;

        // 先触发外部回调（如表情切换）
        OnDialogueFinishedEvent?.Invoke();

        if (!string.IsNullOrEmpty(loadSceneAfterDialogue))
        {
            // 结局模式：黑屏淡出 → 加载目标场景
            StartCoroutine(EndingCoroutine());
        }
        else
        {
            // 教程模式：解锁玩家输入
            if (player != null)
                player.input.Enable();
        }
    }

    private IEnumerator EndingCoroutine()
    {
        if (useBlackFade)
        {
            // 创建黑屏 Canvas
            GameObject canvasGO = new GameObject("EndingFadeCanvas");
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

            // 淡入黑屏
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
                SetAlpha(image, alpha);
                yield return null;
            }
            SetAlpha(image, 1f);
        }

        SceneManager.LoadScene(loadSceneAfterDialogue);
    }

    private static void SetAlpha(Image image, float alpha)
    {
        Color c = image.color;
        c.a = alpha;
        image.color = c;
    }

    private void OnDestroy()
    {
        DialogueTree.OnDialogueFinished -= OnDialogueFinished;

        if (!dialogueFinished && player != null)
            player.input.Enable();
    }
}
