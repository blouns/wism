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

    private const int MountainTileDefault = 0;
    private const int MiddleMountainTile = 5;

    private static List<QuadNode> MountainKinds = null;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                if (HasMountain(tilemap, adjacentPosition))
                {
                    tilemap.RefreshTile(adjacentPosition);
                }
            }
        }

        tilemap.RefreshTile(position);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        QuadNode overlappingMountains = FindOverlappingMountains(position, tilemap);
        List<QuadNode> mountainKinds = GetMountainKinds();
        
        // Lookup the mountain kind that matches the pattern of adjacent mountains
        int index = mountainKinds.FindIndex(x => x.Equals(overlappingMountains));
        if (index < 0)
        {
            tileData.sprite = mountainSprites[MountainTileDefault];
        }
        else
        {
            tileData.sprite = mountainSprites[index];
        }

        //Debug.Log(String.Format("Id: {0}, Overlap: ({1}, {2}, {3}, {4})",
        //    index, overlappingMountains.TopLeft, overlappingMountains.TopRight, overlappingMountains.BottomLeft, overlappingMountains.BottomRight));
    }

    private QuadNode FindOverlappingMountains(Vector3Int position, ITilemap tilemap)
    {
        Vector3Int[] grid = TileUtility.GetNeighbors(position);

        QuadNode adjacentMountains = new QuadNode();
        adjacentMountains.TopLeft =
            HasMountain(tilemap, grid[1]) &&
            HasMountain(tilemap, grid[2]) &&
            HasMountain(tilemap, grid[5]);

        adjacentMountains.TopRight =
            HasMountain(tilemap, grid[5]) &&
            HasMountain(tilemap, grid[7]) &&
            HasMountain(tilemap, grid[8]);

        adjacentMountains.BottomLeft =
            HasMountain(tilemap, grid[0]) &&
            HasMountain(tilemap, grid[1]) &&
            HasMountain(tilemap, grid[3]);

        adjacentMountains.BottomRight =
            HasMountain(tilemap, grid[3]) &&
            HasMountain(tilemap, grid[6]) &&
            HasMountain(tilemap, grid[7]);

        return adjacentMountains;
    }

    private List<QuadNode> GetMountainKinds()
    {
        // Singleton
        if (MountainKinds == null)
        {
            MountainKinds = BuildMountainKinds();
        }

        return MountainKinds;
    }

    /// <summary>
    /// Build the mountain kinds to map the corners which have adjacent mountains
    /// to the sprite number.
    /// </summary>
    /// <returns></returns>
    private List<QuadNode> BuildMountainKinds()
    {
        List<QuadNode> kinds = new List<QuadNode>();
        kinds.Add(new QuadNode(false, true, false, false)); // 0 Bottom-left
        kinds.Add(new QuadNode(true, true, false, true));   // 1 Bottom-left inside corner
        kinds.Add(new QuadNode(true, true, false, false));  // 2 Bottom-middle
        kinds.Add(new QuadNode(true, false, false, false)); // 3 Bottom-right
        kinds.Add(new QuadNode(true, true, true, false));   // 4 Bottom-right inside corner
        kinds.Add(new QuadNode(true, true, true, true));    // 5 Middle-middle
        kinds.Add(new QuadNode(true, false, true, false));  // 6 Right-middle
        kinds.Add(new QuadNode(false, false, false, true)); // 7 Top-left
        kinds.Add(new QuadNode(false, true, true, true));   // 8 Top-left inside corner
        kinds.Add(new QuadNode(false, false, true, true));  // 9 Top-middle
        kinds.Add(new QuadNode(false, false, true, false)); // 10 Top-right
        kinds.Add(new QuadNode(true, false, true, true));   // 11 Top-right inside corner
        kinds.Add(new QuadNode(false, true, false, true));  // 12 Left-middle

        // Special cases
        kinds.Add(new QuadNode(true, true, true, true));    // 13 Snow peak
        kinds.Add(new QuadNode(true, true, true, true));    // 14 Volcano

        return kinds;
    }

    private bool HasMountain(ITilemap tilemap, Vector3Int position)
    {
        return (tilemap.GetTile(position) == this);
    }

    private bool HasBridge(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) is BridgeTile;
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