using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class HillTile : Tile
{
    [SerializeField]
    private Sprite[] hillSprites;

    [SerializeField]
    private Sprite preview;

    private const int HillTileDefault = 4;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                if (HasHill(tilemap, adjacentPosition))
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
                if (HasHill(tilemap, vector))
                {
                    composition += 'H';
                }                
                else
                {
                    composition += 'E';
                }
            }
        }

        tileData.sprite = hillSprites[HillTileDefault];

        // 0) Hill bottom-left
        if (composition[1] == 'E' && composition[7] == 'H')
        {
            tileData.sprite = hillSprites[0];
        }
        // 1) Hill bottom-right
        else if (composition[1] == 'H' && composition[7] == 'E')
        {
            tileData.sprite = hillSprites[1];
        }
        // 2) Hill middle
        else if (composition[1] == 'H' && composition[7] == 'H')
        {
            tileData.sprite = hillSprites[2];
        }
        // 3) Hill top-left
        else if (composition[3] == 'H' && composition[7] == 'H')
        {
            tileData.sprite = hillSprites[3];
        }
        // 4) Hill top-right
        else if (composition[1] == 'H' && composition[3] == 'H')
        {
            tileData.sprite = hillSprites[4];
        }

        //Debug.Log("Composition: " + composition);
    }

    private bool HasHill(ITilemap tilemap, Vector3Int position)
    {
        return (tilemap.GetTile(position) == this);
    }    

#if UNITY_EDITOR
    // Add tile type into Unity Editor

    [MenuItem("Assets/Create/Tiles/HillTile")]
    public static void CreateHillTile()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save Hill Tile", "New Hill Tile", "asset", "Assets");
        if (string.IsNullOrEmpty(path))
            return;

        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<HillTile>(), path);
    }

#endif

}
