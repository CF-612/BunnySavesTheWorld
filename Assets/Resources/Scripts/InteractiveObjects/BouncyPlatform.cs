using UnityEngine;

public class BouncyPlatform : MonoBehaviour
{
    [Header("弹跳属性")]
    public float bounceForce = 25f;

    [Header("音效")]
    [SerializeField] private string bounceSFXPath = "Audio/SFX/BunnyJump/Jump1";

    [Header("组件引用")]
    [SerializeField] private Animator anim;

    // 动画参数名称，可在面板修改
    [SerializeField] private string animTriggerName = "bounce";

    private void Awake()
    {
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 获取接触点的法线。当玩家从上方落下踩到平台时，法线方向通常是向下的（y值接近-1）
        // 这样可以防止玩家从侧面撞到弹簧或者从下方顶到弹簧时也被错误弹飞
        if (collision.contacts.Length > 0 && collision.contacts[0].normal.y < -0.5f)
        {
            Player player = collision.gameObject.GetComponent<Player>();
            
            if (player != null)
            {
                // 将玩家的垂直速度强制设置为弹跳力，水平速度保持不变
                player.SetVelocity(player.rb.linearVelocity.x, bounceForce);

                // 播放弹跳音效
                if (AudioManager.Instance != null)
                    AudioManager.Instance.PlaySFX(bounceSFXPath, 1f, 0.9f, 1.1f);

                // 播放弹簧或蘑菇形变的动画
                if (anim != null)
                {
                    anim.SetTrigger(animTriggerName);
                }
            }
        }
    }
}
