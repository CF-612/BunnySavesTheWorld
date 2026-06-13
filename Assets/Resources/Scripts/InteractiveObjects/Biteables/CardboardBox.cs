using UnityEngine;

public class CardboardBox : MonoBehaviour, IBiteable
{
    [Header("贴图配置")]
    [Tooltip("按顺序排列的贴图：[0]=完好，[1..n]=逐级受损。默认3张：1张完好+2张受损。")]
    [SerializeField] private Sprite[] sprites;

    private SpriteRenderer spriteRenderer;
    private int currentSpriteIndex = 0;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // 确保初始显示第一张贴图
        if (sprites != null && sprites.Length > 0 && spriteRenderer != null)
        {
            spriteRenderer.sprite = sprites[0];
        }
    }

    public void OnBitten()
    {
        // 无贴图或已到最后一张，不做任何变化
        if (sprites == null || sprites.Length <= 1 || currentSpriteIndex >= sprites.Length - 1)
            return;

        currentSpriteIndex++;

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprites[currentSpriteIndex];
        }
    }

    public bool GetIsBroken()
    {
        // 纸箱不会被啃坏
        return false;
    }
}
