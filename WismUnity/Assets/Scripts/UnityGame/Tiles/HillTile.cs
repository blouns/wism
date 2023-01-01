using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Tiles
{
    public class HillTile : Tile
    {
        [SerializeField]
        private Sprite[] hillSprites;

        [SerializeField]
        private Sprite preview;

        private const int HillTileDefault = 4;
        private HasTile hasTile = HasHill;

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            TileUtility.RefreshTile(position, tilemap, this.hasTile);
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

            tileData.sprite = this.hillSprites[HillTileDefault];

            // 0) Hill bottom-left
            if (composition[1] == 'E' && composition[7] == 'H')
            {
                tileData.sprite = this.hillSprites[0];
            }
            // 1) Hill bottom-right
            else if (composition[1] == 'H' && composition[7] == 'E')
            {
                tileData.sprite = this.hillSprites[1];
            }
            // 2) Hill middle
            else if (composition[1] == 'H' && composition[7] == 'H')
            {
                tileData.sprite = this.hillSprites[2];
            }
            // 3) Hill top-left
            else if (composition[3] == 'H' && composition[7] == 'H')
            {
                tileData.sprite = this.hillSprites[3];
            }
            // 4) Hill top-right
            else if (composition[1] == 'H' && composition[3] == 'H')
            {
                tileData.sprite = this.hillSprites[4];
            }
        }

        private static bool HasHill(ITilemap tilemap, Vector3Int position)
        {
            return ((tilemap.GetTile(position) is HillTile) ||
                    (tilemap.GetTile(position) is MountainTile));
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
}