using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TerrainTilemap : MonoBehaviour
{    
    void Start()
    {
        int maxSize = 10000;
        Tilemap tilemap = GetComponent<Tilemap>();
        tilemap.size = new Vector3Int(Mathf.Min(tilemap.size.x, maxSize), Mathf.Min(tilemap.size.y, maxSize), 1);
        tilemap.ResizeBounds();
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;
        TileBase[] allTiles = tilemap.GetTilesBlock(bounds);
        for (int x = 0; x < bounds.size.x; x++) {
            for (int y = 0; y < bounds.size.y; y++) {                
                TileBase tile = allTiles[x + y * bounds.size.x];
                if (tile != null) {
                    Debug.Log("x:" + x + " y:" + y + " tile:" + tile.name);
                    
                } else {
                    Debug.Log("x:" + x + " y:" + y + " tile: (null)");
                }

                if (maxSize-- < 0)
                {
                    Debug.Log("Broke out of large tilemap enumeration.");
                    return;
                }
            }
        }        
    }  

}
