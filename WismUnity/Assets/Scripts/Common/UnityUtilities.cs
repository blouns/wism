using UnityEngine;

public static class UnityUtilities
{
    static public GameObject GameObjectHardFind(string str)
    {
        GameObject result = null;
        foreach (GameObject root in GameObject.FindObjectsOfType(typeof(GameObject)))
        {
            if (root.transform.parent == null)
            { 
                // Root GameObject
                result = GameObjectHardFind(root, str, 0);
                if (result != null) break;
            }
        }
        return result;
    }
    static public GameObject GameObjectHardFind(string str, string tag)
    {
        GameObject result = null;
        foreach (GameObject parent in GameObject.FindGameObjectsWithTag(tag))
        {
            result = GameObjectHardFind(parent, str, 0);
            if (result != null) break;
        }
        return result;
    }
    static private GameObject GameObjectHardFind(GameObject item, string str, int index)
    {
        if (index == 0 && item.name == str) return item;
        if (index < item.transform.childCount)
        {
            GameObject result = GameObjectHardFind(item.transform.GetChild(index).gameObject, str, 0);
            if (result == null)
            {
                return GameObjectHardFind(item, str, ++index);
            }
            else
            {
                return result;
            }
        }
        return null;
    }
}
