using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class ScenePrefabReferenceTests
{
    private const string ItemManagerTypeName = "Assets.Scripts.Managers.ItemManager";
    private const string CityManagerTypeName = "Assets.Scripts.Managers.CityManager";

    [Test]
    public void BuildScenes_ItemManagers_HaveRequiredPrefabsAssigned()
    {
        var failures = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled || string.IsNullOrWhiteSpace(scene.path))
            {
                continue;
            }

            Scene openedScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            foreach (GameObject root in openedScene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.GetType().FullName != ItemManagerTypeName)
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(behaviour);
                    AddMissingPrefabFailure(failures, scene.path, behaviour, serialized, "itemPrefab");
                    AddMissingPrefabFailure(failures, scene.path, behaviour, serialized, "companionPrefab");
                }
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    [Test]
    public void BuildScenes_CityManagers_HaveRuinsTileAssigned()
    {
        var failures = new List<string>();

        foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!scene.enabled || string.IsNullOrWhiteSpace(scene.path))
            {
                continue;
            }

            Scene openedScene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            foreach (GameObject root in openedScene.GetRootGameObjects())
            {
                foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null || behaviour.GetType().FullName != CityManagerTypeName)
                    {
                        continue;
                    }

                    var serialized = new SerializedObject(behaviour);
                    AddMissingPrefabFailure(failures, scene.path, behaviour, serialized, "ruinsTile");
                }
            }
        }

        Assert.That(failures, Is.Empty, string.Join("\n", failures));
    }

    private static void AddMissingPrefabFailure(
        List<string> failures,
        string scenePath,
        MonoBehaviour behaviour,
        SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
        {
            failures.Add($"{scenePath}: {GameObjectPath(behaviour.transform)} is missing serialized field {propertyName}.");
            return;
        }

        if (property.objectReferenceValue == null)
        {
            failures.Add($"{scenePath}: {GameObjectPath(behaviour.transform)} has unassigned {propertyName}.");
        }
    }

    private static string GameObjectPath(Transform transform)
    {
        var names = new Stack<string>();
        for (Transform current = transform; current != null; current = current.parent)
        {
            names.Push(current.name);
        }

        return string.Join("/", names);
    }
}
