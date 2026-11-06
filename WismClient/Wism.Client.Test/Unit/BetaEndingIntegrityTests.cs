using System;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using NUnit.Framework;
using Wism.Client.Core;

namespace Wism.Client.Test.Unit;

/// <summary>
///     Ending-integrity cases for the Beta campaign: bounded turn-order lookup across
///     dead seats, and surrender decisions that only honor the game's current pending
///     offer. Shares the real Illuria campaign fixture with <see cref="BetaEndingBranchesTests" />.
///     See docs/acceptance/beta-ending-branches.md.
/// </summary>
public sealed partial class BetaEndingBranchesTests
{
    private static readonly TimeSpan NextPlayerLimit = TimeSpan.FromSeconds(2);

    // ------------------------------------------------------------------
    // Next-seat lookup is bounded and agrees with the real end-turn order
    // ------------------------------------------------------------------

    [Test]
    public void NextPlayer_SkipsSeveralConsecutiveEliminatedSeats_AndMatchesTheRealEndTurn()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        var rival = Seat(Rival);
        this.CaptureLastCityAndEliminate(human, Seat("StormGiants"));
        this.CaptureLastCityAndEliminate(human, Seat("GreyDwarves"));
        this.AdvanceTo(rival);
        var before = StableState();

        var next = NextPlayerWithinLimit();

