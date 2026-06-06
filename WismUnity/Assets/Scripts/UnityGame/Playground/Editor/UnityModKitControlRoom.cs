using System;
using System.IO;
using Assets.Scripts.UnityGame.ModKit;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WismUnity.Playground
{
    public sealed class UnityModKitControlRoom : EditorWindow
    {
        const string PrefPrefix = "WISM.ModKit.ControlRoom.";
        const string DefaultOutputRoot = "artifacts/mod-kit/control-room";

        string profileId;
        string packIds;
        string worldName;
        string modRoot;
        string outputRoot;
        Vector2 scroll;
        UnityModKitSelectionReport lastReport;
        string lastManifestPath;
        string lastMessage;

        [MenuItem("WISM/Mod Kit/Control Room")]
        public static void Open()
        {
            var window = GetWindow<UnityModKitControlRoom>("WISM Mod Kit");
            window.minSize = new Vector2(520, 560);
            window.Show();
        }

        void OnEnable()
        {
            profileId = EditorPrefs.GetString(PrefPrefix + "ProfileId", "classic-warlords");
            packIds = EditorPrefs.GetString(PrefPrefix + "PackIds", string.Empty);
            worldName = EditorPrefs.GetString(PrefPrefix + "WorldName", string.Empty);
            modRoot = EditorPrefs.GetString(PrefPrefix + "ModRoot", UnityModKitSelection.PluginModRoot);
            outputRoot = EditorPrefs.GetString(PrefPrefix + "OutputRoot", DefaultOutputRoot);
        }

        void OnGUI()
        {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            DrawHeader();
            DrawSelection();
            DrawActions();
            DrawReport();
            EditorGUILayout.EndScrollView();
        }

        void DrawHeader()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Mod Kit Control Room", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Read-only profile, pack, world, and Unity scene status for Mod Kit demos. This window does not save scenes, import maps, export MOD files, or mutate scene objects.",
                MessageType.Info);
        }

        void DrawSelection()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Selection", EditorStyles.boldLabel);
            profileId = EditorGUILayout.TextField("Profile", profileId);
            packIds = EditorGUILayout.TextField("Packs", packIds);
            worldName = EditorGUILayout.TextField("World Override", worldName);
            modRoot = EditorGUILayout.TextField("Mod Root", modRoot);
            outputRoot = EditorGUILayout.TextField("Output Root", outputRoot);

            if (GUILayout.Button("Save Settings"))
            {
                SaveSettings();
                lastMessage = "Saved Control Room settings.";
            }
        }

        void DrawActions()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Refresh Status"))
                {
                    RefreshStatus();
                }

                if (GUILayout.Button("Write Status Manifest"))
                {
                    RefreshStatus();
                    WriteStatusManifest();
                }
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Copy Smoke Command"))
                {
                    EditorGUIUtility.systemCopyBuffer = BuildSmokeCommand();
                    lastMessage = "Copied Unity Playground smoke command.";
                }

                if (GUILayout.Button("Reveal Artifacts"))
                {
                    Directory.CreateDirectory(ResolveOutputRoot());
                    EditorUtility.RevealInFinder(ResolveOutputRoot());
                }
            }

            if (!string.IsNullOrWhiteSpace(lastMessage))
            {
                EditorGUILayout.HelpBox(lastMessage, MessageType.None);
            }
        }

        void DrawReport()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Latest Report", EditorStyles.boldLabel);
            if (lastReport == null)
            {
                EditorGUILayout.HelpBox("No report yet. Click Refresh Status.", MessageType.Warning);
                return;
            }

            DrawLine("Status", lastReport.status);
            DrawLine("Outcome", lastReport.outcome);
            DrawLine("Profile", lastReport.profileId);
            DrawLine("Packs", lastReport.activePackIds.Length == 0 ? "(none)" : string.Join(", ", lastReport.activePackIds));
            DrawLine("World", lastReport.worldName);
            DrawLine("Mod Root", lastReport.modRoot);
            DrawLine("Compatibility", lastReport.compatibilityStatus);
            DrawLine("Green", lastReport.isGreen ? "Yes" : "No");
            DrawLine("Fingerprint", lastReport.contentFingerprint);
            DrawLine("Validation", lastReport.validation == null ? "Default selection" : $"{lastReport.validation.issueCount} issue(s)");
            DrawLine("Dirty Scenes", string.Join(", ", GetLoadedDirtyScenes()));
            DrawLine("Active Scene", EditorSceneManager.GetActiveScene().path);
            DrawLine("Last Manifest", string.IsNullOrWhiteSpace(lastManifestPath) ? "(none)" : lastManifestPath);

            if (lastReport.sceneModDrift != null)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("MOD Data Summary", EditorStyles.boldLabel);
                DrawLine("World Root", lastReport.sceneModDrift.worldRoot);
                DrawLine("City JSON", $"{lastReport.sceneModDrift.cityJsonCount} item(s)");
                DrawLine("Location JSON", $"{lastReport.sceneModDrift.locationJsonCount} item(s)");
                DrawLine("Note", lastReport.sceneModDrift.note);
            }

            if (lastReport.validation != null && lastReport.validation.issues.Length > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Issues", EditorStyles.boldLabel);
                foreach (var issue in lastReport.validation.issues)
                {
                    EditorGUILayout.HelpBox($"{issue.severity} {issue.code}: {issue.message}\n{issue.path}", MessageType.Warning);
                }
            }

            if (lastReport.compatibilityIssues != null && lastReport.compatibilityIssues.Length > 0)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField("Compatibility Issues", EditorStyles.boldLabel);
                foreach (var issue in lastReport.compatibilityIssues)
                {
                    EditorGUILayout.HelpBox($"{issue.severity} {issue.code}: {issue.message}\n{issue.path}", MessageType.Warning);
                }
            }
        }

        void RefreshStatus()
        {
            lastReport = UnityModKitSelection.Inspect(
                profileId,
                ParsePackIds(packIds),
                worldName,
                modRoot);
            lastMessage = "Refreshed read-only Mod Kit status.";
        }

        void WriteStatusManifest()
        {
            if (lastReport == null)
            {
                RefreshStatus();
            }

            var runId = $"control-room-status-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var directory = Path.Combine(ResolveOutputRoot(), runId);
            Directory.CreateDirectory(directory);
            lastManifestPath = Path.Combine(directory, "modkit-status.json");
            var manifest = new ControlRoomStatusManifest
            {
                schemaVersion = 1,
                runId = runId,
                status = string.Equals(lastReport.status, "Failed", StringComparison.OrdinalIgnoreCase) ? "Failed" : "Passed",
                generatedAtUtc = DateTime.UtcNow.ToString("O"),
                unityVersion = Application.unityVersion,
                projectPath = Directory.GetCurrentDirectory(),
                activeScene = EditorSceneManager.GetActiveScene().path,
                dirtyScenes = GetLoadedDirtyScenes(),
                selection = lastReport,
                smokeCommand = BuildSmokeCommand()
            };

            File.WriteAllText(lastManifestPath, JsonUtility.ToJson(manifest, true));
            lastMessage = $"Wrote status manifest: {lastManifestPath}";
        }

        string BuildSmokeCommand()
        {
            var runId = $"control-room-smoke-{DateTime.UtcNow:yyyyMMdd-HHmmss}";
            var output = ResolveOutputRoot().Replace("\\", "/");
            return "-executeMethod WismUnity.Playground.UnityPlaygroundCli.Run " +
                   $"command=world profile={profileId} packs={packIds} world={worldName} modRoot={modRoot} " +
                   $"scenario=smoke runId={runId} out={output}";
        }

        string ResolveOutputRoot()
        {
            var root = string.IsNullOrWhiteSpace(outputRoot) ? DefaultOutputRoot : outputRoot;
            return Path.GetFullPath(root);
        }

        void SaveSettings()
        {
            EditorPrefs.SetString(PrefPrefix + "ProfileId", profileId ?? string.Empty);
            EditorPrefs.SetString(PrefPrefix + "PackIds", packIds ?? string.Empty);
            EditorPrefs.SetString(PrefPrefix + "WorldName", worldName ?? string.Empty);
            EditorPrefs.SetString(PrefPrefix + "ModRoot", modRoot ?? string.Empty);
            EditorPrefs.SetString(PrefPrefix + "OutputRoot", outputRoot ?? string.Empty);
        }

        static string[] ParsePackIds(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? Array.Empty<string>()
                : value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        static string[] GetLoadedDirtyScenes()
        {
            var dirty = new string[SceneManager.sceneCount];
            var count = 0;
            for (var index = 0; index < SceneManager.sceneCount; index++)
            {
                var scene = SceneManager.GetSceneAt(index);
                if (scene.isDirty)
                {
                    dirty[count++] = string.IsNullOrWhiteSpace(scene.path) ? scene.name : scene.path;
                }
            }

            Array.Resize(ref dirty, count);
            return dirty;
        }

        static void DrawLine(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(label, GUILayout.Width(120));
                EditorGUILayout.SelectableLabel(value ?? string.Empty, EditorStyles.wordWrappedLabel, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        [Serializable]
        sealed class ControlRoomStatusManifest
        {
            public int schemaVersion;
            public string runId;
            public string status;
            public string generatedAtUtc;
            public string unityVersion;
            public string projectPath;
            public string activeScene;
            public string[] dirtyScenes = Array.Empty<string>();
            public UnityModKitSelectionReport selection;
            public string smokeCommand;
        }
    }
}
