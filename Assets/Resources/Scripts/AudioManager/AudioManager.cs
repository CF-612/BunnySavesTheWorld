using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    #region Singleton & Lifecycle

    public static AudioManager Instance;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxCache = new Dictionary<string, AudioClip>();
        loopingSources = new Dictionary<string, AudioSource>();
        InitSfxPool();
    }

    #endregion

    #region BGM

    [Header("BGM")]
    [SerializeField] private AudioSource bgmSource;
    private AudioClip currentBGM;

    /// <summary>播放背景音乐。若已在播放同一首则跳过，避免重复从头播放。</summary>
    public void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (clip == null || bgmSource == null) return;

        if (currentBGM == clip && bgmSource.isPlaying) return;

        currentBGM = clip;
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    /// <summary>停止背景音乐并清除缓存，之后可重新播放同一首。</summary>
    public void StopBGM()
    {
        if (bgmSource == null) return;

        bgmSource.Stop();
        currentBGM = null;
    }

    #endregion

    #region SFX

    [Header("SFX 音源池（数量 = 最大可重叠音效数）")]
    [SerializeField] private AudioSource[] sfxSources;

    private int nextSfxIndex;

    // 专用于可随时启停的循环音效（多通道，按路径隔离，互不干扰）
    private Dictionary<string, AudioSource> loopingSources;

    // Resources 路径 → Clip 缓存，避免重复加载
    private Dictionary<string, AudioClip> sfxCache;

    /// <summary>若未在 Inspector 中配置音源池，自动创建默认 5 个。</summary>
    private void InitSfxPool()
    {
        if (sfxSources != null && sfxSources.Length > 0) return;

        sfxSources = new AudioSource[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject child = new GameObject($"SFX_Source_{i}");
            child.transform.SetParent(transform);
            AudioSource src = child.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxSources[i] = src;
        }
    }

    /// <summary>为指定路径获取或创建独占的循环音源。</summary>
    private AudioSource GetLoopingSource(string path)
    {
        if (!loopingSources.TryGetValue(path, out AudioSource source))
        {
            GameObject child = new GameObject($"LoopingSFX_{path.Replace('/', '_')}");
            child.transform.SetParent(transform);
            source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            loopingSources[path] = source;
        }
        return source;
    }

    /// <summary>播放循环音效（如拖拽声、风扇运转）。每类音效独占一个通道，互不干扰。</summary>
    public void PlayLoopingSFX(string path, float volume = 1f)
    {
        AudioClip clip = GetClip(path);
        if (clip == null) return;

        AudioSource source = GetLoopingSource(path);

        // 同一段音效已在播放则跳过
        if (source.clip == clip && source.isPlaying) return;

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.Play();
    }

    /// <summary>停止指定路径的循环音效。</summary>
    public void StopLoopingSFX(string path)
    {
        if (loopingSources.TryGetValue(path, out AudioSource source))
            source.Stop();
    }

    /// <summary>停止所有循环音效。</summary>
    public void StopLoopingSFX()
    {
        foreach (var source in loopingSources.Values)
        {
            if (source != null) source.Stop();
        }
    }

    /// <summary>
    /// 播放单个音效。
    /// </summary>
    /// <param name="clip">音效剪辑</param>
    /// <param name="volume">音量 0~1</param>
    /// <param name="pitchMin">随机音高下限（1 = 原音高）</param>
    /// <param name="pitchMax">随机音高上限</param>
    public void PlaySFX(AudioClip clip, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clip == null) return;

        AudioSource source = GetFreeSfxSource();
        if (source == null) return;

        source.clip = clip;
        source.volume = Mathf.Clamp01(volume);
        source.pitch = Random.Range(pitchMin, pitchMax);
        source.loop = false;
        source.Play();
    }

    /// <summary>
    /// 从候选数组中随机选一个音效播放。
    /// 适用于脚步声、受伤呻吟等需要变化感的重复音效。
    /// </summary>
    public void PlayRandomSFX(AudioClip[] clips, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (clips == null || clips.Length == 0) return;

        int index = Random.Range(0, clips.Length);
        PlaySFX(clips[index], volume, pitchMin, pitchMax);
    }

    // ──────────── Resources 路径重载 ────────────

    /// <summary>
    /// 通过 Resources 路径播放单个音效（自动缓存）。
    /// </summary>
    /// <param name="path">Resources 相对路径，如 "BunnyJump/Jump1"</param>
    public void PlaySFX(string path, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        AudioClip clip = GetClip(path);
        if (clip != null)
            PlaySFX(clip, volume, pitchMin, pitchMax);
    }

    /// <summary>
    /// 从 Resources 路径数组中随机选一个播放（自动缓存）。
    /// </summary>
    public void PlayRandomSFX(string[] paths, float volume = 1f, float pitchMin = 1f, float pitchMax = 1f)
    {
        if (paths == null || paths.Length == 0) return;

        int index = Random.Range(0, paths.Length);
        PlaySFX(paths[index], volume, pitchMin, pitchMax);
    }

    /// <summary>
    /// 从缓存获取 Clip，未命中则 Resources.Load 并缓存。
    /// </summary>
    private AudioClip GetClip(string path)
    {
        if (string.IsNullOrEmpty(path)) return null;

        if (sfxCache.TryGetValue(path, out AudioClip cached))
            return cached;

        AudioClip clip = Resources.Load<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"AudioManager: 找不到 Resources 路径下的音频 —— \"{path}\"");
            return null;
        }

        sfxCache[path] = clip;
        return clip;
    }

    /// <summary>
    /// 从音源池中获取一个空闲源。全部占用时轮询复用最早播放的源。
    /// </summary>
    private AudioSource GetFreeSfxSource()
    {
        if (sfxSources == null || sfxSources.Length == 0) return null;

        // 优先返回空闲源
        for (int i = 0; i < sfxSources.Length; i++)
        {
            int idx = (nextSfxIndex + i) % sfxSources.Length;
            if (sfxSources[idx] != null && !sfxSources[idx].isPlaying)
            {
                nextSfxIndex = (idx + 1) % sfxSources.Length;
                return sfxSources[idx];
            }
        }

        // 全部占用，轮询复用
        AudioSource fallback = sfxSources[nextSfxIndex];
        nextSfxIndex = (nextSfxIndex + 1) % sfxSources.Length;
        return fallback;
    }

    #endregion
}
