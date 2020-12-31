using Assets.Scripts;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterTile : Tile
{
    [SerializeField]
    private Sprite[] sprites;

    [SerializeField]
    private Sprite preview;

    private const int DefaultTileIndex = 6;
    public HasTile hasTile = HasTile;

    public static bool HasTile(ITilemap tilemap, Vector3Int position)
    {
        return ((tilemap.GetTile(position) is WaterTile) || 
                (tilemap.GetTile(position) is BridgeTile));
    }

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        TileUtility.RefreshTile(position, tilemap, hasTile);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        int index = TileUtility.FindOverlapping14SpriteIndex(position, tilemap, hasTile, DefaultTileIndex);
        tileData.sprite = sprites[index];
    }  

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/WaterTile")]
    public static void CreateWaterTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Water Tile", "New Water Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<WaterTile>(), path);
    }

#endif
}
