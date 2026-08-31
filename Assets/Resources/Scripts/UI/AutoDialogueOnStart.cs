using System.Collections;
using NodeCanvas.DialogueTrees;
using UnityEngine;
using UnityEngine.Events;

/// <summary>Starts a scene-authored NodeCanvas dialogue and optionally transitions afterward.</summary>
public class AutoDialogueOnStart : MonoBehaviour
{
    [Header("对话 Dialogue")]
    [SerializeField] private DialogueTreeController dialogue;

    [Header("玩家 Player")]
    [SerializeField] private Player player;

    [Header("延迟 Delay")]
    public float delayBeforeStart = 0.5f;

    [Header("对话结束回调")]
    public UnityEvent OnDialogueFinishedEvent;

    [Header("结局跳转（可选）")]
    public string loadSceneAfterDialogue;
    public bool useBlackFade = true;
    public float fadeDuration = 1f;

    private bool dialogueRunning;
    private bool completed;
    private Coroutine pendingDialogue;

    private void Start()
    {
        if (dialogue == null)
        {
            Debug.LogWarning("AutoDialogueOnStart：DialogueTreeController 未配置。", this);
            return;
        }

        if (player == null)
            player = FindAnyObjectByType<Player>();

        pendingDialogue = StartCoroutine(StartDialogueRoutine());
    }

    private IEnumerator StartDialogueRoutine()
    {
        if (delayBeforeStart > 0f)
            yield return new WaitForSeconds(delayBeforeStart);

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
        completed = true;
        player?.ReleaseInputLock(this);
        OnDialogueFinishedEvent?.Invoke();

        if (string.IsNullOrWhiteSpace(loadSceneAfterDialogue))
            return;

        float duration = useBlackFade ? fadeDuration : 0f;
        SceneTransitionService.LoadScene(loadSceneAfterDialogue, duration, 0f, duration);
    }

    private void OnDisable()
    {
        if (pendingDialogue != null)
        {
            StopCoroutine(pendingDialogue);
            pendingDialogue = null;
        }

        bool shouldStopDialogue = !completed && dialogueRunning;
        dialogueRunning = false;
        if (shouldStopDialogue && dialogue != null)
            dialogue.StopDialogue();

        player?.ReleaseInputLock(this);
    }
}
