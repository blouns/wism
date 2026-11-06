using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.Commands;
using Wism.Client.Commands.Cities;
using Wism.Client.Commands.Players;
using Wism.Client.Common;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Data;
using Wism.Client.Data.Entities;
using Wism.Client.Factories;
using Wism.Client.MapObjects;
using Wism.Client.Modules;
using Wism.Client.Modules.Infos;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit;

/// <summary>
///     Beta ending branches exercised on the real eight-clan Illuria campaign state
///     (80 cities: one home city per clan plus 72 neutral cities). Every turn change
///     goes through the real Start/End turn commands and every clan-to-clan capture
///     uses the real capture command; the victory evaluator is only ever fed live
///     World/Player state. See docs/acceptance/beta-ending-branches.md.
/// </summary>
[TestFixture, NonParallelizable]
public sealed partial class BetaEndingBranchesTests
{
    private const string Human = "Sirians";
    private const string Rival = "LordBane";
    private const string NeutralClan = "Neutral";
    private const int IlluriaCities = 80;

    // Seat order is turn order. Each clan owns exactly one home city on Illuria.
    private static readonly string[] Seats =
    {
        "Sirians",
        "LordBane",
        "StormGiants",
        "GreyDwarves",
        "OrcsOfKor",
        "Elvallie",
        "Selentines",
        "HorseLords"
    };

    private ControllerProvider provider;
    private IWismLogger quietLogger;

    [SetUp]
    public void SetUp()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    // ------------------------------------------------------------------
    // Classic surrender eligibility (one human versus seven computer clans)
    // ------------------------------------------------------------------

