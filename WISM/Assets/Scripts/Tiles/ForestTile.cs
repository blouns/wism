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

    private const int ForestTileDefault = 13;
    private const int MiddleForestTile = 5;

    private static List<QuadNode> ForestKinds = null;

    public override void RefreshTile(Vector3Int position, ITilemap tilemap)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int adjacentPosition = new Vector3Int(position.x + x, position.y + y, position.z);

                if (HasForest(tilemap, adjacentPosition))
                {
                    tilemap.RefreshTile(adjacentPosition);
                }
            }
        }

        tilemap.RefreshTile(position);
    }

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        QuadNode overlappingForests = FindOverlappingForests(position, tilemap);
        List<QuadNode> forestKinds = GetForestKinds();

        // Lookup the forest kind that matches the pattern of adjacent forests
        int index = forestKinds.FindIndex(x => x.Equals(overlappingForests));
        if (index < 0)
        {
            tileData.sprite = forestSprites[ForestTileDefault];
        }
        else
        {
            tileData.sprite = forestSprites[index];
        }

        Debug.Log(String.Format("Id: {0}, Overlap: ({1}, {2}, {3}, {4})",
            index, overlappingForests.TopLeft, overlappingForests.TopRight, overlappingForests.BottomLeft, overlappingForests.BottomRight));
    }

    private QuadNode FindOverlappingForests(Vector3Int position, ITilemap tilemap)
    {
        Vector3Int[] grid = TileUtility.GetNeighbors(position);

        QuadNode adjacentForests = new QuadNode();
        adjacentForests.TopLeft =
            HasForest(tilemap, grid[1]) &&
            HasForest(tilemap, grid[2]) &&
            HasForest(tilemap, grid[5]);

        adjacentForests.TopRight =
            HasForest(tilemap, grid[5]) &&
            HasForest(tilemap, grid[7]) &&
            HasForest(tilemap, grid[8]);

        adjacentForests.BottomLeft =
            HasForest(tilemap, grid[0]) &&
            HasForest(tilemap, grid[1]) &&
            HasForest(tilemap, grid[3]);

        adjacentForests.BottomRight =
            HasForest(tilemap, grid[3]) &&
            HasForest(tilemap, grid[6]) &&
            HasForest(tilemap, grid[7]);

        return adjacentForests;
    }

    private List<QuadNode> GetForestKinds()
    {
        // Singleton
        if (ForestKinds == null)
        {
            ForestKinds = BuildForestKinds();
        }

        return ForestKinds;
    }

    /// <summary>
    /// Build the forest kinds to map the corners which have adjacent forests
    /// to the sprite number.
    /// </summary>
    /// <returns></returns>
    private List<QuadNode> BuildForestKinds()
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
        kinds.Add(new QuadNode(false, false, false, false));    // 13 Single tile        

        return kinds;
    }

    private bool HasForest(ITilemap tilemap, Vector3Int position)
    {
        return (tilemap.GetTile(position) == this);
    }

    private bool HasBridge(ITilemap tilemap, Vector3Int position)
    {
        return tilemap.GetTile(position) is BridgeTile;
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