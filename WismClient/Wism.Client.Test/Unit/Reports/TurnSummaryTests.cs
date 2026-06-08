using System;
using NUnit.Framework;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.Modules;
using Wism.Client.Test.Common;

namespace Wism.Client.Test.Unit.Reports;

/// <summary>
///     Verifies the TurnSummary report API and tribute mechanics.
/// </summary>
[TestFixture]
public class TurnSummaryTests
{
    private PlayerController playerController;
    private ControllerProvider controllerProvider;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
    }

    [SetUp]
    public void SetUp()
    {
        controllerProvider = TestUtilities.CreateControllerProvider();
        TestUtilities.NewGame(controllerProvider, "TestWorld");
        playerController = new PlayerController(TestUtilities.CreateLogFactory());
    }

    // -------------------------------------------------------------------------
    // TurnSummary
    // -------------------------------------------------------------------------

    [Test]
    public void TurnSummary_ReturnsPlayerClanAndTurn()
    {
        var sirians = Game.Current.Players[0];
        var summary = playerController.GetTurnSummary(sirians);

        Assert.That(summary.ClanName, Is.EqualTo("Sirians"));
        Assert.That(summary.Turn, Is.GreaterThan(0));
    }

    [Test]
    public void TurnSummary_IncomeMatchesPlayerGetIncome()
    {
        var sirians = Game.Current.Players[0];
        var expected = sirians.GetIncome();
        var summary = playerController.GetTurnSummary(sirians);

        Assert.That(summary.GoldIncome, Is.EqualTo(expected));
    }

    [Test]
    public void TurnSummary_UpkeepMatchesPlayerGetUpkeep()
    {
        var sirians = Game.Current.Players[0];
        var tile = World.Current.Map[2, 2];
        var army = sirians.ConscriptArmy(ModFactory.FindArmyInfo("LightInfantry"), tile);
        // ConscriptArmy(ArmyInfo) does not set upkeep (that requires ArmyInTraining from city production).
        // Set it explicitly to match LightInfantry's production upkeep so the > 0 assertion is meaningful.
        army.Upkeep = 4;

        var expected = sirians.GetUpkeep();
        var summary = playerController.GetTurnSummary(sirians);

        Assert.That(summary.ArmyUpkeep, Is.EqualTo(expected));
        Assert.That(summary.ArmyUpkeep, Is.GreaterThan(0));
    }

    [Test]
    public void TurnSummary_NetGold_IsIncomMinusUpkeep()
    {
        var sirians = Game.Current.Players[0];
        var summary = playerController.GetTurnSummary(sirians);

        Assert.That(summary.NetGold, Is.EqualTo(summary.GoldIncome - summary.ArmyUpkeep));
    }

    [Test]
    public void TurnSummary_GoldBalance_MatchesPlayerGold()
    {
        var sirians = Game.Current.Players[0];
        var summary = playerController.GetTurnSummary(sirians);

        Assert.That(summary.GoldBalance, Is.EqualTo(sirians.Gold));
    }

    [Test]
    public void TurnSummary_NullPlayer_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => playerController.GetTurnSummary(null));
    }

    // -------------------------------------------------------------------------
    // Tribute
    // -------------------------------------------------------------------------

    [Test]
    public void Tribute_CalculatesQuarterOfLoserGold()
    {
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        lordBane.Gold = 400;

        var city = sirians.GetCities()[0]; // Marthos (Sirians own it)
        var offer = playerController.CalculateTribute(sirians, lordBane, city);

        Assert.That(offer.Amount, Is.EqualTo(100)); // 25% of 400
        Assert.That(offer.CaptorClan, Is.EqualTo("Sirians"));
        Assert.That(offer.LoserClan, Is.EqualTo("LordBane"));
    }

    [Test]
    public void Tribute_CalculatesZero_WhenLoserBroke()
    {
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        lordBane.Gold = 0;

        var city = sirians.GetCities()[0];
        var offer = playerController.CalculateTribute(sirians, lordBane, city);

        Assert.That(offer.Amount, Is.EqualTo(0));
    }

    [Test]
    public void Tribute_PayTransfersGoldCorrectly()
    {
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        sirians.Gold = 200;
        lordBane.Gold = 500;

        playerController.PayTribute(lordBane, sirians, 100);

        Assert.That(lordBane.Gold, Is.EqualTo(400));
        Assert.That(sirians.Gold, Is.EqualTo(300));
    }

    [Test]
    public void Tribute_PayClampsToAvailableGold()
    {
        var sirians = Game.Current.Players[0];
        var lordBane = Game.Current.Players[1];
        lordBane.Gold = 50;
        sirians.Gold = 0;

        // LordBane tries to pay 200 but only has 50
        var actual = playerController.PayTribute(lordBane, sirians, 200);

        Assert.That(actual, Is.EqualTo(50));
        Assert.That(lordBane.Gold, Is.EqualTo(0));
        Assert.That(sirians.Gold, Is.EqualTo(50));
    }

    [Test]
    public void Tribute_NullCaptor_Throws()
    {
        var sirians = Game.Current.Players[0];
        var city = sirians.GetCities()[0];
        Assert.Throws<ArgumentNullException>(
            () => playerController.CalculateTribute(null, sirians, city));
    }

    [Test]
    public void Tribute_NullLoser_Throws()
    {
        var sirians = Game.Current.Players[0];
        var city = sirians.GetCities()[0];
        Assert.Throws<ArgumentNullException>(
            () => playerController.CalculateTribute(sirians, null, city));
    }
}
