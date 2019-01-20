using Assets.Scripts;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class VolcanoTile : Tile
{       
    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        HasTile hasTile = MountainTile.HasTile;
        TileUtility.RefreshTile(position, tilemap, hasTile);
    }

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/VolcanoTile")]
    public static void CreateVolcanoTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Volcano Tile", "New Volcano Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<VolcanoTile>(), path);
    }

#endif
}
