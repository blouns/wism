using System.Collections;
using System.Linq;
using Assets.Scripts.Managers;
using Assets.Scripts.UI;
using Assets.Tests.PlayMode;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    // Exercise presentation with a durable eligible offer; the core suite owns its city thresholds.
    private void SetPresentationOffer()
    {
        var player = Game.Current.GetCurrentPlayer();
        var standings = new[] { new VictoryClanStanding(player.Clan.ShortName, player.Clan.DisplayName, 50, 20, 500, true, false) }
            .Concat(Enumerable.Range(0, 7).Select(i => new VictoryClanStanding("rival" + i, "Rival " + i, 4, 3, 40, false, false))).ToArray();
        var offer = VictoryEvaluator.EvaluateClassicSurrender(standings, 80, player.Turn);
        Assert.That(offer.SurrenderEligible, Is.True);
        Game.Current.SetVictoryOutcome(offer);
        unity.InteractiveUI = true;
    }

    private Button CampaignButton(string name) => unity.GetComponentsInChildren<Button>(true).Single(button => button.name == name);

    private IEnumerator OpenSurrenderChoice()
    {
        yield return WaitFor(() => unity.CampaignPresentation.Page == CampaignPage.Arrival);
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        Assert.That(unity.CampaignPresentation.Page, Is.EqualTo(CampaignPage.Scroll));
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        Assert.That(unity.CampaignPresentation.Page, Is.EqualTo(CampaignPage.Surrender));
    }

    [UnityTest]
    public IEnumerator CampaignPresentation_AcceptTransfersRealmThenFreezesForInspection()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        SetPresentationOffer();
        var winner = Game.Current.GetCurrentPlayer();
        var before = TerminalState();
        yield return OpenSurrenderChoice();
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(TerminalState(), Is.EqualTo(before), "An unanswered offer must pause without granting victory.");
        yield return PressJourneyKey(Key.E);
        Assert.That(TerminalState(), Is.EqualTo(before));
        yield return PressJourneyButton(CampaignButton("AcceptSurrender"));
        yield return WaitFor(() => unity.CampaignPresentation.Page == CampaignPage.Victory);
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.GameOver));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.InspectionMode));
        Assert.That(World.Current.GetCities().All(city => city.Player == winner), Is.True);
        Assert.That(Game.Current.Players.Where(player => player != winner).All(player => player.GetArmies().Count == 0), Is.True);
        var terminal = TerminalState();
        unity.CampaignPresentation.Decide(true);
        unity.CampaignPresentation.Decide(false);
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        Assert.That(unity.CampaignPresentation.Page, Is.EqualTo(CampaignPage.Dominion));
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        Assert.That(unity.CampaignPresentation.Page, Is.EqualTo(CampaignPage.Inspection));
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        Assert.That(unity.CampaignPresentation.IsOpen, Is.False);
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(TerminalState(), Is.EqualTo(terminal));
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
        Assert.That(unity.CampaignResultText, Does.Contain(winner.Clan.DisplayName));
        unity.GameMenu.Open();
        Assert.That(MenuButton("LoadGame").interactable, Is.True);
    }

    [UnityTest]
    public IEnumerator CampaignPresentation_RejectKeepsOwnershipAndDoesNotReoffer()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        SetPresentationOffer();
        var ownership = World.Current.GetCities().Select(city => city.Player).ToArray();
        int turn = Game.Current.GetCurrentPlayer().Turn;
        yield return OpenSurrenderChoice();
        yield return PressJourneyButton(CampaignButton("RejectSurrender"));
        Assert.That(unity.CampaignPresentation.Page, Is.EqualTo(CampaignPage.Rejection));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        Assert.That(Game.Current.GameState, Is.Not.EqualTo(GameState.GameOver));
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(unity.CampaignPresentation.IsOpen, Is.False);
        Assert.That(World.Current.GetCities().Select(city => city.Player).ToArray(), Is.EqualTo(ownership));
        Assert.That(Game.Current.GetCurrentPlayer().Turn, Is.EqualTo(turn));
        Assert.That(input.CanAcceptGameplayInput, Is.True);
        unity.InteractiveUI = false;
        yield return PressJourneyKey(Key.E);
        yield return new WaitForLastCommand(manager.ControllerProvider);
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        Assert.That(unity.CampaignPresentation.IsOpen, Is.False);
    }

    [UnityTest]
    public IEnumerator CampaignPresentation_LoadedOfferResumesWithoutQueuedEndTurn()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        SetPresentationOffer();
        var snapshot = Game.Current.Snapshot();
        var load = new Wism.Client.Commands.Games.LoadGameCommand(manager.ControllerProvider.GameController, snapshot);
        Assert.That(load.Execute(), Is.EqualTo(Wism.Client.Controllers.ActionState.Succeeded));
        unity.Reset();
        unity.InteractiveUI = true;
        yield return OpenSurrenderChoice();
        yield return PressJourneyButton(CampaignButton("RejectSurrender"));
        Assert.That(Game.Current.VictoryOutcome.OutcomeKind, Is.EqualTo(VictoryOutcomeKind.RejectedSurrender));
        yield return PressJourneyButton(CampaignButton("CampaignContinue"));
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
    }

    [UnityTest]
    public IEnumerator CampaignPresentation_AllAiUsesOnlyPersistentResult()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        foreach (var player in Game.Current.Players) player.IsHuman = false;
        unity.InteractiveUI = true;
        Game.Current.Transition(GameState.GameOver);
        var before = TerminalState();
        for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        Assert.That(unity.CampaignPresentation.IsOpen, Is.False);
        Assert.That(unity.CampaignResultText, Is.Not.Empty);
        Assert.That(TerminalState(), Is.EqualTo(before));
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
    }

    [UnityTest]
    public IEnumerator CampaignPresentation_UnknownWinnerDoesNotInventAVictor()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.InteractiveUI = true;
        Game.Current.Transition(GameState.GameOver);
        yield return new WaitForFixedUpdate();
        Assert.That(unity.CampaignPresentation.IsOpen, Is.False);
        Assert.That(unity.CampaignResultText, Is.EqualTo("The war is over. You may inspect the realm."));
    }

    [UnityTest]
    public IEnumerator CampaignPresentation_EliminatedHumanIsNotAnAiOnlyCampaign()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var loser = Game.Current.GetCurrentPlayer();
        var winner = Game.Current.Players.Single(player => player != loser);
        foreach (var army in loser.GetArmies().ToArray()) army.Kill();
        foreach (var city in loser.GetCities().ToArray()) winner.ClaimCity(city);
        Game.Current.EndTurn();
        Game.Current.StartTurn();
        Game.Current.EndTurn();
        Game.Current.StartTurn();
        Assert.That(loser.IsDead, Is.True);
        winner.IsHuman = false;
        unity.InteractiveUI = true;
        Game.Current.EndTurn();
        yield return WaitFor(() => unity.CampaignPresentation.Page == CampaignPage.Victory);
        var message = unity.GetComponentsInChildren<Text>().Single(text => text.name == "Message" && text.transform.parent.name == "CampaignPanel").text;
        Assert.That(message, Does.Contain(winner.Clan.DisplayName));
        Assert.That(message, Does.Not.Contain("You have won"));
    }

    [UnityTest] public IEnumerator CampaignPresentation_Viewport1024() => CampaignViewport(1024, 768);
    [UnityTest] public IEnumerator CampaignPresentation_Viewport1280() => CampaignViewport(1280, 720);
    [UnityTest] public IEnumerator CampaignPresentation_Viewport1920() => CampaignViewport(1920, 1080);
    [UnityTest] public IEnumerator CampaignPresentation_ViewportUltrawide() => CampaignViewport(2560, 1080);

    private IEnumerator CampaignViewport(int width, int height)
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        AssertBatchWindowless();
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            SetPresentationOffer();
            yield return OpenSurrenderChoice();
            Canvas.ForceUpdateCanvases();
            foreach (var name in new[] { "AcceptSurrender", "RejectSurrender" })
            {
                var rect = (RectTransform)CampaignButton(name).transform;
                var corners = new Vector3[4];
                rect.GetWorldCorners(corners);
                Assert.That(corners.All(point => point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height), Is.True);
                Assert.That(CampaignButton(name).GetComponentInChildren<Text>().preferredWidth, Is.LessThan(rect.rect.width));
            }
            var canvas = unity.GetComponentsInChildren<Canvas>().Single(item => item.name == "CampaignCanvas");
            CaptureUiCanvas(canvas, "campaign-surrender", width, height);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var buttonRect = (RectTransform)CampaignButton("RejectSurrender").transform;
            yield return Tap(RectTransformUtility.WorldToScreenPoint(null, buttonRect.TransformPoint(buttonRect.rect.center)));
            Assert.That(unity.CampaignPresentation.Page, Is.EqualTo(CampaignPage.Rejection));
        }
        finally { Screen.SetResolution(oldWidth, oldHeight, false); }
    }
}
