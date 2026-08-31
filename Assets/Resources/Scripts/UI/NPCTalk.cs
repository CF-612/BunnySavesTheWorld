using NodeCanvas.DialogueTrees;
using NodeCanvas.DialogueTrees.UI.Examples;
using UnityEngine;

/// <summary>连接玩家交互与 NPC 所属 NodeCanvas 对话的场景适配器。</summary>
public class NPCTalk : MonoBehaviour
{
    [Header("要显示或隐藏的提示物体")]
    public GameObject textObject;

    [Header("玩家标签")]
    public string playerTag = "Player";

    [Header("NodeCanvas 对话")]
    [SerializeField] private DialogueTreeController dialogue;

    [Header("玩家朝向设置")]
    public Transform playerVisual;
    public bool playerDefaultFaceRight = true;

    [Header("NPC 朝向设置")]
    public Transform npcVisual;
    public bool npcDefaultFaceRight = true;

    private bool playerInRange;
    private bool ownsDialogue;
    private DialogueUGUI dialogueUi;
    private Transform currentPlayer;
    private Player currentPlayerController;

    private void Awake()
    {
        if (dialogue == null)
            dialogue = GetComponent<DialogueTreeController>();
        if (npcVisual == null)
            npcVisual = transform;

        dialogueUi = FindAnyObjectByType<DialogueUGUI>(FindObjectsInactive.Include);
        SetPromptVisible(false);
    }

    private void Update()
    {
        bool anotherDialogueIsPlaying = dialogueUi != null && dialogueUi.isTalking && !ownsDialogue;
        SetPromptVisible(playerInRange && !ownsDialogue && !anotherDialogueIsPlaying);

        if (playerInRange && !ownsDialogue && !anotherDialogueIsPlaying && Input.GetKeyDown(KeyCode.E))
            StartDialogue();
    }

    private void StartDialogue()
    {
        if (dialogue == null)
        {
            Debug.LogWarning("NPCTalk：DialogueTreeController 未配置。", this);
            return;
        }

        FacePlayerToNpc();
        FaceNpcToPlayer();
        ownsDialogue = true;
        currentPlayerController?.AcquireInputLock(this);
        SetPromptVisible(false);
        dialogue.StartDialogue(_ => CompleteDialogue());
    }

    private void CompleteDialogue()
    {
        ownsDialogue = false;
        currentPlayerController?.ReleaseInputLock(this);
        SetPromptVisible(playerInRange);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = true;
        currentPlayer = other.transform;
        currentPlayerController = other.GetComponentInParent<Player>();
        if (playerVisual == null)
            playerVisual = currentPlayer;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag))
            return;

        playerInRange = false;
        SetPromptVisible(false);

        if (!ownsDialogue)
        {
            currentPlayer = null;
            currentPlayerController = null;
        }
    }

    private void OnDisable()
    {
        bool shouldStopDialogue = ownsDialogue;
        ownsDialogue = false;

        if (shouldStopDialogue && dialogue != null)
            dialogue.StopDialogue();

        currentPlayerController?.ReleaseInputLock(this);
        SetPromptVisible(false);
    }

    private void FacePlayerToNpc()
    {
        if (currentPlayer == null || playerVisual == null)
            return;

        bool targetOnRight = transform.position.x > currentPlayer.position.x;
        SetFacing(playerVisual, targetOnRight, playerDefaultFaceRight);
    }

    private void FaceNpcToPlayer()
    {
        if (currentPlayer == null || npcVisual == null)
            return;

        bool targetOnRight = currentPlayer.position.x > transform.position.x;
        SetFacing(npcVisual, targetOnRight, npcDefaultFaceRight);
    }

    private static void SetFacing(Transform visual, bool targetOnRight, bool defaultFacesRight)
    {
        Vector3 scale = visual.localScale;
        float absoluteX = Mathf.Abs(scale.x);
        scale.x = targetOnRight == defaultFacesRight ? absoluteX : -absoluteX;
        visual.localScale = scale;
    }

    private void SetPromptVisible(bool visible)
    {
        if (textObject != null && textObject.activeSelf != visible)
            textObject.SetActive(visible);
    }
}
