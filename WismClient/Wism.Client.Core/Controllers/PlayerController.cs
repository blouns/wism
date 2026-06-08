using System;
using System.Collections.Generic;
using System.Linq;
using Wism.Client.Common;
using Wism.Client.Core;
using Wism.Client.Core.Reports;
using Wism.Client.MapObjects;
using Wism.Client.Modules.Infos;

namespace Wism.Client.Controllers
{
    public class PlayerController
    {
        private readonly IWismLogger logger;

        public PlayerController(IWismLoggerFactory loggerFactory)
        {
            if (loggerFactory is null)
            {
                throw new ArgumentNullException(nameof(loggerFactory));
            }

            this.logger = loggerFactory.CreateLogger();
        }

        public Hero HireHero(Player player, Tile tile)
        {
            return player.HireHero(tile);
        }

        public bool TryHireHero(Player player, Tile tile, string name, int price, out Hero hero)
        {
            return player.TryHireHero(tile, price, name, out hero);
        }

        /// <summary>
        ///     Gets info on any available hero for hire
        /// </summary>
        /// <param name="player">Player looking for a hero</param>
        /// <param name="name">Display name of hero</param>
        /// <param name="city">City of hero's origin</param>
        /// <param name="price">Hero's price to hire</param>
        /// <param name="allyKinds">Any allies joining the hero (may be zero)</param>
        /// <returns>True if a hero is available</returns>
        public bool RecruitHero(Player player, out string name, out City city, out int price,
            out List<ArmyInfo> allyKinds)
        {
            name = null;
            city = null;
            price = int.MaxValue;
            allyKinds = null;

            var success = player.RecruitHeroStrategy.IsHeroAvailable(player);

            if (success)
            {
                name = player.RecruitHeroStrategy.GetHeroName();
                city = player.RecruitHeroStrategy.GetTargetCity(player);
                price = player.RecruitHeroStrategy.GetHeroPrice(player);
                allyKinds = player.RecruitHeroStrategy.GetAllies(player);
                this.logger.LogInformation($"{name} of {city} is available for {price}!");
            }

            return success;
        }

        /// <summary>
        ///     Conscript new armies at the given tile.
        /// </summary>
        /// <param name="player">Player to conscript armies</param>
        /// <param name="armyKinds">Army kinds (may be duplicate)</param>
        /// <param name="tile">Tile to create armies</param>
        /// <param name="armies">Armies created</param>
        /// <returns>ActionState.Succeeded if armies successfully conscripted; otherwise Failed</returns>
        /// <summary>
        ///     Returns a summary of the player's completed turn: gold, production, city events.
        ///     Call after StartTurn has run for the player.
        /// </summary>
        public TurnSummary GetTurnSummary(Player player)
        {
            if (player is null) throw new ArgumentNullException(nameof(player));

            var produced = player.GetProducedArmies()
                .Select(a => new ArmyInTrainingSnapshot
                {
                    ArmyKind = a.ArmyInfo?.ShortName,
                    DestinationCity = a.DestinationCity?.ShortName,
                    Strength = a.Strength,
                    Moves = a.Moves
                })
                .ToList();

            var delivered = player.GetDeliveredArmies()
                .Select(a => new ArmyInTrainingSnapshot
                {
                    ArmyKind = a.ArmyInfo?.ShortName,
                    DestinationCity = a.DestinationCity?.ShortName,
                    Strength = a.Strength,
                    Moves = a.Moves
                })
                .ToList();

            var income = player.GetIncome();
            var upkeep = player.GetUpkeep();

            return new TurnSummary
            {
                ClanName = player.Clan.ShortName,
                Turn = player.Turn,
                GoldIncome = income,
                ArmyUpkeep = upkeep,
                GoldBalance = player.Gold,
                ArmiesProduced = produced,
                ArmiesDelivered = delivered,
                CitiesCaptured = new List<string>(),
                CitiesLost = new List<string>()
            };
        }

        /// <summary>
        ///     Calculates a tribute demand when the captor captures one of the loser's cities.
        ///     Returns a TributeOffer the UI can present to the player.
        ///     Formula: 25% of the loser's gold, rounded down, minimum 0.
        /// </summary>
        public TributeOffer CalculateTribute(Player captor, Player loser, MapObjects.City capturedCity)
        {
            if (captor is null) throw new ArgumentNullException(nameof(captor));
            if (loser is null) throw new ArgumentNullException(nameof(loser));
            if (capturedCity is null) throw new ArgumentNullException(nameof(capturedCity));

            var amount = Math.Max(0, loser.Gold / 4);
            return new TributeOffer(
                captor.Clan.ShortName,
                loser.Clan.ShortName,
                capturedCity.ShortName,
                amount);
        }

        /// <summary>
        ///     Transfers tribute gold from the payer to the recipient.
        ///     Clamps to the payer's available gold (can't go negative).
        /// </summary>
        /// <returns>Actual amount transferred.</returns>
        public int PayTribute(Player payer, Player recipient, int amount)
        {
            if (payer is null) throw new ArgumentNullException(nameof(payer));
            if (recipient is null) throw new ArgumentNullException(nameof(recipient));

            var actual = Math.Min(amount, payer.Gold);
            payer.Gold -= actual;
            recipient.Gold += actual;
            this.logger.LogInformation($"{payer} paid tribute of {actual}g to {recipient}");
            return actual;
        }

        public ActionState ConscriptArmies(Player player, List<ArmyInfo> armyKinds, Tile tile, out List<Army> armies)
        {
            if (player is null)
            {
                throw new ArgumentNullException(nameof(player));
            }

            if (armyKinds is null ||
                armyKinds.Count == 0)
            {
                throw new ArgumentNullException(nameof(armyKinds));
            }

            if (tile is null)
            {
                throw new ArgumentNullException(nameof(tile));
            }

            armies = new List<Army>();
            foreach (var armyKind in armyKinds)
            {
                armies.Add(
                    player.ConscriptArmy(armyKind, tile));
            }

            return armies.Count > 0 ? ActionState.Succeeded : ActionState.Failed;
        }
    }
}