    [TestCase(41, 25, true, TestName = "ClassicSurrender_Illuria_41Cities_16CityLead_IsOffered")]
    [TestCase(41, 26, false, TestName = "ClassicSurrender_Illuria_41Cities_Exactly15CityLead_IsNotOffered")]
    [TestCase(40, 1, false, TestName = "ClassicSurrender_Illuria_40Cities_HugeLead_IsNotOffered")]
    [TestCase(42, 26, true, TestName = "ClassicSurrender_Illuria_42Cities_16CityLead_IsOffered")]
    [TestCase(42, 27, false, TestName = "ClassicSurrender_Illuria_42Cities_Exactly15CityLead_IsNotOffered")]
    public void ClassicSurrender_OneHumanSevenComputers_UsesStrictCityAndLeadBoundaries(
        int humanCities,
        int strongestComputerCities,
        bool expectedOffer)
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, humanCities);
        GrowTo(Seat(Rival), strongestComputerCities);
        var stateBefore = StableState();

        var offer = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, human.Turn);

        Assert.That(offer.SurrenderEligible, Is.EqualTo(expectedOffer));
        Assert.That(offer.OutcomeKind, Is.EqualTo(expectedOffer ? VictoryOutcomeKind.SurrenderOffered : VictoryOutcomeKind.None));
        Assert.That(offer.WinnerClanShortName, Is.EqualTo(Human), "The human leads in every boundary case.");
        Assert.That(offer.LeaderCities, Is.EqualTo(humanCities));
        Assert.That(offer.RunnerUpCities, Is.EqualTo(strongestComputerCities));
        Assert.That(offer.TotalCities, Is.EqualTo(IlluriaCities), "Neutral cities count toward the realm total.");
        Assert.That(offer.ActiveClanCount, Is.EqualTo(Seats.Length));
        var neutralCities = IlluriaCities - humanCities - strongestComputerCities - (Seats.Length - 2);
        Assert.That(CountNeutralCities(), Is.EqualTo(neutralCities));
        Assert.That(offer.UnclaimedCityShare, Is.EqualTo(Math.Round(neutralCities / (double)IlluriaCities, 4)),
            "Neutral cities are unclaimed; they never count toward the human or any computer clan.");
        Assert.That(Game.Current.VictoryOutcome, Is.Null, "Evaluating an offer must not record one.");
        Assert.That(StableState(), Is.EqualTo(stateBefore), "Evaluating an offer must not mutate the campaign.");
    }

    [Test]
    public void ClassicSurrender_IsNeverOfferedInAiOnlyCampaign_EvenToADominantComputer()
    {
        this.CreateIlluriaCampaign();
        GrowTo(Seat(Rival), 60);

        var offer = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, 40);

        Assert.That(offer.WinnerClanShortName, Is.EqualTo(Rival));
        Assert.That(offer.LeaderCities, Is.EqualTo(60));
        Assert.That(offer.SurrenderEligible, Is.False);
        Assert.That(offer.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.None));
    }

    [Test]
    public void ClassicSurrender_AtHumanEndTurn_StillCountsAComputerThatJustLostItsLastCity()
    {
        // The offer is evaluated when the human ends a turn. A computer clan whose last
        // city was just taken is only eliminated at the start of its own next turn.
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        var victim = Seat("StormGiants");
        GrowTo(human, 40);

        this.Capture(human, victim.GetCities().Single());
        this.EndTurn(human);

        Assert.That(human.GetCities().Count, Is.EqualTo(41));
        Assert.That(victim.GetCities(), Is.Empty);
        Assert.That(victim.IsDead, Is.False, "Elimination happens at the victim's next turn start.");
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(Seat(Rival)));
        var offer = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, human.Turn);
        Assert.That(offer.ActiveClanCount, Is.EqualTo(Seats.Length));
        Assert.That(offer.SurrenderEligible, Is.True);
        Assert.That(offer.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.SurrenderOffered));
    }

    // ------------------------------------------------------------------
    // Offer acceptance versus rejection, and persistence of each decision
    // ------------------------------------------------------------------

    [TestCase(false, TestName = "PendingOffer_SurvivesUnitySaveLoad_ThenAcceptanceEndsInInspectionMode")]
    [TestCase(true, TestName = "PendingOffer_SurvivesTerminalSaveLoad_ThenAcceptanceEndsInInspectionMode")]
    public void PendingOffer_SurvivesSaveLoad_ThenAcceptanceEndsInInspectionMode(bool terminalSerializer)
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        var rival = Seat(Rival);
        rival.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), FreeTileBeside(rival.GetCities().Single(), rival));
        GrowTo(human, 41);
        GrowTo(rival, 25);
        var offer = this.EndHumanTurnAndRecordOffer(human);
        Assert.That(offer.SurrenderEligible, Is.True);

        SaveAndReload(terminalSerializer);

        var pending = Game.Current.VictoryOutcome;
        Assert.That(pending, Is.Not.Null);
        Assert.That(pending.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.SurrenderOffered));
        Assert.That(pending.SurrenderEligible, Is.True);
        Assert.That(pending.WinnerClanShortName, Is.EqualTo(Human));
        Assert.That(pending.RunnerUpClanShortName, Is.EqualTo(Rival));
        Assert.That(pending.LeaderCities, Is.EqualTo(41));
        Assert.That(pending.RunnerUpCities, Is.EqualTo(25));
        Assert.That(pending.TotalCities, Is.EqualTo(IlluriaCities));
        Assert.That(pending.Turns, Is.EqualTo(offer.Turns));
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.StartingTurn), "A pending offer is not an ending.");
        Assert.That(Game.Current.GetCurrentPlayer().Clan.ShortName, Is.EqualTo(Rival),
            "The next computer seat holds the turn while the offer waits for the human.");

        VictoryEvaluator.AcceptSurrender(Game.Current, World.Current, pending);

        AssertAcceptedSurrender();
        this.AssertNoFurtherTurnsOrAi();
        SaveAndReload(terminalSerializer);
        AssertAcceptedSurrender();
        this.UseFreshControllers();
        this.AssertNoFurtherTurnsOrAi();
    }

    [TestCase(false, TestName = "RejectedOffer_SurvivesUnitySaveLoad_CannotBeAcceptedLater_AndWarContinues")]
    [TestCase(true, TestName = "RejectedOffer_SurvivesTerminalSaveLoad_CannotBeAcceptedLater_AndWarContinues")]
    public void RejectedOffer_SurvivesSaveLoad_CannotBeAcceptedLater_AndWarContinues(bool terminalSerializer)
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 41);
        GrowTo(Seat(Rival), 25);
        this.EndHumanTurnAndRecordOffer(human);
        VictoryEvaluator.RejectSurrender(Game.Current, Game.Current.VictoryOutcome);

        SaveAndReload(terminalSerializer);
        this.UseFreshControllers();
        human = Seat(Human);

        var rejected = Game.Current.VictoryOutcome;
        Assert.That(rejected.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        Assert.That(rejected.SurrenderEligible, Is.False);
        Assert.That(rejected.WinnerClanShortName, Is.EqualTo(Human));
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.StartingTurn));

        var beforeLateDecision = StableState();
        Assert.Throws<InvalidOperationException>(() =>
            VictoryEvaluator.AcceptSurrender(Game.Current, World.Current, Game.Current.VictoryOutcome));
        Assert.Throws<InvalidOperationException>(() =>
            VictoryEvaluator.RejectSurrender(Game.Current, Game.Current.VictoryOutcome));
        Assert.That(StableState(), Is.EqualTo(beforeLateDecision), "A rejected offer cannot be revived from the saved outcome.");

        // The war continues through every computer seat and back to the human.
        var played = this.AdvanceTo(human);
        Assert.That(played.Select(player => player.Clan.ShortName), Is.EqualTo(Seats.Skip(1).Append(Human)));
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        Assert.That(human.GetCities().Count, Is.EqualTo(41));
        Assert.That(Seat(Rival).GetCities().Count, Is.EqualTo(25));

        // The evaluator is stateless and would offer again; the stored rejection is what
        // keeps the classic "no quarter" decision closed for the rest of the campaign.
        GrowTo(human, 48);
        this.EndTurn(human);
        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, human.Turn).SurrenderEligible, Is.True);
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
    }

    [Test]
    public void RejectedOffer_LaterHumanConquestReplacesOutcome_AndLeavesNeutralCitiesNeutral()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 41);
        this.EndHumanTurnAndRecordOffer(human);
        VictoryEvaluator.RejectSurrender(Game.Current, Game.Current.VictoryOutcome);
        var neutralBefore = CountNeutralCities();

        foreach (var victim in Game.Current.Players.Where(player => player != human).ToArray())
        {
            this.CaptureLastCityAndEliminate(human, victim);
        }

        this.AdvanceTo(human);
        this.EndTurn(human);

        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        var outcome = Game.Current.VictoryOutcome;
        Assert.That(outcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.Conquest), "Conquest replaces the rejected offer.");
        Assert.That(outcome.WinnerClanShortName, Is.EqualTo(Human));
        Assert.That(outcome.SurrenderEligible, Is.False);
        Assert.That(outcome.ActiveClanCount, Is.EqualTo(1));
        Assert.That(CountNeutralCities(), Is.EqualTo(neutralBefore), "Conquest does not claim neutral cities.");
        Assert.That(outcome.UnclaimedCityShare, Is.EqualTo(Math.Round(neutralBefore / (double)IlluriaCities, 4)));
        Assert.That(neutralBefore, Is.GreaterThan(0));
        this.AssertNoFurtherTurnsOrAi();

        SaveAndReload(terminalSerializer: true);
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.Conquest));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(Human));
        this.UseFreshControllers();
        this.AssertNoFurtherTurnsOrAi();
    }

    // ------------------------------------------------------------------
    // Human defeat, hotseat survivors, and AI-only continuation
    // ------------------------------------------------------------------

    [Test]
    public void HumanDefeat_EliminatedHumanIsSkipped_AndLastComputerStandingWinsByConquest()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        var rival = Seat(Rival);
        var fieldArmy = human.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), FreeTileBeside(human.GetCities().Single(), human));

        this.AdvanceTo(rival);
        this.Capture(rival, human.GetCities().Single());
        this.EndTurn(rival);
        this.AdvanceTo(human);

        Assert.That(human.IsDead, Is.True, "A human with no cities is eliminated at their turn start.");
        Assert.That(human.GetArmies(), Is.Empty);
        Assert.That(fieldArmy.IsDead, Is.True, "Field armies leave the war with their clan.");
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.Ready), "The eliminated seat still ends its turn.");
        Assert.That(Game.Current.VictoryOutcome, Is.Null, "Six computer clans remain, so nothing has ended.");

        this.EndTurn(human);
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(rival));
        var lastSeat = Seat(Seats[^1]);
        var round = this.AdvanceTo(lastSeat);
        this.EndTurn(lastSeat);
        Assert.That(round.Select(player => player.Clan.ShortName), Is.EqualTo(Seats.Skip(1)));
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(rival), "An eliminated human's seat is skipped.");

        foreach (var victim in Game.Current.Players.Where(player => player != human && player != rival).ToArray())
        {
            this.CaptureLastCityAndEliminate(rival, victim);
        }

        this.AdvanceTo(rival);
        this.EndTurn(rival);

        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.Conquest));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(Rival));
        Assert.That(human.IsHuman && human.IsDead, Is.True,
            "The defeated human remains on the roster, so the campaign is still a human campaign.");
        Assert.That(CountNeutralCities(), Is.EqualTo(IlluriaCities - Seats.Length));
        this.AssertNoFurtherTurnsOrAi();

        SaveAndReload(terminalSerializer: false);
        this.UseFreshControllers();
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(Rival));
        Assert.That(Seat(Human).IsDead, Is.True);
        this.AssertNoFurtherTurnsOrAi();
    }

    [Test]
    public void Hotseat_EliminatedHumanIsSkipped_SurvivingHumansPlayOn_AndLastHumanWins()
    {
        this.CreateIlluriaCampaign(Human, Rival, "StormGiants");
        var first = Seat(Human);
        var fallen = Seat(Rival);
        var survivor = Seat("StormGiants");

        this.CaptureLastCityAndEliminate(first, fallen);

        Assert.That(fallen.IsDead, Is.True);
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome, Is.Null);
        var round = this.AdvanceTo(first);
        Assert.That(round, Does.Contain(survivor).And.Not.Contain(fallen));
        var handoff = this.AdvanceTo(survivor);
        Assert.That(handoff, Is.EqualTo(new[] { survivor }),
            "The turn passes from one surviving human straight to the next, skipping the fallen seat.");

        // Hotseat campaigns are not one-human campaigns: no offer even with a crushing lead,
        // whether two humans survive or only one does.
        GrowTo(first, 50);
        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, first.Turn).SurrenderEligible, Is.False);
        this.CaptureLastCityAndEliminate(first, survivor);
        Assert.That(Game.Current.Players.Count(player => player.IsHuman && !player.IsDead), Is.EqualTo(1));
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
        Assert.That(VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, first.Turn).SurrenderEligible, Is.False);

        foreach (var victim in Game.Current.Players.Where(player => player != first && !player.IsDead).ToArray())
        {
            this.CaptureLastCityAndEliminate(first, victim);
        }

        this.AdvanceTo(first);
        this.EndTurn(first);

        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.Conquest));
        Assert.That(Game.Current.VictoryOutcome.WinnerClanShortName, Is.EqualTo(Human));
        Assert.That(new[] { fallen, survivor }.All(player => player.IsHuman && player.IsDead), Is.True);
        this.AssertNoFurtherTurnsOrAi();
    }

    // ------------------------------------------------------------------
    // Assertions
    // ------------------------------------------------------------------

    private static void AssertAcceptedSurrender()
    {
        var game = Game.Current;
        var winner = Seat(Human);
        Assert.That(game.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(game.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.InspectionMode));
        Assert.That(game.VictoryOutcome.SurrenderEligible, Is.False);
        Assert.That(game.VictoryOutcome.WinnerClanShortName, Is.EqualTo(Human));
        Assert.That(World.Current.GetCities().Count, Is.EqualTo(IlluriaCities));
        Assert.That(World.Current.GetCities().All(city => city.Clan.ShortName == Human), Is.True,
            "Accepting the surrender claims every city, including neutral cities.");
        Assert.That(winner.GetCities().Count, Is.EqualTo(IlluriaCities));
        foreach (var loser in game.Players.Where(player => player != winner))
        {
            Assert.That(loser.GetCities(), Is.Empty, loser.Clan.ShortName);
            Assert.That(loser.GetArmies(), Is.Empty, loser.Clan.ShortName);
        }

        var boardArmies = World.Current.Map.Cast<Tile>().SelectMany(tile => tile.GetAllArmies()).ToArray();
        Assert.That(boardArmies, Is.Not.Empty);
        Assert.That(boardArmies.All(army => army.Player == winner), Is.True, "Every remaining army belongs to the winner.");
    }

    /// <summary>
    ///     After an ending, turn commands fail, the production AI for the seat that holds
    ///     the turn generates nothing, and neither the board nor any turn counter moves.
    /// </summary>
    private void AssertNoFurtherTurnsOrAi()
    {
        var game = Game.Current;
        var seat = game.GetCurrentPlayer();
        var ai = WarlordsClassicAiFactory.CreateCommandProvider(this.provider, this.quietLogger);
        var before = StableState();
        var turns = game.Players.Select(player => player.Turn).ToArray();
        var queued = this.provider.CommandController.GetCommands().Count();

        for (var i = 0; i < 10; i++)
        {
            Assert.That(TestUtilities.ExecuteCommandUntilDone(this.provider.CommandController,
                new StartTurnCommand(this.provider.GameController, seat)), Is.EqualTo(ActionState.Failed));
            Assert.That(TestUtilities.ExecuteCommandUntilDone(this.provider.CommandController,
                new EndTurnCommand(this.provider.GameController, seat)), Is.EqualTo(ActionState.Failed));
            ai.GenerateCommands();
            Assert.That(ai.GetBufferedCommands(), Is.Empty);
            game.StartTurn();
            game.EndTurn();
        }

        Assert.That(this.provider.CommandController.GetCommands().Count(), Is.EqualTo(queued + 20),
            "Only the rejected turn commands were queued; the AI queued nothing.");
        Assert.That(game.Players.Select(player => player.Turn).ToArray(), Is.EqualTo(turns));
        Assert.That(game.GetCurrentPlayer(), Is.SameAs(seat));
        Assert.That(game.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(StableState(), Is.EqualTo(before));
    }

    // ------------------------------------------------------------------
    // Campaign fixture and real turn/capture flow
    // ------------------------------------------------------------------

    private void CreateIlluriaCampaign(params string[] humanClans)
    {
        var settings = TestGameFactory.CreateDefaultNewGameSettings(TestUtilities.DefaultTestWorld);
        var worldPath = Path.Combine(ModFactory.ModPath, ModFactory.WorldsPath, "Illuria");
        settings.World = LoadJson<WorldEntity>(Path.Combine(worldPath, "Map.json"));
        settings.World.Cities = LoadJson<CityInfo[]>(Path.Combine(worldPath, "City.json"))
            .Select(info => new CityEntity
            {
                CityShortName = info.ShortName,
                ClanShortName = info.ClanName,
                Defense = info.Defense,
                X = info.X,
                Y = info.Y,
                Definition = info
            })
            .ToArray();
        settings.World.Locations = new LocationEntity[0];
        settings.Players = Seats
            .Select(clan => new PlayerEntity { ClanShortName = clan, IsHuman = humanClans.Contains(clan) })
            .ToArray();
        GameFactory.Create(settings);

        foreach (var player in Game.Current.Players)
        {
            player.IsHuman = humanClans.Contains(player.Clan.ShortName);
            Assert.That(player.GetCities().Count, Is.EqualTo(1), player.Clan.ShortName);
        }

        Assert.That(Game.Current.Players.Select(player => player.Clan.ShortName), Is.EqualTo(Seats));
        Assert.That(World.Current.GetCities().Count, Is.EqualTo(IlluriaCities));
        Assert.That(CountNeutralCities(), Is.EqualTo(IlluriaCities - Seats.Length));
        Assert.That(Game.Current.GetCurrentPlayer().Clan.ShortName, Is.EqualTo(Seats[0]));
        this.UseFreshControllers();
    }

    private void UseFreshControllers()
    {
        var loggers = new QuietLoggerFactory();
        this.quietLogger = loggers.CreateLogger();
        this.provider = new ControllerProvider
        {
            ArmyController = new ArmyController(loggers),
            CommandController = new CommandController(loggers, new WismClientInMemoryRepository(new SortedList<int, Command>())),
            GameController = new GameController(loggers),
            CityController = new CityController(loggers),
            HeroController = new HeroController(loggers),
            LocationController = new LocationController(loggers),
            PlayerController = new PlayerController(loggers)
        };
    }

    /// <summary>
    ///     Mirrors the presentation end-turn hook: the human ends the turn through the real
    ///     command, then the classic offer is evaluated with that human's turn number and
    ///     recorded as the durable outcome only when eligible.
    /// </summary>
    private VictoryOutcomeSnapshot EndHumanTurnAndRecordOffer(Player human)
    {
        this.EndTurn(human);
        var offer = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, human.Turn);
        Assert.That(offer.SurrenderEligible, Is.True, "Fixture must produce an eligible classic offer.");
        Game.Current.SetVictoryOutcome(offer);
        return offer;
    }

    private void CaptureLastCityAndEliminate(Player conqueror, Player victim)
    {
        this.AdvanceTo(conqueror);
        this.Capture(conqueror, victim.GetCities().Single());
        Assert.That(victim.GetCities(), Is.Empty);
        this.EndTurn(conqueror);

        while (!victim.IsDead)
        {
            Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
            var current = Game.Current.GetCurrentPlayer();
            Assert.That(current, Is.Not.SameAs(conqueror), victim.Clan.ShortName + " was skipped before elimination.");
            this.StartTurn(current);
            if (current != victim)
            {
                this.EndTurn(current);
            }
        }

        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(victim));
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.Ready));
        this.EndTurn(victim);
    }

    /// <summary>
    ///     Plays seats (start and end turn) until <paramref name="target" /> holds the turn,
    ///     then starts the target's turn. Returns every seat that started a turn.
    /// </summary>
    private List<Player> AdvanceTo(Player target)
    {
        var started = new List<Player>();
        for (var guard = 0; Game.Current.GetCurrentPlayer() != target; guard++)
        {
            Assert.That(guard, Is.LessThan(Seats.Length * 2), "Turn order never reached " + target.Clan.ShortName);
            var current = Game.Current.GetCurrentPlayer();
            if (Game.Current.GameState == GameState.StartingTurn)
            {
                this.StartTurn(current);
                started.Add(current);
            }

            Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
            this.EndTurn(current);
        }

        if (Game.Current.GameState == GameState.StartingTurn)
        {
            this.StartTurn(target);
            started.Add(target);
        }

        return started;
    }

    private void StartTurn(Player player)
    {
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(player));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(this.provider.CommandController,
            new StartTurnCommand(this.provider.GameController, player)), Is.EqualTo(ActionState.Succeeded));
    }

    private void EndTurn(Player player)
    {
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(player));
        Assert.That(TestUtilities.ExecuteCommandUntilDone(this.provider.CommandController,
            new EndTurnCommand(this.provider.GameController, player)), Is.EqualTo(ActionState.Succeeded));
    }

    private void Capture(Player conqueror, City city)
    {
        var army = conqueror.ConscriptArmy(ArmyInfo.GetArmyInfo("LightInfantry"), FreeTileBeside(city, conqueror));
        var command = new CaptureCityCommand(this.provider.CityController, conqueror, new List<Army> { army }, city);

        Assert.That(TestUtilities.ExecuteCommandUntilDone(this.provider.CommandController, command), Is.EqualTo(ActionState.Succeeded));
        Assert.That(city.Player, Is.SameAs(conqueror));
    }

    private static Tile FreeTileBeside(City city, Player player)
    {
        var infantry = ArmyInfo.GetArmyInfo("LightInfantry");
        return World.Current.Map.Cast<Tile>()
            .Where(tile => tile.GetAllArmies().Count == 0)
            .Where(tile => !tile.HasCity())
            .Where(tile => city.GetTiles().Any(cityTile => tile.IsNeighbor(cityTile)))
            .First(tile => tile.CanTraverseHere(player.Clan, infantry));
    }

    /// <summary>
    ///     Grants neutral cities (lowest short names first) until the player owns
    ///     <paramref name="cityCount" /> cities. Any neutral garrison changes hands with the
    ///     city, as it does when a surrender is accepted.
    /// </summary>
    private static void GrowTo(Player player, int cityCount)
    {
        var needed = cityCount - player.GetCities().Count;
        Assert.That(needed, Is.GreaterThanOrEqualTo(0));
        var grants = World.Current.GetCities()
            .Where(city => city.Clan.ShortName == NeutralClan)
            .OrderBy(city => city.ShortName, StringComparer.Ordinal)
            .Take(needed)
            .ToArray();
        Assert.That(grants.Length, Is.EqualTo(needed), "Not enough neutral cities for the fixture.");
        foreach (var city in grants)
        {
            foreach (var garrison in city.MusterArmies().ToArray())
            {
                garrison.Player.TransferArmyTo(garrison, player);
            }

            player.ClaimCity(city);
        }

        Assert.That(player.GetCities().Count, Is.EqualTo(cityCount));
    }

    private static Player Seat(string clan)
    {
        return Game.Current.Players.Single(player => player.Clan.ShortName == clan);
    }

    private static int CountNeutralCities()
    {
        return World.Current.GetCities().Count(city => city.Clan.ShortName == NeutralClan);
    }

    /// <summary>
    ///     Round-trips through JSON using either the resolver the Unity save path uses
    ///     (default) or the one the terminal and agent runtimes use (all members).
    /// </summary>
    private static void SaveAndReload(bool terminalSerializer)
    {
        var settings = new JsonSerializerSettings
        {
            ContractResolver = terminalSerializer ? new JsonContractResolver() : new DefaultContractResolver()
        };
        var json = JsonConvert.SerializeObject(Game.Current.Snapshot(), settings);
        GameFactory.Load(JsonConvert.DeserializeObject<GameEntity>(json, settings));
    }

    private static string StableState()
    {
        var snapshot = Game.Current.Snapshot();
        snapshot.Timestamp = DateTime.MinValue;
        return JsonConvert.SerializeObject(snapshot);
    }

    private static T LoadJson<T>(string path)
    {
        return JsonConvert.DeserializeObject<T>(File.ReadAllText(path));
    }

    /// <summary>
    ///     Controllers and the AI log through this no-op logger so the suite does not
    ///     depend on the file-backed trace log.
    /// </summary>
    private sealed class QuietLoggerFactory : IWismLoggerFactory, IWismLogger
    {
        public IWismLogger CreateLogger()
        {
            return this;
        }

        public void LogInformation(string message)
        {
        }

        public void LogWarning(string message)
        {
        }

        public void LogError(string message)
        {
        }
    }
}
