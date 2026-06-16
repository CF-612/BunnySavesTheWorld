using UnityEngine;

/// <summary>
/// 挂载在场景 GameObject 上，声明该场景要播放的 BGM。
/// Start 时自动通过 AudioManager 播放，切换场景后由新场景的 BGMPlayer 接管。
/// </summary>
public class BGMPlayer : MonoBehaviour
{
    [Header("场景 BGM")]
    public AudioClip bgmClip;

    [Header("是否循环")]
    public bool loop = true;

    private void Start()
    {
        if (bgmClip == null)
        {
            Debug.LogWarning("BGMPlayer：bgmClip 未配置，跳过。", this);
            return;
        }

        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("BGMPlayer：AudioManager 未找到，BGM 不会播放。请确保场景中存在挂载 AudioManager 的 GameObject。");
            return;
        }

        AudioManager.Instance.PlayBGM(bgmClip, loop);
    }
}
