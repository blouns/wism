using Assets.Scripts.Tilemaps;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Editors
{
    public class LocationEntry : MonoBehaviour
    {
        public string locationShortName;

        public Vector2Int GetGameCoordinates()
        {
            var worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
                .GetComponent<WorldTilemap>();

            var coords = worldTilemap.ConvertUnityToGameVector(gameObject.transform.position);
            return new Vector2Int(coords.x, coords.y + 1); 
        }

#if UNITY_EDITOR

        [MenuItem("Assets/Create/Location")]
        public static void CreateLocation()
        {
            var locationContainer = UnityUtilities.GameObjectHardFind("Locations");
            var locationGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            DestroyImmediate(locationGO.GetComponent<MeshRenderer>());
            DestroyImmediate(locationGO.GetComponent<BoxCollider>());
            locationGO.AddComponent<LocationEntry>();

            locationGO.transform.localScale = new Vector3(2f, 2f, 1f);
            locationGO.transform.parent = locationContainer.transform;
            locationGO.name = "Location";
        }

#endif
    }
}
