using System.Collections.Generic;

namespace WismUnity.Playground
{
    [System.Serializable]
    public sealed class UnityPlaygroundReport
    {
        public int schemaVersion;
        public string command;
        public string status;
        public string outcome;
        public string world;
        public string scenePath;
        public string runId;
        public string startedAtUtc;
        public string finishedAtUtc;
        public string unityVersion;
        public bool batchMode;
        public string artifactDirectory;
        public string screenshotPath;
        public UnityPlaygroundSceneSummary scene;
        public UnityPlaygroundGameSummary game;
        public UnityPlaygroundConsoleSummary console;
        public string[] dirtyScenes = new string[0];
        public List<string> events = new List<string>();
    }

    [System.Serializable]
    public sealed class UnityPlaygroundSceneSummary
    {
        public string name;
        public string path;
        public int rootGameObjectCount;
        public int sceneGameObjectCount;
        public bool isDirty;
    }

    [System.Serializable]
    public sealed class UnityPlaygroundGameSummary
    {
        public bool gameInitialized;
        public bool worldInitialized;
        public string worldName;
        public int mapWidth;
        public int mapHeight;
        public int cityCount;
        public int locationCount;
        public int playerCount;
        public string currentClan;
        public string executionMode;
        public int lastCommandId;
        public bool interactiveUI;
    }

    [System.Serializable]
    public sealed class UnityPlaygroundConsoleSummary
    {
        public bool available;
        public int errors;
        public int warnings;
        public int logs;
        public string note;
    }
}
