using UnityEngine;

public class TriggerUIText : MonoBehaviour
{
    [Header("要显示/隐藏的文字物体 Text Object")]
    public GameObject textObject; // 可以拖 UI Text、TMP Text、场景里的 TextMeshPro、3D Text

    [Header("玩家标签 Player Tag")]
    public string playerTag = "Player";

    private void Start()
    {
        if (textObject != null)
        {
            textObject.SetActive(false); // 游戏开始先隐藏
        }
    }

    private void OnTriggerEnter2D(Collider2D other) // 当有 2D 碰撞体进入这个触发器时自动执行
    {
        if (other.CompareTag(playerTag)) // 判断进入触发器的物体是不是玩家
        {
            ShowText(); // 如果是玩家，就显示文字
        }
    }

    private void OnTriggerExit2D(Collider2D other) // 当有 2D 碰撞体离开这个触发器时自动执行
    {
        if (other.CompareTag(playerTag))
        {
            HideText(); // 如果离开的物体是玩家，就隐藏文字
        }
    }

    private void ShowText() // 显示文字的方法
    {
        if (textObject != null)
        {
            textObject.SetActive(true);
        }
    }

    private void HideText() // 隐藏文字的方法
    {
        if (textObject != null)
        {
            textObject.SetActive(false);
        }
    }
}