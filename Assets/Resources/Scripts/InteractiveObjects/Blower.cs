using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Blower : MonoBehaviour
{
    [Header("吹风机设置")]
    public bool IsOn = true;
    public Vector2 WindForce = new Vector2(5f, 0f);

    [Header("组件引用")]
    [SerializeField] private ParticleSystem windVFX;
    [SerializeField] private Animator anim;

    private BoxCollider2D windZone;
    private Player playerInZone;

    private void Awake()
    {
        windZone = GetComponent<BoxCollider2D>();
        windZone.isTrigger = true;

        if (anim == null)
            anim = GetComponent<Animator>();
    }

    private void Start()
    {
        // 根据初始状态进行激活
        if (IsOn)
            TurnOn();
        else
            TurnOff();
    }

    public void TurnOn()
    {
        IsOn = true;
        if (anim != null) anim.SetBool("IsOn", IsOn);
        if (windVFX != null && !windVFX.isPlaying) windVFX.Play();
    }

    public void TurnOff()
    {
        IsOn = false;
        if (anim != null) anim.SetBool("IsOn", IsOn);
        if (windVFX != null && windVFX.isPlaying) windVFX.Stop();

        // 如果关闭时玩家正好处于风区内，需清空玩家的受力缓冲
        if (playerInZone != null)
        {
            playerInZone.SetWindVelocity(Vector2.zero);
            playerInZone = null;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!IsOn) return;

        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            playerInZone = player;
            player.SetWindVelocity(WindForce);
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