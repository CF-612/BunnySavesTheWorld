using System;
using System.IO;
using UnityEngine;

/// <summary>
/// 统一管理场景流程和检查点所需的少量持久化进度。
/// 数据对象刻意不包含战斗、背包或任务数据，避免引入无关系统耦合。
/// </summary>
public static class GameProgressService
{
    private const string SaveFileName = "bunny-progress.json";

    private static GameProgressData data;
    private static string pendingCheckpointRespawnScene;

    public static bool HasStarted
    {
        get
        {
            EnsureLoaded();
            return data.hasStarted && !string.IsNullOrWhiteSpace(data.lastSceneName);
        }
    }

    public static string ContinueScene
    {
        get
        {
            EnsureLoaded();
            return data.lastSceneName;
        }
    }

    /// <summary>清空当前进度，并从指定场景开始一轮新游戏。</summary>
    public static void BeginNewGame(string firstSceneName)
    {
        data = new GameProgressData
        {
            hasStarted = true,
            lastSceneName = firstSceneName
        };

        pendingCheckpointRespawnScene = null;
        Save();
    }

    /// <summary>标记下一次加载存档场景时，需要执行一次检查点续玩定位。</summary>
    public static void RequestContinue()
    {
        EnsureLoaded();
        pendingCheckpointRespawnScene = data.lastSceneName;
    }

    public static bool ConsumeCheckpointRespawnRequest(string sceneName)
    {
        if (!string.Equals(pendingCheckpointRespawnScene, sceneName, StringComparison.Ordinal))
            return false;

        pendingCheckpointRespawnScene = null;
        return true;
    }

    public static void MarkSceneEntered(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName == "MainMenu")
            return;

        EnsureLoaded();
        data.hasStarted = true;
        data.lastSceneName = sceneName;
        Save();
    }

    public static void RecordPlayerPosition(string sceneName, Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(sceneName) || sceneName == "MainMenu")
            return;

        EnsureLoaded();
        data.hasStarted = true;
        data.lastSceneName = sceneName;
        data.lastPlayerPosition = position;
        Save();
    }

    /// <summary>保存检查点解锁记录，以及当前场景最新的重生位置。</summary>
    public static void ActivateCheckpoint(string checkpointId, string sceneName, Vector3 position)
    {
        if (string.IsNullOrWhiteSpace(checkpointId) || string.IsNullOrWhiteSpace(sceneName))
            return;

        EnsureLoaded();

        if (!data.unlockedCheckpointIds.Contains(checkpointId))
            data.unlockedCheckpointIds.Add(checkpointId);

        SceneCheckpointData checkpoint = data.sceneCheckpoints.Find(item => item.sceneName == sceneName);
        if (checkpoint == null)
        {
            checkpoint = new SceneCheckpointData { sceneName = sceneName };
            data.sceneCheckpoints.Add(checkpoint);
        }

        checkpoint.checkpointId = checkpointId;
        checkpoint.position = position;
        data.hasStarted = true;
        data.lastSceneName = sceneName;
        data.lastPlayerPosition = position;
        Save();
    }

    public static bool IsCheckpointUnlocked(string checkpointId)
    {
        EnsureLoaded();
        return !string.IsNullOrWhiteSpace(checkpointId) && data.unlockedCheckpointIds.Contains(checkpointId);
    }

    public static bool TryGetCheckpoint(string sceneName, out Vector3 position)
    {
        EnsureLoaded();
        SceneCheckpointData checkpoint = data.sceneCheckpoints.Find(item => item.sceneName == sceneName);
        if (checkpoint != null)
        {
            position = checkpoint.position;
            return true;
        }

        position = default;
        return false;
    }

    public static void Save()
    {
        EnsureLoaded();

        try
        {
            string path = GetSavePath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllText(path, JsonUtility.ToJson(data, true));
        }
        catch (Exception exception)
        {
            Debug.LogError($"保存游戏进度失败：{exception.Message}");
        }
    }

    private static void EnsureLoaded()
    {
        if (data != null)
            return;

        string path = GetSavePath();
        if (!File.Exists(path))
        {
            data = new GameProgressData();
            return;
        }

        try
        {
            data = JsonUtility.FromJson<GameProgressData>(File.ReadAllText(path));
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"读取游戏进度失败，将使用新进度：{exception.Message}");
        }

        data ??= new GameProgressData();
        data.unlockedCheckpointIds ??= new System.Collections.Generic.List<string>();
        data.sceneCheckpoints ??= new System.Collections.Generic.List<SceneCheckpointData>();
    }

    private static string GetSavePath()
    {
        return Path.Combine(Application.persistentDataPath, SaveFileName);
    }
}
