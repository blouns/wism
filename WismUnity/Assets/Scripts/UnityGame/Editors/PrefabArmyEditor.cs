namespace Assets.Scripts.Editors
{

    /// <summary>
    /// This is a [failed] prototype. It works except the sprite image is dropped once the prefab is saved.
    /// This appears to be due to the fact that it doesn't have the path mapping info back to the
    /// sprite image. I cannot see how this is set unfortunately, so parking this code for now.
    /// </summary>
#if UNITY_EDITOR
    //public class PrefabArmyEditor
    //{
    //    private static int index;

    //    [MenuItem("Examples/Instantiate Army Prefab")]
    //    public static void InstantiatePrefab()
    //    {
    //        const float defaultPixelsPerUnit = 40f;

    //        // Create the sprite from the loaded texture
    //        var prefabOrig = (GameObject)AssetDatabase.LoadMainAssetAtPath("Assets/Prefab/Armies/Elvallie/Elvallie_Cavalry.prefab");
    //        var goOrig = PrefabUtility.InstantiatePrefab(prefabOrig);
    //        var instanceOrig = PrefabUtility.SaveAsPrefabAsset((GameObject)goOrig, "Assets/Prefab/Armies/Sirians/Elvallie_Cavalry_orig.prefab", out bool success);

    //        var myTexture2D = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sprites/Armies/Sirians/sirians_cavalry.png");
    //        myTexture2D.filterMode = FilterMode.Point;
    //        //myTexture2D.Compress(false);
    //        var sprite = Sprite.Create(myTexture2D,
    //            new Rect(0.0f, 0.0f, myTexture2D.width, myTexture2D.height),
    //            new Vector2(0.5f, 0.5f), // pivot
    //            defaultPixelsPerUnit,
    //            1,  // extrude edges 
    //            SpriteMeshType.Tight);
    //        sprite.name = "sirians_cavalry.png";

    //        // Create the GameObject for the prefab
    //        var rootGO = new GameObject();
    //        rootGO.name = "Sirians_Cavalry" + ++index;
    //        rootGO.AddComponent<SpriteRenderer>();
    //        var sr = rootGO.GetComponent<SpriteRenderer>();
    //        sr.sprite = sprite;
    //        sr.drawMode = SpriteDrawMode.Simple;
    //        sr.color = Color.white;
    //        sr.spriteSortPoint = SpriteSortPoint.Center;

    //        // Create the prefab
    //        var instanceRoot = PrefabUtility.SaveAsPrefabAsset(rootGO, "Assets/Prefab/Armies/Sirians/Sirians_Cavalry.prefab", out success);

    //        if (success)
    //        {
    //            var newGO = (GameObject)PrefabUtility.InstantiatePrefab(instanceRoot);

    //            // Add the prefab variant to Assets
    //            var variantRoot = PrefabUtility.SaveAsPrefabAsset((GameObject)newGO, "Assets/Prefab/Armies/Sirians/Sirians_Cavalry_Variant.prefab");
    //        }
    //    }
    //}
#endif
}
