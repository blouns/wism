using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class RoadTile : Tile
{
    [SerializeField]
    private Sprite[] roadSprites;

    [SerializeField]
    private Sprite preview;

    private const int RoadTileDefault = 1;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                if (HasRoad(tilemap, adjacentPosition))
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
                if (HasRoad(tilemap, vector) || HasBridge(tilemap, vector))
                {
                    composition += 'R';
                }
                else
                {
                    composition += 'E';
                }
            }
        }
        
        tileData.sprite = roadSprites[RoadTileDefault];

        // 0) East end
        if (composition[1] == 'R' && composition[3] == 'E' && composition[5] == 'E' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[0];
        }
        // 1) East-west
        else if (composition[1] == 'R' && composition[3] == 'E' && composition[5] == 'E' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[1];
        }       
        // 2) North end
        else if (composition[1] == 'E' && composition[3] == 'R' && composition[5] == 'E' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[2];
        }
        // 3) North-east corner
        else if (composition[1] == 'E' && composition[3] == 'E' && composition[5] == 'R' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[3];
        }
        // 4) North-south
        else if (composition[1] == 'E' && composition[3] == 'R' && composition[5] == 'R' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[4];
        }        
        // 5) North-west corner
        else if (composition[1] == 'R' && composition[3] == 'E' && composition[5] == 'R' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[5];
        }
        // 6) South end
        else if (composition[1] == 'E' && composition[3] == 'E' && composition[5] == 'R' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[6];
        }
        // 7) South-east corner
        else if (composition[1] == 'E' && composition[3] == 'R' && composition[5] == 'E' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[7];
        }
        // 8) South-west corner
        else if (composition[1] == 'R' && composition[3] == 'R' && composition[5] == 'E' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[8];
        }
        // 9) T north-east-south
        else if (composition[1] == 'E' && composition[3] == 'R' && composition[5] == 'R' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[9];
        }
        // 10) T north-east-south-west
        else if (composition[1] == 'R' && composition[3] == 'R' && composition[5] == 'R' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[10];
        }
        // 11) T west-north-east
        else if (composition[1] == 'R' && composition[3] == 'E' && composition[5] == 'R' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[11];
        }
        // 12) T west-north-south
        else if (composition[1] == 'R' && composition[3] == 'R' && composition[5] == 'R' && composition[7] == 'E')
        {
            tileData.sprite = roadSprites[12];
        }
        // 13) T west-south-east
        else if (composition[1] == 'R' && composition[3] == 'R' && composition[5] == 'E' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[13];
        }
        // 14) West end
        else if (composition[1] == 'E' && composition[3] == 'E' && composition[5] == 'E' && composition[7] == 'R')
        {
            tileData.sprite = roadSprites[14];
        }        

        //Debug.Log("Composition: " + composition);
    }

    private bool HasRoad(ITilemap tilemap, Vector3Int position)
    {
        return (tilemap.GetTile(position) == this);
    }

    private bool HasBridge(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) is BridgeTile;
    }

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/RoadTile")]
    public static void CreateRoadTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Road Tile", "New Road Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<RoadTile>(), path);
    }

#endif
}
