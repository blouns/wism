using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
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

                    if (HasRoad(tilemap, adjacentPosition) || HasBridge(tilemap, adjacentPosition))
                    {
                        tilemap.RefreshTile(adjacentPosition);
                    }
                }
            }

            base.RefreshTile(position, tilemap);
        }

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            bool west = IsRoadLike(tilemap, new Vector3Int(position.x - 1, position.y, position.z));
            bool east = IsRoadLike(tilemap, new Vector3Int(position.x + 1, position.y, position.z));
            bool south = IsRoadLike(tilemap, new Vector3Int(position.x, position.y - 1, position.z));
            bool north = IsRoadLike(tilemap, new Vector3Int(position.x, position.y + 1, position.z));

            tileData.sprite = this.roadSprites[RoadTileDefault];

            // 0) East end
            if (!west && east && !south && !north)
            {
                tileData.sprite = this.roadSprites[14];
            }
            // 1) East-west
            else if (west && east && !south && !north)
            {
                tileData.sprite = this.roadSprites[1];
            }
            // 2) North end
            else if (!west && !east && !south && north)
            {
                tileData.sprite = this.roadSprites[6];
            }
            // 3) North-east corner
            else if (!west && east && !south && north)
            {
                tileData.sprite = this.roadSprites[3];
            }
            // 4) North-south
            else if (!west && !east && south && north)
            {
                tileData.sprite = this.roadSprites[4];
            }
            // 5) North-west corner
            else if (west && !east && !south && north)
            {
                tileData.sprite = this.roadSprites[5];
            }
            // 6) South end
            else if (!west && !east && south && !north)
            {
                tileData.sprite = this.roadSprites[2];
            }
            // 7) South-east corner
            else if (!west && east && south && !north)
            {
                tileData.sprite = this.roadSprites[7];
            }
            // 8) South-west corner
            else if (west && !east && south && !north)
            {
                tileData.sprite = this.roadSprites[8];
            }
            // 9) T north-east-south
            else if (!west && east && south && north)
            {
                tileData.sprite = this.roadSprites[9];
            }
            // 10) T north-east-south-west
            else if (west && east && south && north)
            {
                tileData.sprite = this.roadSprites[10];
            }
            // 11) T west-north-east
            else if (west && east && !south && north)
            {
                tileData.sprite = this.roadSprites[11];
            }
            // 12) T west-north-south
            else if (west && !east && south && north)
            {
                tileData.sprite = this.roadSprites[12];
            }
            // 13) T west-south-east
            else if (west && east && south && !north)
            {
                tileData.sprite = this.roadSprites[13];
            }
            // 14) West end
            else if (west && !east && !south && !north)
            {
                tileData.sprite = this.roadSprites[0];
            }
        }

        private bool IsRoadLike(ITilemap tilemap, Vector3Int position)
        {
            return HasRoad(tilemap, position) || HasBridge(tilemap, position);
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
}