        Assert.That(next, Is.SameAs(Seat("OrcsOfKor")), "Both eliminated seats after the rival are skipped.");
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(rival), "Looking ahead does not move the turn.");
        Assert.That(StableState(), Is.EqualTo(before), "Looking ahead does not mutate the campaign.");
        this.EndTurn(rival);
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(next), "The prediction is the seat the real end turn selects.");
    }

    [TestCase(7, new[] { 0, 1, 2 }, 3, TestName = "NextPlayer_FromLastSeat_WrapsPastEliminatedFirstSeats")]
    [TestCase(5, new[] { 6, 7, 0 }, 1, TestName = "NextPlayer_WrapsAcrossTheEndOfTheRosterPastEliminatedSeats")]
    [TestCase(3, new[] { 4, 5, 6, 7, 0, 1, 2 }, 3, TestName = "NextPlayer_WhenOnlyTheCurrentSeatIsAlive_ReturnsTheCurrentSeat")]
    [TestCase(3, new[] { 0, 1, 2, 3, 4, 5, 6, 7 }, -1, TestName = "NextPlayer_WhenEverySeatIsEliminated_ReturnsNull")]
    public void NextPlayer_IsBoundedAcrossEliminatedSeats(int currentSeat, int[] eliminatedSeats, int expectedSeat)
    {
        this.CreateIlluriaCampaign(Human);
        Game.Current.CurrentPlayerIndex = currentSeat;
        foreach (var seat in eliminatedSeats)
        {
            Game.Current.Players[seat].IsDead = true;
        }

        var next = NextPlayerWithinLimit();

        Assert.That(next, expectedSeat < 0 ? Is.Null : Is.SameAs(Game.Current.Players[expectedSeat]));
        Assert.That(Game.Current.CurrentPlayerIndex, Is.EqualTo(currentSeat), "Looking ahead does not move the turn.");
    }

    [Test]
    public void NextPlayer_WithoutARoster_ThrowsInsteadOfSpinning()
    {
        Game.CreateEmpty();
        Assert.Throws<InvalidOperationException>(() => NextPlayerWithinLimit());
        Game.Current.Players = new System.Collections.Generic.List<Player>();
        Assert.Throws<InvalidOperationException>(() => NextPlayerWithinLimit());
    }

    // ------------------------------------------------------------------
    // Only the game's current pending offer can be decided
    // ------------------------------------------------------------------

    [Test]
    public void RetainedOffer_AfterRejection_CannotBeAcceptedOrRejectedAgain()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 41);
        GrowTo(Seat(Rival), 25);
        var offer = this.EndHumanTurnAndRecordOffer(human);
        VictoryEvaluator.RejectSurrender(Game.Current, offer);
        var rejected = Game.Current.VictoryOutcome;

        AssertDecisionRefused(offer);

        Assert.That(Game.Current.VictoryOutcome, Is.SameAs(rejected));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.StartingTurn));
    }

    [Test]
    public void ReplacedOffer_CannotBeDecided_OnlyTheCurrentPendingOfferCan()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 41);
        GrowTo(Seat(Rival), 25);
        var first = this.EndHumanTurnAndRecordOffer(human);
        this.AdvanceTo(human);
        GrowTo(human, 43);
        var second = this.EndHumanTurnAndRecordOffer(human);
        Assert.That(second.Turns, Is.GreaterThan(first.Turns));

        AssertDecisionRefused(first);
        Assert.That(Game.Current.VictoryOutcome, Is.SameAs(second));

        VictoryEvaluator.AcceptSurrender(Game.Current, World.Current, second);
        AssertAcceptedSurrender();
        Assert.That(Game.Current.VictoryOutcome.Turns, Is.EqualTo(second.Turns));
    }

    [Test]
    public void OfferFromAnEarlierGame_IsRefusedByANewGame_BeforeAnyMutation()
    {
        this.CreateIlluriaCampaign(Human);
        GrowTo(Seat(Human), 41);
        var earlier = this.EndHumanTurnAndRecordOffer(Seat(Human));

        this.CreateIlluriaCampaign(Human);
        Assert.That(Game.Current.VictoryOutcome, Is.Null);
        AssertDecisionRefused(earlier);
        Assert.That(Game.Current.VictoryOutcome, Is.Null, "The new game records no decision.");

        // Same clans and an equally eligible offer of its own: the earlier snapshot is still foreign.
        GrowTo(Seat(Human), 41);
        var current = this.EndHumanTurnAndRecordOffer(Seat(Human));
        AssertDecisionRefused(earlier);
        Assert.That(Game.Current.VictoryOutcome, Is.SameAs(current));
    }

    [TestCase(false, TestName = "PendingOffer_AfterUnitySaveLoad_OnlyTheLoadedOfferCanBeDecided")]
    [TestCase(true, TestName = "PendingOffer_AfterTerminalSaveLoad_OnlyTheLoadedOfferCanBeDecided")]
    public void PendingOffer_AfterSaveLoad_OnlyTheLoadedOfferCanBeDecided(bool terminalSerializer)
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 41);
        GrowTo(Seat(Rival), 25);
        var beforeLoad = this.EndHumanTurnAndRecordOffer(human);

        SaveAndReload(terminalSerializer);
        this.UseFreshControllers();

        var loaded = Game.Current.VictoryOutcome;
        Assert.That(loaded, Is.Not.SameAs(beforeLoad));
        Assert.That(loaded.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.SurrenderOffered));
        AssertDecisionRefused(beforeLoad);
        Assert.That(Game.Current.VictoryOutcome, Is.SameAs(loaded));

        VictoryEvaluator.RejectSurrender(Game.Current, loaded);
        SaveAndReload(terminalSerializer);
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        AssertDecisionRefused(loaded);
        AssertDecisionRefused(Game.Current.VictoryOutcome);
    }

    [Test]
    public void AcceptedOffer_IsTerminal_RetainedOfferCannotReopenIt()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 41);
        GrowTo(Seat(Rival), 25);
        var offer = this.EndHumanTurnAndRecordOffer(human);
        VictoryEvaluator.AcceptSurrender(Game.Current, World.Current, offer);
        AssertAcceptedSurrender();

        AssertDecisionRefused(offer);
        AssertDecisionRefused(Game.Current.VictoryOutcome);

        AssertAcceptedSurrender();
        this.AssertNoFurtherTurnsOrAi();
    }

    // ------------------------------------------------------------------
    // Lead is measured against the strongest computer, wherever it sits
    // ------------------------------------------------------------------

    [TestCase(25, true, TestName = "ClassicSurrender_LeadIsMeasuredAgainstTheStrongestComputer_16CityLead_IsOffered")]
    [TestCase(26, false, TestName = "ClassicSurrender_LeadIsMeasuredAgainstTheStrongestComputer_Exactly15CityLead_IsNotOffered")]
    public void ClassicSurrender_LeadIsMeasuredAgainstTheStrongestComputer(int strongestCities, bool expectedOffer)
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        var strongest = Seat(Seats[^1]);
        GrowTo(human, 41);
        GrowTo(strongest, strongestCities);

        var offer = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, human.Turn);

        Assert.That(offer.SurrenderEligible, Is.EqualTo(expectedOffer));
        Assert.That(offer.RunnerUpClanShortName, Is.EqualTo(strongest.Clan.ShortName),
            "The runner-up is the strongest computer, not the next seat.");
        Assert.That(offer.RunnerUpCities, Is.EqualTo(strongestCities));
    }

    // ------------------------------------------------------------------
    // The offer does not depend on how many computer clans the map has or has lost
    // ------------------------------------------------------------------

    [Test]
    public void ClassicSurrender_IsStillOfferedAfterAComputerClanIsEliminated()
    {
        this.CreateIlluriaCampaign(Human);
        var human = Seat(Human);
        GrowTo(human, 40);
        this.CaptureLastCityAndEliminate(human, Seat("StormGiants"));
        GrowTo(Seat(Rival), 25);

        var offer = VictoryEvaluator.EvaluateClassicSurrender(World.Current, Game.Current.Players, human.Turn);

        Assert.That(Seat("StormGiants").IsDead, Is.True);
        Assert.That(human.GetCities().Count, Is.EqualTo(41));
        Assert.That(offer.ActiveClanCount, Is.EqualTo(Seats.Length - 1));
        Assert.That(offer.SurrenderEligible, Is.True,
            "41 cities with a 16-city lead over the strongest computer, six computers still fighting.");
    }

    [TestCase(1, 0, true, TestName = "ClassicSurrender_TwoClanMap_OneComputer_IsOffered")]
    [TestCase(3, 0, true, TestName = "ClassicSurrender_FourClanMap_ThreeComputers_IsOffered")]
    [TestCase(5, 4, true, TestName = "ClassicSurrender_SixClanMap_OneOfFiveComputersLeft_IsOffered")]
    [TestCase(3, 3, false, TestName = "ClassicSurrender_FourClanMap_EveryComputerEliminated_IsNotOffered")]
    public void ClassicSurrender_AppliesToAnyComputerClanCount(int computers, int eliminated, bool expectedOffer)
    {
        var standings = new[] { new VictoryClanStanding("Human", "Human", 41, 10, 100, true, false) }
            .Concat(Enumerable.Range(0, computers).Select(index => new VictoryClanStanding(
                "Computer" + index,
                "Computer " + index,
                index < eliminated ? 0 : index == eliminated ? 25 : 1,
                index < eliminated ? 0 : 3,
                10,
                false,
                index < eliminated)))
            .ToArray();

        var offer = VictoryEvaluator.EvaluateClassicSurrender(standings, totalCities: 80, turn: 40);

        Assert.That(offer.SurrenderEligible, Is.EqualTo(expectedOffer));
        Assert.That(offer.ActiveClanCount, Is.EqualTo(1 + computers - eliminated));
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    /// <summary>
    ///     Both decisions on <paramref name="offer" /> are refused and leave the campaign,
    ///     its recorded outcome and its game state exactly as they were.
    /// </summary>
    private static void AssertDecisionRefused(VictoryOutcomeSnapshot offer)
    {
        var game = Game.Current;
        var before = StableState();
        var outcome = game.VictoryOutcome;
        var state = game.GameState;

        Assert.Throws<InvalidOperationException>(() => VictoryEvaluator.AcceptSurrender(game, World.Current, offer));
        Assert.Throws<InvalidOperationException>(() => VictoryEvaluator.RejectSurrender(game, offer));

        Assert.That(game.VictoryOutcome, Is.SameAs(outcome));
        Assert.That(game.GameState, Is.EqualTo(state));
        Assert.That(StableState(), Is.EqualTo(before), "A refused decision must not mutate the campaign.");
    }

    /// <summary>
    ///     Calls <see cref="Game.GetNextPlayer" /> on a background probe thread. If it has not
    ///     returned within the limit, every seat is revived so a spinning lookup (which re-reads
    ///     the same dead seat) exits, the probe is joined, and the test fails. No probe thread
    ///     is left running.
    /// </summary>
    private static Player NextPlayerWithinLimit()
    {
        var game = Game.Current;
        Player result = null;
        Exception error = null;
        var probe = new Thread(() =>
        {
            try
            {
                result = game.GetNextPlayer();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        })
        {
            IsBackground = true,
            Name = "GetNextPlayer probe"
        };

        probe.Start();
        if (!probe.Join(NextPlayerLimit))
        {
            foreach (var player in game.Players)
            {
                player.IsDead = false;
            }

            var released = probe.Join(TimeSpan.FromSeconds(10));
            Assert.Fail($"GetNextPlayer did not return within {NextPlayerLimit.TotalSeconds:0} s " +
                        $"(probe released after reviving seats: {released}).");
        }

        if (error != null)
        {
            ExceptionDispatchInfo.Capture(error).Throw();
        }

        return result;
    }
}
