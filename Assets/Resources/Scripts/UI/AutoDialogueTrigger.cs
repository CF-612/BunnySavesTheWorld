using NodeCanvas.DialogueTrees;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 自动对话触发器：玩家进入 Trigger 后自动锁定输入并播放对话。
/// 可挂载在任意带 Collider2D (IsTrigger) 的 GameObject 上，与 ScenePortal 解耦。
///
/// 典型用法：
/// - 单独使用：场景中任意触发器，玩家走到此处自动触发 NPC 对话
/// - 与 ScenePortal 协作：挂在同一个 Trigger 上，对话结束后通过 UnityEvent 激活 ScenePortal
/// </summary>
public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("对话 Dialogue")]
    [SerializeField] private DialogueTreeController dialogue;

    [Header("玩家 Player")]
    [SerializeField] private Player player;
    [SerializeField] private string playerTag = "Player";

    [Header("触发设置 Trigger Settings")]
    [Tooltip("是否只触发一次，触发后组件自动禁用")]
    public bool triggerOnce = true;
    [Tooltip("进入触发器后延迟多久开始对话（秒）")]
    public float delayBeforeDialogue = 0.3f;

    [Header("对话结束回调 On Dialogue Completed")]
    [Tooltip("对话播放完毕后触发，可在 Inspector 中拖入任意方法（如 ScenePortal.EnablePortal）")]
    public UnityEvent OnDialogueCompleted;

    private bool hasTriggered;

    private void Start()
    {
        hasTriggered = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (hasTriggered && triggerOnce) return;
        if (dialogue == null)
        {
            Debug.LogWarning("AutoDialogueTrigger：DialogueTreeController 未配置，跳过。", this);
            return;
        }

        hasTriggered = true;
        StartCoroutine(TriggerDialogueCoroutine());
    }

    private System.Collections.IEnumerator TriggerDialogueCoroutine()
    {
        yield return new WaitForSeconds(delayBeforeDialogue);

        // 锁定玩家输入
        if (player != null)
            player.input.Disable();

        // 订阅对话结束事件
        DialogueTree.OnDialogueFinished += OnDialogueFinished;

        dialogue.StartDialogue();
    }

    private void OnDialogueFinished(DialogueTree dlg)
    {
        // 取消订阅，避免重复调用
        DialogueTree.OnDialogueFinished -= OnDialogueFinished;

        // 解锁玩家输入
        if (player != null)
            player.input.Enable();

        // 触发 Inspector 中配置的回调
        OnDialogueCompleted?.Invoke();

        // 仅触发一次时禁用自身（不销毁，保留 Collider 给 ScenePortal 等协作脚本使用）
        if (triggerOnce)
            enabled = false;
    }

    private void OnDestroy()
    {
        // 防止脚本被销毁时事件仍残留
        DialogueTree.OnDialogueFinished -= OnDialogueFinished;
    }
}
