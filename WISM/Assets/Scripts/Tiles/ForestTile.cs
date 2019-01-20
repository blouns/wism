using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using Assets.Scripts;

public class ForestTile : Tile
{
    [SerializeField]
    private Sprite[] forestSprites;

    [SerializeField]
    private Sprite preview;

    private const int DefaultTileIndex = 13;

    private static List<AdjacencyMap> kinds = null;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        TileUtility.RefreshTile<ForestTile>(position, tilemap);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        AdjacencyMap overlappingTiles = TileUtility.FindOverlappingTiles<ForestTile>(position, tilemap);
        List<AdjacencyMap> kinds = GetOverlappingKinds();
        int index = TileUtility.FindOverlappingTileIndex(kinds, overlappingTiles);
        tileData.sprite = (index < 0) ? forestSprites[DefaultTileIndex] : forestSprites[index];
    }
   
    private List<AdjacencyMap> GetOverlappingKinds()
    {
        // Singleton
        if (kinds == null)
        {
            kinds = TileUtility.BuildOverlappingKinds();

            // Special cases
            kinds.Add(new AdjacencyMap(false, false, false, false));    // 13 Single tile        
        }

        return kinds;
    }    

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/ForestTile")]
    public static void CreateForestTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Forest Tile", "New Forest Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ForestTile>(), path);
    }

#endif
}