using System.Collections;
using NodeCanvas.DialogueTrees;
using UnityEngine;
using UnityEngine.Events;

/// <summary>玩家进入触发器时，启动该场景对象配置的 NodeCanvas 对话。</summary>
public class AutoDialogueTrigger : MonoBehaviour
{
    [Header("对话")]
    [SerializeField] private DialogueTreeController dialogue;

    [Header("玩家")]
    [SerializeField] private Player player;
    [SerializeField] private string playerTag = "Player";

    [Header("触发设置")]
    public bool triggerOnce = true;
    public float delayBeforeDialogue = 0.3f;

    [Header("对话结束回调")]
    public UnityEvent OnDialogueCompleted;

    private bool hasTriggered;
    private bool dialogueRunning;
    private Coroutine pendingDialogue;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag) || dialogueRunning || (hasTriggered && triggerOnce))
            return;

        if (dialogue == null)
        {
            Debug.LogWarning("AutoDialogueTrigger：DialogueTreeController 未配置。", this);
            return;
        }

        if (player == null)
            player = other.GetComponentInParent<Player>();

        hasTriggered = true;
        pendingDialogue = StartCoroutine(StartDialogueRoutine());
    }

    private IEnumerator StartDialogueRoutine()
    {
        if (delayBeforeDialogue > 0f)
            yield return new WaitForSeconds(delayBeforeDialogue);

        pendingDialogue = null;
        dialogueRunning = true;
        player?.AcquireInputLock(this);
        dialogue.StartDialogue(_ => CompleteDialogue());
    }

    private void CompleteDialogue()
    {
        if (!dialogueRunning)
            return;

        dialogueRunning = false;
        player?.ReleaseInputLock(this);
        OnDialogueCompleted?.Invoke();

        if (triggerOnce)
            enabled = false;
    }

    private void OnDisable()
    {
        if (pendingDialogue != null)
        {
            StopCoroutine(pendingDialogue);
            pendingDialogue = null;
        }

        bool shouldStopDialogue = dialogueRunning;
        dialogueRunning = false;
        if (shouldStopDialogue && dialogue != null)
            dialogue.StopDialogue();

        player?.ReleaseInputLock(this);
    }
}
