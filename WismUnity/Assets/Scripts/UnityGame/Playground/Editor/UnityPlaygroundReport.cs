using System.Collections.Generic;
using Assets.Scripts.UnityGame.ModKit;

namespace WismUnity.Playground
{
    [System.Serializable]
    public sealed class UnityPlaygroundReport
    {
        public int schemaVersion;
        public string command;
        public string status;
        public string outcome;
        public string profile;
        public string[] packs;
        public string world;
        public string modRoot;
        public string scenePath;
        public string scenarioName;
        public string runId;
        public string startedAtUtc;
        public string finishedAtUtc;
        public string unityVersion;
        public bool batchMode;
        public string artifactDirectory;
        public string screenshotPath;
        public UnityPlaygroundSceneSummary scene;
        public UnityModKitSelectionReport selection;
        public UnityPlaygroundGameSummary game;
        public UnityPlaygroundScenarioSummary scenario;
        public UnityPlaygroundConsoleSummary console;
        public UnityPlaygroundMixedModeSummary mixedMode;
        public List<UnityPlaygroundInvariantEntry> invariants = new List<UnityPlaygroundInvariantEntry>();
        public List<UnityPlaygroundScreenshotEntry> screenshots = new List<UnityPlaygroundScreenshotEntry>();
        public List<UnityPlaygroundCommandTraceEntry> commandTrace = new List<UnityPlaygroundCommandTraceEntry>();
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
    public sealed class UnityPlaygroundScenarioSummary
    {
        public string name;
        public string status;
        public string outcome;
        public int maxTicks;
        public int ticksRun;
        public int startLastCommandId;
        public int endLastCommandId;
        public int queuedCommandCount;
        public int executedCommandCount;
        public string startingClan;
        public string endingClan;
    }

    [System.Serializable]
    public sealed class UnityPlaygroundMixedModeSummary
    {
        public int seed;
        public bool fuzz;
        public int humanAgentCount;
        public int aiAgentCount;
        public int turnsRequested;
        public int turnsCompleted;
        public int scriptedHumanTurns;
        public int aiTurnsObserved;
        public int commandStalls;
        public int humanDecisionsApplied;
        public int humanDecisionFallbacks;
        public int cityCaptures;
        public int searches;
        public int battles;
        public int stuckCommandId;
        public string stuckCommandType;
        public string humanDecisionScriptPath;
        public string[] humanClans = new string[0];
        public string[] aiClans = new string[0];
    }

    [System.Serializable]
    public sealed class UnityPlaygroundInvariantEntry
    {
        public string name;
        public string status;
        public string evidence;
    }

    [System.Serializable]
    public sealed class UnityPlaygroundScreenshotEntry
    {
        public string label;
        public string path;
    }

    [System.Serializable]
    public sealed class UnityPlaygroundCommandTraceEntry
    {
        public int id;
        public string commandType;
        public string result;
        public bool advanced;
        public string playerClan;
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
