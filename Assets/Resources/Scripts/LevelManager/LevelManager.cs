using System.Collections;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Player player;

    [Header("重生配置")]
    [Tooltip("死亡动画预估时长（秒），应与 Animator 中 dead 动画的实际长度一致")]
    [SerializeField] private float deathAnimDuration = 1.5f;

    private Vector3 respawnPosition;
    private bool isRespawning;

    private void Awake()
    {
        if (player != null)
        {
            respawnPosition = player.transform.position;
        }
    }

    private void OnEnable()
    {
        Player.OnPlayerDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDeath -= OnPlayerDeath;
    }

    /// <summary>由 CheckPoint 调用，更新重生点为检查点位置</summary>
    public void SetCheckpoint(Vector3 position)
    {
        respawnPosition = position;
    }

    private void OnPlayerDeath()
    {
        if (isRespawning) return;

        isRespawning = true;
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        // 第一阶段：等待死亡动画播放
        yield return new WaitForSeconds(deathAnimDuration);

        // 隐藏玩家视觉
        player.HideVisual();

        // 第二阶段：死亡停留时间
        yield return new WaitForSeconds(player.RespawnDelay);

        // 在检查点重生
        player.Respawn(respawnPosition);

        isRespawning = false;
    }
}
