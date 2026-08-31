using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class GameProgressData
{
    public const int CurrentVersion = 1;

    public int version = CurrentVersion;
    public bool hasStarted;
    public string lastSceneName;
    public Vector3 lastPlayerPosition;
    public List<string> unlockedCheckpointIds = new List<string>();
    public List<SceneCheckpointData> sceneCheckpoints = new List<SceneCheckpointData>();
}

[Serializable]
public sealed class SceneCheckpointData
{
    public string sceneName;
    public string checkpointId;
    public Vector3 position;
}
