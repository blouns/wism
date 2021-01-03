using System;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
#endif

namespace Assets.Scripts.Tiles
{
    [Serializable]
    [CreateAssetMenu(fileName = "New Switchable Tile", menuName = "Tiles/Switchable Tile")]
    public class SwitchableTile : Tile
    {
        public Sprite[] sprites;
        public Tile.ColliderType m_TileColliderType;

        private int spriteIndex = 0;

        public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
        {
            tileData.transform = Matrix4x4.identity;
            tileData.color = Color.white;
            if (sprites != null && sprites.Length > 0)
            {
                tileData.sprite = sprites[spriteIndex];
                tileData.colliderType = m_TileColliderType;
            }
        }

        public override void RefreshTile(Vector3Int position, ITilemap tilemap)
        {
            tilemap.RefreshTile(position);
        }

        public Sprite GetNextSprite()
        {
            spriteIndex += 1;
            spriteIndex = spriteIndex % sprites.Length;
            return sprites[spriteIndex];
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(SwitchableTile))]
    public class SwitchableTileEditor : Editor
    {
        private SwitchableTile tile { get { return (target as SwitchableTile); } }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            int count = EditorGUILayout.DelayedIntField("Number of Switchable Sprites", tile.sprites != null ? tile.sprites.Length : 0);
            if (count < 0)
                count = 0;

            if (tile.sprites == null || tile.sprites.Length != count)
            {
                Array.Resize<Sprite>(ref tile.sprites, count);
            }

            if (count == 0)
                return;

            EditorGUILayout.LabelField("Place sprites shown based on the order of switching.");
            EditorGUILayout.Space();

            for (int i = 0; i < count; i++)
            {
                tile.sprites[i] = (Sprite)EditorGUILayout.ObjectField("Sprite " + (i + 1), tile.sprites[i], typeof(Sprite), false, null);
            }

            tile.m_TileColliderType = (Tile.ColliderType)EditorGUILayout.EnumPopup("Collider Type", tile.m_TileColliderType);
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(tile);
        }
    }
#endif
}