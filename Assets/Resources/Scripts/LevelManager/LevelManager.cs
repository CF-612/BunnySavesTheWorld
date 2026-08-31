using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>负责当前场景内的检查点选择，以及死亡与重生流程协调。</summary>
public class LevelManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private Player player;

    [Header("重生配置")]
    [Tooltip("死亡动画预估时长（秒），应与 Animator 中 dead 动画的实际长度一致")]
    [SerializeField] private float deathAnimDuration = 1.5f;

    private Vector3 respawnPosition;
    private bool isRespawning;
    private bool resumeAtCheckpoint;

    private void Awake()
    {
        if (player == null)
            player = FindAnyObjectByType<Player>();

        if (player != null)
            respawnPosition = player.transform.position;

        string sceneName = SceneManager.GetActiveScene().name;
        if (GameProgressService.TryGetCheckpoint(sceneName, out Vector3 savedPosition))
            respawnPosition = savedPosition;

        resumeAtCheckpoint = GameProgressService.ConsumeCheckpointRespawnRequest(sceneName);
    }

    private void Start()
    {
        if (resumeAtCheckpoint && player != null)
            player.TeleportTo(respawnPosition);
    }

    private void OnEnable()
    {
        Player.OnPlayerDeath += OnPlayerDeath;
    }

    private void OnDisable()
    {
        Player.OnPlayerDeath -= OnPlayerDeath;
        StopAllCoroutines();
        isRespawning = false;
    }

    public void SetCheckpoint(CheckPoint checkpoint)
    {
        if (checkpoint == null)
            return;

        respawnPosition = checkpoint.RespawnPosition;
        GameProgressService.ActivateCheckpoint(
            checkpoint.CheckpointId,
            SceneManager.GetActiveScene().name,
            respawnPosition);
    }

    /// <summary>为现有 UnityEvent 和旧检查点脚本保留的兼容入口。</summary>
    public void SetCheckpoint(Vector3 position)
    {
        respawnPosition = position;
        GameProgressService.RecordPlayerPosition(SceneManager.GetActiveScene().name, position);
    }

    private void OnPlayerDeath()
    {
        if (isRespawning || player == null)
            return;

        isRespawning = true;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        if (deathAnimDuration > 0f)
            yield return new WaitForSeconds(deathAnimDuration);

        player.HideVisual();

        if (player.RespawnDelay > 0f)
            yield return new WaitForSeconds(player.RespawnDelay);

        player.Respawn(respawnPosition);
        isRespawning = false;
    }
}
