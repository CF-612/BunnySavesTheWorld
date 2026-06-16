using NodeCanvas.DialogueTrees;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 场景入场自动对话：场景加载后自动锁定玩家并播放对话，对话结束后解锁。
/// 挂载在场景中的空 GameObject 上即可。
///
/// 典型用法：
/// - 新手教程关卡：玩家进入场景后不能动，由主人 NPC 自动开始教程对话
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

    private System.Collections.IEnumerator StartDialogueCoroutine()
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

        // 解锁玩家输入
        if (player != null)
            player.input.Enable();

        // 触发外部回调（如切换表情）
        OnDialogueFinishedEvent?.Invoke();
    }

    private void OnDestroy()
    {
        // 防止脚本被销毁时事件仍残留
        DialogueTree.OnDialogueFinished -= OnDialogueFinished;

        // 确保玩家输入在异常退出时也能恢复
        if (!dialogueFinished && player != null)
            player.input.Enable();
    }
}
