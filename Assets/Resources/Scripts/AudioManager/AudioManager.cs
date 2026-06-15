using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // 音效用：全局单例 Instance
    // 其他脚本可以通过 AudioManager 调用音频播放
    public static AudioManager Instance;

    [Header("BGM 音源 Background Music Source")]
    public AudioSource bgmSource; 
    [Header("SFX 音源 Sound Effects Source")]
    public AudioSource sfxSource; 
    private AudioClip currentBGM; // 音效用：记录当前正在播放的 BGM，避免同一首音乐重复从头播放

    private void Awake()
    {
        // 音效用：防止场景切换后出现多个 AudioManager
        // 如果已经存在一个 AudioManager，就销毁新生成的这个
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;  // 设置当前对象为全局唯一 AudioManager
        DontDestroyOnLoad(gameObject);   // 切换场景时不销毁 AudioManager，保证 BGM / SFX 系统持续存在
    }

    public void PlayBGM(AudioClip clip, bool loop = true) //播放BGM
    {
        if (clip == null) return;
        if (bgmSource == null) return;

        // 音效用：如果当前已经在播放同一首 BGM，就不重复播放
        // 这样可以避免切换场景或重复进入触发点时，音乐不断从头开始
        if (currentBGM == clip && bgmSource.isPlaying) return;

        currentBGM = clip; 
        bgmSource.clip = clip;  
        bgmSource.loop = loop; 
        bgmSource.Play(); // 开始播放背景音乐
    }

    public void StopBGM() //停止BGM
    {
        if (bgmSource == null) return;

        // 音效用：停止当前背景音乐，并且清空当前 BGM 记录，方便之后重新播放
        bgmSource.Stop();
        currentBGM = null;
    }

    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        if (sfxSource == null) return;

        // 音效用：播放一次性音效
        // PlayOneShot 不会替换当前正在播放的音效，适合攻击、受伤、按钮、拾取等短音效
        sfxSource.PlayOneShot(clip);
    }
}