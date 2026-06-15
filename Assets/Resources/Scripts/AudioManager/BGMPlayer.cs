using UnityEngine;

public class BGMPlayer : MonoBehaviour
{
    [Header("当前关卡 BGM Background Music")]
    public AudioClip bgmClip;  // 音效用：当前关卡要播放的背景音乐

    [Header("是否循环 Loop")]
    public bool loop = true;

    private void Start()
    {
        if (AudioManager.Instance == null)
        {
            Debug.LogWarning("没有找到 AudioManager，请确认是否从菜单场景进入游戏。");
            return;
        }

        AudioManager.Instance.PlayBGM(bgmClip, loop);  // 音效用：场景开始时，通知 AudioManager 播放当前关卡 BGM
    }
}