using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BridgeTile : Tile
{
    [SerializeField]
    private Sprite[] bridgeSprites;

    [SerializeField]
    private Sprite preview;

    private const int BridgeTileDefault = 3;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                if (HasBridge(tilemap, adjacentPosition))
                {
                    tilemap.RefreshTile(adjacentPosition);
                }
            }
        }

        base.RefreshTile(position, tilemap);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        string composition = string.Empty;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int vector = new Vector3Int(position.x + x, position.y + y, position.z);
                if (HasBridge(tilemap, vector))
                {
                    composition += 'B';
                }
                else if (HasRoad(tilemap, vector))
                {
                    composition += 'R';
                }
                else if (HasWater(tilemap, vector))
                {
                    composition += 'W';
                }
                else
                {
                    composition += 'E';
                }
            }
        }

        tileData.sprite = bridgeSprites[BridgeTileDefault];
        
        // 0) Bridge east-west left 
        if (composition[1] == 'R' && composition[3] == 'W' && composition[5] == 'W' && composition[7] == 'B')
        {
            tileData.sprite = bridgeSprites[0];
        }
        // 1) Bridge north-south bottom
        else if (composition[1] == 'W' && composition[3] == 'R' && composition[5] == 'B' && composition[7] == 'W')
        {
            tileData.sprite = bridgeSprites[1];
        }
        // 2) Bridge north-south top
        else if (composition[1] == 'W' && composition[3] == 'B' && composition[5] == 'R' && composition[7] == 'W')
        {
            tileData.sprite = bridgeSprites[2];
        }
        // 3) Bridge east-west right        
        else if (composition[1] == 'B' && composition[3] == 'W' && composition[5] == 'W' && composition[7] == 'R')
        {
            tileData.sprite = bridgeSprites[3];
        }
    }

    private bool HasBridge(ITilemap tilemap, Vector3Int position)
    {
        return (tilemap.GetTile(position) == this);
    }

    private bool HasWater(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) is WaterTile;
    }

    private bool HasRoad(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) is RoadTile;
    }

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/BridgeTile")]
    public static void CreateBridgeTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Bridge Tile", "New Bridge Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<BridgeTile>(), path);
    }

#endif
}
