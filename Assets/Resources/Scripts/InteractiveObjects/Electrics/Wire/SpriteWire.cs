using UnityEngine;

public class SpriteWire : EntityWire
{
    [Header("图片替换")]
    [Tooltip("逐级受损贴图序列，数量应与 Max Bite Resistance 对应。")]
    [SerializeField] private Sprite[] damagedSprites;
    [Tooltip("完全断裂后的贴图。")]
    [SerializeField] private Sprite brokenSprite;

    private SpriteRenderer spriteRenderer;

    protected override void Awake()
    {
        base.Awake();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public override void OnBitten()
    {
        // 记录啃咬前的破损状态
        bool wasAlreadyBroken = isBroken;

        base.OnBitten();

        // 未被破坏则更新损伤贴图
        if (!isBroken && !wasAlreadyBroken)
        {
            UpdateDamageSprite();
        }
    }

    protected override void HandleDestruction()
    {
        // 替换为断裂贴图
        if (spriteRenderer != null && brokenSprite != null)
        {
            spriteRenderer.sprite = brokenSprite;
        }

        // 禁用碰撞体，阻止再次被啃咬检测命中
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }

        // 基类处理：标记破损 + 断裂火花 + 触发事件
        base.HandleDestruction();
    }

    private void UpdateDamageSprite()
    {
        if (spriteRenderer == null || damagedSprites == null || damagedSprites.Length == 0)
            return;

        // 损伤索引 = 总次数 - 剩余次数 - 1
        int damageIndex = maxBiteResistance - currentResistance - 1;

        if (damageIndex >= 0 && damageIndex < damagedSprites.Length)
        {
            if (damagedSprites[damageIndex] != null)
            {
                spriteRenderer.sprite = damagedSprites[damageIndex];
            }
        }
    }
}
