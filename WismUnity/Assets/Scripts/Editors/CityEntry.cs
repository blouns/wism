using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editors
{
    public class CityEntry : MonoBehaviour
    {
        public string cityShortName;

        public Vector2Int GetGameCoordinates()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            var coords = worldTilemap.ConvertUnityToGameCoordinates(gameObject.transform.position);

            // Center on top-left of city
            return new Vector2Int(coords.Item1, coords.Item2 + 1); 
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
