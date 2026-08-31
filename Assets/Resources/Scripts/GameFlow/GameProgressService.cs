using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Owns the small amount of persistent progress used by scene flow and checkpoints.
/// The DTO deliberately contains no combat, inventory, or quest data.
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

    /// <summary>Replaces the current progress with a clean run that starts in the supplied scene.</summary>
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

    /// <summary>Marks the next load of the saved scene as a one-time checkpoint resume.</summary>
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

    /// <summary>Persists both checkpoint history and the latest respawn point for this scene.</summary>
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
