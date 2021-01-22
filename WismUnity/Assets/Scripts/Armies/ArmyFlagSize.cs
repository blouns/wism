using Assets.Scripts.Managers;
using Assets.Scripts.Tilemaps;
using UnityEngine;
using Wism.Client.Core;

public class ArmyFlagSize : MonoBehaviour
{
    private WorldTilemap worldTilemap;
    private FlagManager flagManager;

    public void Start()
    {
        this.worldTilemap = GameObject.FindGameObjectWithTag("WorldTilemap")
            .GetComponent<WorldTilemap>();

        this.flagManager = GameObject.FindGameObjectWithTag("ArmyManager")
            .GetComponent<FlagManager>();
    }

    public void UpdateFlagSize()
    {
        var gameCoords = worldTilemap.ConvertUnityToGameCoordinates(gameObject.transform.position);
        var tile = World.Current.Map[gameCoords.Item1, gameCoords.Item2];

        int flagSize;
        Clan clan;
        if (tile.HasVisitingArmies())
        {
            // Draw moving armies first
            flagSize = tile.VisitingArmies.Count;
            clan = tile.VisitingArmies[0].Clan;
        }
        else
        {
            flagSize = tile.Armies.Count;
            clan = tile.Armies[0].Clan;
        }

        UpdateFlagSprite(clan, flagSize);
    }

    private void UpdateFlagSprite(Clan clan, int flagSize)
    {
        var newFlagPrefab = flagManager.FindGameObjectKind(clan, flagSize);
        var newFlagSpriteRenderer = newFlagPrefab.GetComponent<SpriteRenderer>();
        gameObject.GetComponent<SpriteRenderer>().sprite = newFlagSpriteRenderer.sprite;
    }
}
