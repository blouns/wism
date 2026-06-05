using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.AI.MCP.Editor.Helpers;
using Unity.AI.MCP.Editor.ToolRegistry;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.PackageManager;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace WismUnity.EditorBridge
{
    public static class WismUnityMcpTools
    {
        const string Group = "wismunity";

        [McpTool("WismUnity.GetProjectStatus", "Returns public-safe WismUnity project, editor, build target, and active scene status.", Groups = new[] { Group, "editor" }, EnabledByDefault = true)]
        public static object GetProjectStatus()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var projectRoot = Directory.GetCurrentDirectory();

            return Response.Success("WismUnity project status loaded.", new
            {
                projectName = new DirectoryInfo(projectRoot).Name,
                unityVersion = Application.unityVersion,
                editorVersion = InternalEditorUtility.GetFullUnityVersion(),
                buildTarget = EditorUserBuildSettings.activeBuildTarget.ToString(),
                activeScene = SceneInfo(scene),
                isPlaying = EditorApplication.isPlaying,
                isCompiling = EditorApplication.isCompiling,
                isUpdating = EditorApplication.isUpdating,
                hasUnsavedSceneChanges = scene.isDirty,
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetPackageStatus", "Returns installed package versions relevant to WismUnity and Unity AI Assistant integration.", Groups = new[] { Group, "packages" }, EnabledByDefault = true)]
        public static object GetPackageStatus()
        {
            var packages = new[]
            {
                "com.unity.ai.assistant",
                "com.unity.nuget.newtonsoft-json",
                "com.unity.ugui",
                "com.unity.test-framework"
            };

            return Response.Success("WismUnity package status loaded.", new
            {
                packages = packages.Select(PackageStatus).ToArray(),
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        [McpTool("WismUnity.GetSceneSummary", "Returns a read-only summary of the active scene hierarchy and WISM manager components.", Groups = new[] { Group, "scene" }, EnabledByDefault = true)]
        public static object GetSceneSummary()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return Response.Success("WismUnity scene is not loaded.", new
                {
                    activeScene = SceneInfo(scene),
                    rootGameObjectCount = 0,
                    sceneGameObjectCount = 0,
                    managerCount = 0,
                    managers = Array.Empty<object>()
                });
            }

            var roots = scene.GetRootGameObjects();
            var sceneObjects = roots.SelectMany(Flatten).ToArray();
            var managers = sceneObjects
                .SelectMany(go => go.GetComponents<MonoBehaviour>()
                    .Where(component => component != null && component.GetType().Name.Contains("Manager"))
                    .Select(component => new
                    {
                        gameObject = HierarchyPath(component.gameObject),
                        type = component.GetType().FullName,
                        enabled = component.enabled
                    }))
                .OrderBy(manager => manager.type)
                .ThenBy(manager => manager.gameObject)
                .ToArray();

            return Response.Success("WismUnity scene summary loaded.", new
            {
                activeScene = SceneInfo(scene),
                rootGameObjectCount = roots.Length,
                sceneGameObjectCount = sceneObjects.Length,
                managerCount = managers.Length,
                managers
            });
        }

        [McpTool("WismUnity.GetConsoleSummary", "Returns Unity console counts without clearing or modifying console messages.", Groups = new[] { Group, "debug" }, EnabledByDefault = true)]
        public static object GetConsoleSummary()
        {
            try
            {
                var logEntriesType = Type.GetType("UnityEditor.LogEntries,UnityEditor");
                if (logEntriesType == null)
                    return Response.Error("CONSOLE_SUMMARY_UNAVAILABLE", new { reason = "UnityEditor.LogEntries type was not found." });

                var getCountsMethod = logEntriesType.GetMethod("GetCountsByType", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                if (getCountsMethod != null)
                {
                    var parameters = new object[] { 0, 0, 0 };
                    getCountsMethod.Invoke(null, parameters);
                    return Response.Success("Unity console summary loaded.", new
                    {
                        available = true,
                        errors = (int)parameters[0],
                        warnings = (int)parameters[1],
                        logs = (int)parameters[2],
                        timestampUtc = DateTime.UtcNow.ToString("O")
                    });
                }

                var getCountMethod = logEntriesType.GetMethod("GetCount", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var total = getCountMethod != null ? (int)getCountMethod.Invoke(null, null) : -1;
                return Response.Success("Unity console total count loaded.", new
                {
                    available = true,
                    errors = -1,
                    warnings = -1,
                    logs = -1,
                    totalEntries = total,
                    note = "Unity console per-type counts were unavailable for this editor version.",
                    timestampUtc = DateTime.UtcNow.ToString("O")
                });
            }
            catch (Exception ex)
            {
                return Response.Error("CONSOLE_SUMMARY_FAILED", new { reason = ex.Message });
            }
        }

        [McpTool("WismUnity.GetGameViewMetadata", "Returns read-only game view and camera metadata useful for visual smoke tests.", Groups = new[] { Group, "visual" }, EnabledByDefault = true)]
        public static object GetGameViewMetadata()
        {
            var mainCamera = Camera.main;
            var allCameras = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)
                .Select(camera => new
                {
                    name = HierarchyPath(camera.gameObject),
                    enabled = camera.enabled,
                    tag = camera.tag,
                    targetTexture = camera.targetTexture != null ? camera.targetTexture.name : null,
                    orthographic = camera.orthographic,
                    orthographicSize = camera.orthographic ? camera.orthographicSize : 0f,
                    fieldOfView = camera.orthographic ? 0f : camera.fieldOfView,
                    depth = camera.depth
                })
                .OrderBy(camera => camera.depth)
                .ThenBy(camera => camera.name)
                .ToArray();

            return Response.Success("WismUnity game view metadata loaded.", new
            {
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                gameViewAspect = Screen.height > 0 ? Math.Round((double)Screen.width / Screen.height, 4) : 0,
                mainCamera = mainCamera != null ? HierarchyPath(mainCamera.gameObject) : null,
                cameraCount = allCameras.Length,
                cameras = allCameras,
                timestampUtc = DateTime.UtcNow.ToString("O")
            });
        }

        static object PackageStatus(string packageName)
        {
            var package = UnityEditor.PackageManager.PackageInfo.FindForPackageName(packageName);
            if (package == null)
                return new { name = packageName, installed = false };

            return new
            {
                name = package.name,
                installed = true,
                version = package.version,
                source = package.source.ToString(),
                resolvedPath = ProjectRelativePackagePath(package.resolvedPath)
            };
        }

        static object SceneInfo(Scene scene)
        {
            return new
            {
                name = scene.name,
                path = scene.path,
                isValid = scene.IsValid(),
                isLoaded = scene.isLoaded,
                isDirty = scene.isDirty,
                buildIndex = scene.buildIndex
            };
        }

        static IEnumerable<GameObject> Flatten(GameObject root)
        {
            yield return root;
            foreach (Transform child in root.transform)
            {
                foreach (var nested in Flatten(child.gameObject))
                    yield return nested;
            }
        }

        static string HierarchyPath(GameObject gameObject)
        {
            var names = new Stack<string>();
            var current = gameObject.transform;
            while (current != null)
            {
                names.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", names);
        }

        static string ProjectRelativePackagePath(string resolvedPath)
        {
            if (string.IsNullOrEmpty(resolvedPath))
                return resolvedPath;

            var projectRoot = Directory.GetCurrentDirectory().Replace('\\', '/');
            var normalized = resolvedPath.Replace('\\', '/');
            return normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase)
                ? normalized.Substring(projectRoot.Length).TrimStart('/')
                : Path.GetFileName(normalized);
        }
    }
}
