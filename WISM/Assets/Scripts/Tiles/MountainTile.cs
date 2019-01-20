using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Assets.Scripts;

public class MountainTile : Tile
{
    [SerializeField]
    private Sprite[] mountainSprites;

    [SerializeField]
    private Sprite preview;

    private const int DefaultTileIndex = 0;

    private static List<AdjacencyMap> TileKinds = null;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        TileUtility.RefreshTile<MountainTile>(position, tilemap);
    }
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        AdjacencyMap overlappingTiles = TileUtility.FindOverlappingTiles<MountainTile>(position, tilemap);
        List<AdjacencyMap> kinds = GetOverlappingKinds();
        int index = TileUtility.FindOverlappingTileIndex(kinds, overlappingTiles);
        tileData.sprite = (index < 0) ? mountainSprites[DefaultTileIndex] : mountainSprites[index];        
    }

    private List<AdjacencyMap> GetOverlappingKinds()
    {
        // Singleton
        if (TileKinds == null)
        {
            TileKinds = TileUtility.BuildOverlappingKinds();
        }

        return TileKinds;
    }

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/MountainTile")]
    public static void CreateMountainTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Mountain Tile", "New Mountain Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<MountainTile>(), path);
    }

#endif
}