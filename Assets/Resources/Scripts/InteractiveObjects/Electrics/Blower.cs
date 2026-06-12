using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Blower : EntityEle
{
    [Header("吹风机独占设置")]
    public Vector2 windForce = new Vector2(5f, 0f);
    [SerializeField] private ParticleSystem windVFX;

    private BoxCollider2D windZone;
    private Player playerInZone;

    protected override void Awake()
    {
        // 调用基类 Awake 获取动画器
        base.Awake();
        
        windZone = GetComponent<BoxCollider2D>();
        windZone.isTrigger = true;
    }

    public override void TurnOn()
    {
        base.TurnOn();
            
        if (windVFX != null && !windVFX.isPlaying) 
            windVFX.Play();
    }

    public override void TurnOff()
    {
        base.TurnOff();
            
        if (windVFX != null && windVFX.isPlaying) 
            windVFX.Stop();

        // 如果关闭时玩家正好处于风区内，需清空玩家的受力缓冲
        if (playerInZone != null)
        {
            playerInZone.SetWindVelocity(Vector2.zero);
            playerInZone = null;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!isOn) return;

        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            playerInZone = player;
            player.SetWindVelocity(windForce);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            // 离开风区时清空风力
            player.SetWindVelocity(Vector2.zero);
            if (playerInZone == player)
            {
                playerInZone = null;
            }
        }
    }
}