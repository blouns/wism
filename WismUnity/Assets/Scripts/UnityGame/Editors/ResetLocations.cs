using Assets.Scripts.Editors;
using UnityEditor;

#if UNITY_EDITOR

[CustomEditor(typeof(LocationContainer))]
public class ResetLocations : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var container = target as LocationContainer;
        if (container != null)
        {
            if (container.Reset == true)

            {
                ClearLocations(container);
                container.Reset = false;
            }
        }        
    }

    private void ClearLocations(LocationContainer locations)
    {        
        for (int i = locations.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(locations.transform.GetChild(i).gameObject);
        }
    }
}

#endif