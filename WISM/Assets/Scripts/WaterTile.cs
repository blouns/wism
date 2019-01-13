using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterTile : Tile
{
    [SerializeField]
    private Sprite[] waterSprites;

    [SerializeField]
    private Sprite preview;

    private const int WaterTileDefault = 6;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                if (HasWater(tilemap, adjacentPosition))
                {
                    tilemap.RefreshTile(adjacentPosition);
                }
            }
        }

        tilemap.RefreshTile(position);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        string composition = string.Empty;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int vector = new Vector3Int(position.x + x, position.y + y, position.z);
                if (HasWater(tilemap, vector) || HasBridge(tilemap, vector))
                {
                    composition += 'W';
                }
                else
                {
                    composition += 'E';
                }
            }
        }

        Debug.Log("Composition: " + composition);
        tileData.sprite = waterSprites[WaterTileDefault];

        // 0) Bottom-left
        if (composition[1] == 'E' && composition[3] == 'E' && composition[5] == 'W' && composition[7] == 'W')
        {
            tileData.sprite = waterSprites[0];
        }
        // 1) Bottom-left inside corner
        else if (
            composition[0] == 'E' && composition[1] == 'W' && composition[2] == 'W' && composition[3] == 'W' &&
            composition[5] == 'W' && composition[6] == 'W' && composition[7] == 'W' && composition[8] == 'W')
        {
            tileData.sprite = waterSprites[1];
        }
        // 2) Bottom-middle
        else if (composition[1] == 'W' && composition[3] == 'E' && composition[5] == 'W' && composition[7] == 'W')
        {
            tileData.sprite = waterSprites[2];
        }
        // 3) Bottom-right
        else if (composition[1] == 'W' && composition[3] == 'E' && composition[5] == 'W' && composition[7] == 'E')
        {
            tileData.sprite = waterSprites[3];
        }
        // 4) Bottom-right inside corner
        else if (
            composition[0] == 'W' && composition[1] == 'W' && composition[2] == 'W' && composition[3] == 'W' &&
            composition[5] == 'W' && composition[6] == 'E' && composition[7] == 'W' && composition[8] == 'W')
        {
            tileData.sprite = waterSprites[4];
        }
        // 5) Left-middle
        else if (composition[1] == 'E' && composition[3] == 'W' && composition[5] == 'W' && composition[7] == 'W')
        {
            tileData.sprite = waterSprites[5];
        }
        // 6) Middle
        else if (
            composition[0] == 'W' && composition[1] == 'W' && composition[2] == 'W' && composition[3] == 'W' &&
            composition[5] == 'W' && composition[6] == 'W' && composition[7] == 'W' && composition[8] == 'W')
        {
            tileData.sprite = waterSprites[6];
        }
        // 7) Right-middle
        else if (composition[1] == 'W' && composition[3] == 'W' && composition[5] == 'W' && composition[7] == 'E')
        {
            tileData.sprite = waterSprites[7];
        }
        // 8) Top-left
        else if (composition[1] == 'E' && composition[3] == 'W' && composition[5] == 'E' && composition[7] == 'W')
        {
            tileData.sprite = waterSprites[8];
        }
        // 9) Top-left inside corner
        else if (
            composition[0] == 'W' && composition[1] == 'W' && composition[2] == 'E' && composition[3] == 'W' &&
            composition[5] == 'W' && composition[6] == 'W' && composition[7] == 'W' && composition[8] == 'W')
        {
            tileData.sprite = waterSprites[9];
        }
        // 10) Top-right
        else if (composition[1] == 'W' && composition[3] == 'W' && composition[5] == 'E' && composition[7] == 'E')
        {
            tileData.sprite = waterSprites[10];
        }
        // 11) Top-right inside corner
        else if (
            composition[0] == 'W' && composition[1] == 'W' && composition[2] == 'W' && composition[3] == 'W' &&
            composition[5] == 'W' && composition[6] == 'W' && composition[7] == 'W' && composition[8] == 'E')
        {
            tileData.sprite = waterSprites[11];
        }
        // 12) Top-middle
        else if (composition[1] == 'W' && composition[3] == 'W' && composition[5] == 'E' && composition[7] == 'W')
        {
            tileData.sprite = waterSprites[12];
        }        
    }

    private bool HasWater(ITilemap tilemap, Vector3Int position)
    {
        return (tilemap.GetTile(position) == this);
    }

    private bool HasBridge(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) is BridgeTile;
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
