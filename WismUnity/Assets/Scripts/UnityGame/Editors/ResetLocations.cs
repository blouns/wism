using Assets.Scripts.Editors;
using Assets.Scripts.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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