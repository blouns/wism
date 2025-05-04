using UnityEngine;

public static class UnityUtilities
{
    // Public method to find by name across all root objects
    public static GameObject GameObjectHardFind(string str, int maxDepth = 10)
    {
        foreach (GameObject root in GameObject.FindObjectsOfType<GameObject>())
        {
            if (root.transform.parent == null) // Only check root objects
            {
                GameObject result = FindInChildren(root, str, 0, maxDepth);
                if (result != null) return result;
            }
        }
        Debug.LogWarning($"GameObject '{str}' not found in scene.");
        return null;
    }

    // Public method to find by name under a specific tag
    public static GameObject GameObjectHardFind(string str, string tag, int maxDepth = 10)
    {
        GameObject[] parents = GameObject.FindGameObjectsWithTag(tag);
        foreach (GameObject parent in parents)
        {
            GameObject result = FindInChildren(parent, str, 0, maxDepth);
            if (result != null) return result;
        }
        Debug.LogWarning($"GameObject '{str}' not found under tag '{tag}'.");
        return null;
    }

    // Recursive search with null safety and depth limit
    private static GameObject FindInChildren(GameObject parent, string targetName, int depth, int maxDepth)
    {
        if (parent == null || string.IsNullOrEmpty(targetName) || depth > maxDepth)
            return null;

        // Check current object
        if (parent.name == targetName)
            return parent;

        // Check children
        Transform transform = parent.transform;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (child == null) continue; // Skip destroyed/null children

            GameObject result = FindInChildren(child.gameObject, targetName, depth + 1, maxDepth);
            if (result != null) return result;
        }

        return null; // Not found
    }
}