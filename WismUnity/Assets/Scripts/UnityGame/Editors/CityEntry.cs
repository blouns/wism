using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;

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

            var coords = worldTilemap.ConvertUnityToGameVector(this.gameObject.transform.position);

            // City markers are centered on the 2x2 footprint. The generic
            // tile conversion reports the cell immediately below that anchor;
            // move back to the top-left tile used by WismClient cities.
            return new Vector2Int(coords.x, coords.y + 1);
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
