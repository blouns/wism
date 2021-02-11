using Assets.Scripts.Tiles;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CityContainer))]
public class ResetCities : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var container = target as CityContainer;
        if (container != null)
        {
            if (container.Reset == true)

            {
                ClearCities(container);
                container.Reset = false;
            }
        }        
    }

    private void ClearCities(CityContainer cities)
    {        
        for (int i = cities.transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(cities.transform.GetChild(i).gameObject);
        }
    }
}
