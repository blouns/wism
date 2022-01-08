using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Core;

public class ArmyFlagSize : MonoBehaviour
{
    private WorldTilemap worldTilemap;
    private FlagManager flagManager;

    public void Awake()
    {
        this.worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
            .GetComponent<WorldTilemap>();
        this.flagManager = GameObject.FindGameObjectWithTag("UnityManager")
            .GetComponent<FlagManager>();
    }

    public void UpdateFlagSize()
    {
        var gameCoords = worldTilemap.ConvertUnityToGameVector(gameObject.transform.position);
        var tile = World.Current.Map[gameCoords.x, gameCoords.y];

        int flagSize = tile.GetAllArmies().Count;
        Clan clan = (tile.HasVisitingArmies()) ? 
                        tile.VisitingArmies[0].Clan :
                        tile.Armies[0].Clan;

        UpdateFlagSprite(clan, flagSize);
    }

    private void UpdateFlagSprite(Clan clan, int flagSize)
    {
        var newFlagPrefab = flagManager.FindGameObjectKind(clan, flagSize);
        var newFlagSpriteRenderer = newFlagPrefab.GetComponent<SpriteRenderer>();
        gameObject.GetComponent<SpriteRenderer>().sprite = newFlagSpriteRenderer.sprite;
    }
}
