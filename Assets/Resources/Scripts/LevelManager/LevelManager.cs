using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Scene-local owner for checkpoint selection and death/respawn orchestration.</summary>
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

    /// <summary>Compatibility entry point for existing UnityEvents and older checkpoint scripts.</summary>
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
