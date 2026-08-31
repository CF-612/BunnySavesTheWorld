using System.Text;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CheckPoint : MonoBehaviour
{
    [Header("持久化")]
    [SerializeField] private string checkpointId;
    [SerializeField] private Transform respawnPoint;

    [Header("表现（可选）")]
    [SerializeField] private Animator animator;
    [SerializeField] private string activeAnimatorParameter = "isActive";
    [SerializeField] private AudioSource activationAudio;

    private LevelManager levelManager;
    private bool isActive;

    public string CheckpointId => GetStableCheckpointId();
    public Vector3 RespawnPosition => respawnPoint != null ? respawnPoint.position : transform.position;

    private void Awake()
    {
        levelManager = FindAnyObjectByType<LevelManager>();
        GetComponent<Collider2D>().isTrigger = true;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        if (activationAudio == null)
            activationAudio = GetComponent<AudioSource>();
    }

    private void Start()
    {
        SetPresentation(GameProgressService.IsCheckpointUnlocked(CheckpointId), false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (levelManager == null)
            levelManager = FindAnyObjectByType<LevelManager>();

        SetPresentation(true, !isActive);
        levelManager?.SetCheckpoint(this);
    }

    private void SetPresentation(bool active, bool playAudio)
    {
        isActive = active;

        if (animator != null && !string.IsNullOrWhiteSpace(activeAnimatorParameter))
            animator.SetBool(activeAnimatorParameter, active);

        if (activationAudio == null)
            return;

        if (active)
        {
            if (playAudio && !activationAudio.isPlaying)
                activationAudio.Play();
        }
        else if (activationAudio.isPlaying)
        {
            activationAudio.Stop();
        }
    }

    private string GetStableCheckpointId()
    {
        if (!string.IsNullOrWhiteSpace(checkpointId))
            return checkpointId;

        // Existing scene instances predate persistent IDs. The scene/hierarchy path gives them a
        // deterministic identity without forcing high-risk scene YAML edits during this migration.
        StringBuilder path = new StringBuilder(transform.name);
        Transform parent = transform.parent;
        while (parent != null)
        {
            path.Insert(0, '/');
            path.Insert(0, parent.name);
            parent = parent.parent;
        }

        return $"{gameObject.scene.name}:{path}";
    }

    private void OnDrawGizmos()
    {
        Vector3 position = respawnPoint != null ? respawnPoint.position : transform.position;
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.4f);
        Gizmos.DrawCube(position, new Vector3(1f, 2f, 0f));
        Gizmos.color = new Color(0f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireCube(position, new Vector3(1f, 2f, 0f));
        Gizmos.color = Color.white;
        Gizmos.DrawLine(position, position + Vector3.up * 1.5f);
        Gizmos.DrawWireSphere(position + Vector3.up * 1.5f, 0.2f);
    }
}
