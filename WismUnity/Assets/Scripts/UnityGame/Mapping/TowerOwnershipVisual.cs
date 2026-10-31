using UnityEngine;
using System.Linq;
using Wism.Client.Core;

namespace Assets.Scripts.UnityGame.Mapping
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class TowerOwnershipVisual : MonoBehaviour
    {
        [SerializeField] private Sprite neutralTower;
        [SerializeField] private Sprite siriansTower;
        [SerializeField] private Sprite stormGiantsTower;
        [SerializeField] private Sprite greyDwarvesTower;
        [SerializeField] private Sprite orcsOfKorTower;
        [SerializeField] private Sprite elvallieTower;
        [SerializeField] private Sprite selentinesTower;
        [SerializeField] private Sprite horseLordsTower;
        [SerializeField] private Sprite lordBaneTower;
        [SerializeField] private Sprite razedTower;

        private SpriteRenderer spriteRenderer;

        public string OwnerClanShortName { get; private set; } = "Neutral";

        public bool IsRazed { get; private set; }
        public bool RuntimeCreated { get; set; }

        public void Awake()
        {
            this.spriteRenderer = GetComponent<SpriteRenderer>();
            SetOwner(this.OwnerClanShortName);
        }

        public void SetOwner(string clanShortName)
        {
            this.IsRazed = false;

            this.OwnerClanShortName = string.IsNullOrWhiteSpace(clanShortName)
                ? "Neutral"
                : clanShortName;

            var sprite = ResolveTowerSprite(this.OwnerClanShortName) ?? this.neutralTower;
            if (sprite != null)
            {
                GetRenderer().sprite = sprite;
            }
            GetRenderer().color = Color.white;
            if (sprite == this.neutralTower && this.OwnerClanShortName != "Neutral" && Game.IsInitialized())
            {
                var clan = Game.Current.Players.FirstOrDefault(player => player.Clan.ShortName == this.OwnerClanShortName)?.Clan;
                if (clan != null)
                    GetRenderer().color = MinimapCityOverlay.ResolveColor(clan.Info.PrimaryColor ?? clan.Info.Color);
            }
        }

        public void SetRazed()
        {
            this.IsRazed = true;
            this.OwnerClanShortName = "Neutral";
            GetRenderer().color = Color.white;

            if (this.razedTower != null)
            {
                GetRenderer().sprite = this.razedTower;
            }
        }

        private Sprite ResolveTowerSprite(string clanShortName)
        {
            switch ((clanShortName ?? string.Empty).Replace(" ", string.Empty).ToLowerInvariant())
            {
                case "sirians":
                    return this.siriansTower;
                case "stormgiants":
                    return this.stormGiantsTower;
                case "greydwarves":
                    return this.greyDwarvesTower;
                case "orcsofkor":
                    return this.orcsOfKorTower;
                case "elvallie":
                    return this.elvallieTower;
                case "selentines":
                    return this.selentinesTower;
                case "horselords":
                    return this.horseLordsTower;
                case "lordbane":
                    return this.lordBaneTower;
                case "neutral":
                default:
                    return this.neutralTower;
            }
        }

        private SpriteRenderer GetRenderer()
        {
            return this.spriteRenderer != null
                ? this.spriteRenderer
                : this.spriteRenderer = GetComponent<SpriteRenderer>();
        }
    }
}
