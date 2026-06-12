using NodeCanvas.DialogueTrees;
using NodeCanvas.DialogueTrees.UI.Examples;
using UnityEngine;

public class NPCTalk : MonoBehaviour
{
    [Header("要显示/隐藏的文字物体 Text Object")]
    public GameObject textObject;

    [Header("玩家标签 Player Tag")]
    public string playerTag = "Player";

    [Header("NodeCanvas 对话 Dialogue")]
    [SerializeField] private DialogueTreeController dialogue;

    [Header("玩家朝向设置 Player Facing")]
    public Transform playerVisual; // 拖玩家身上负责显示美术的物体；没有就拖玩家自己
    public bool playerDefaultFaceRight = true; // 玩家美术默认是否面朝右

    [Header("NPC朝向设置 NPC Facing")]
    public Transform npcVisual; // 拖 NPC 身上负责显示美术的物体；没有就拖 NPC 自己
    public bool npcDefaultFaceRight = true; // NPC 美术默认是否面朝右

    private bool playerInRange;
    private DialogueUGUI ugui;
    private Transform currentPlayer;

    private void Start()
    {
        if (textObject != null)
            textObject.SetActive(false);

        if (dialogue == null)
            dialogue = GetComponent<DialogueTreeController>();

        if (npcVisual == null)
            npcVisual = transform;

        ugui = FindObjectOfType<DialogueUGUI>();
    }

    private void Update()
    {
        bool isTalking = ugui != null && ugui.isTalking;

        // 没有对话时，玩家在范围内才显示“按E对话”
        if (playerInRange && !isTalking)
            ShowText();
        else
            HideText();

        // 按 E 开始对话：玩家和 NPC 同时转向对方
        if (playerInRange && !isTalking && Input.GetKeyDown(KeyCode.E))
        {
            FacePlayerToNPC();
            FaceNPCToPlayer();

            if (dialogue != null)
            {
                dialogue.StartDialogue();
                HideText();
            }
            else
            {
                Debug.LogWarning("NPCTalk：DialogueTreeController 没有拖，也没有挂在当前物体上。");
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = true;
            currentPlayer = other.transform;

            if (playerVisual == null)
                playerVisual = currentPlayer;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInRange = false;
            currentPlayer = null;
            HideText();
        }
    }

    private void FacePlayerToNPC()
    {
        if (currentPlayer == null || playerVisual == null) return;

        // NPC 在玩家右边，玩家应该面朝右；NPC 在玩家左边，玩家应该面朝左
        bool npcOnRight = transform.position.x > currentPlayer.position.x;

        Vector3 scale = playerVisual.localScale;
        float absX = Mathf.Abs(scale.x);

        if (playerDefaultFaceRight)
            scale.x = npcOnRight ? absX : -absX;
        else
            scale.x = npcOnRight ? -absX : absX;

        playerVisual.localScale = scale;
    }

    private void FaceNPCToPlayer()
    {
        if (currentPlayer == null || npcVisual == null) return;

        // 玩家在 NPC 右边，NPC 应该面朝右；玩家在 NPC 左边，NPC 应该面朝左
        bool playerOnRight = currentPlayer.position.x > transform.position.x;

        Vector3 scale = npcVisual.localScale;
        float absX = Mathf.Abs(scale.x);

        if (npcDefaultFaceRight)
            scale.x = playerOnRight ? absX : -absX;
        else
            scale.x = playerOnRight ? -absX : absX;

        npcVisual.localScale = scale;
    }

    private void ShowText()
    {
        if (textObject != null)
            textObject.SetActive(true);
    }

    private void HideText()
    {
        if (textObject != null)
            textObject.SetActive(false);
    }
}