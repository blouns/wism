using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Assets.Scripts.Editors
{
    public class CityEntry : MonoBehaviour
    {
        public string cityShortName;

        private void Awake()
        {
            HideRuntimeMarkerSprite();
        }

        private void OnEnable()
        {
            HideRuntimeMarkerSprite();
        }

        public Vector2Int GetGameCoordinates()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            var map = worldTilemap.GetComponent<Tilemap>();
            var local = map.transform.InverseTransformPoint(transform.position);
            var intersection = map.LocalToCellInterpolated(local);

            // Authoring markers sit at the center intersection of a 2x2 city,
            // sometimes with tiny placement offsets. Unlike pointer input, they
            // snap to an intersection rather than floor to the containing cell.
            return new Vector2Int(
                Mathf.RoundToInt(intersection.x) - 1 - map.cellBounds.xMin,
                Mathf.RoundToInt(intersection.y) - map.cellBounds.yMin);
        }

        private void HideRuntimeMarkerSprite()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            var spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
        }

#if UNITY_EDITOR

        [MenuItem("Assets/Create/City")]
        public static void CreateCity()
        {
            var cityContainer = UnityUtilities.GameObjectHardFind("Cities");
            var cityGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(cityGO.GetComponent<MeshRenderer>());
            DestroyImmediate(cityGO.GetComponent<BoxCollider>());
            cityGO.AddComponent<CityEntry>();

            cityGO.transform.localScale = new Vector3(2f, 2f, 1f);
            cityGO.transform.parent = cityContainer.transform;
            cityGO.name = "City";
        }

#endif
    }
}
