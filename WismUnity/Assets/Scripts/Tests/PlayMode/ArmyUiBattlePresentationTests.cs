using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.CommandProcessors;
using Assets.Scripts.Managers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Wism.Client.Commands.Armies;
using Wism.Client.Commands.Players;
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator TurnAnnouncement_HumanClickDismissesWithoutIssuingMapOrder()
    {
        var endingPlayer = Game.Current.GetCurrentPlayer();
        var enteringPlayer = Game.Current.GetNextPlayer();
        var notification = GameObject.FindGameObjectWithTag("NotificationBox").GetComponent<Text>();
        unity.InteractiveUI = true;
        manager.EndTurn();
        yield return WaitFor(() => input.InputMode == InputMode.WaitForKey &&
            notification.text.Contains("your turn is starting"));
        Assert.That(Game.Current.GetCurrentPlayer(), Is.SameAs(enteringPlayer));
        Assert.That(Game.Current.GetCurrentPlayer(), Is.Not.SameAs(endingPlayer));
        var announcement = notification.text;
        var priorPressFrame = input.LastPrimaryActionFrame;
        yield return new WaitForSecondsRealtime(Assets.Scripts.UI.NotificationBox.DefaultInterval + 0.1f);
        Assert.That(notification.text, Is.EqualTo(announcement));
        Assert.That(Game.Current.GameState, Is.EqualTo(GameState.StartingTurn));
        yield return Click(ScreenPoint(enteringPlayer.Capitol.Tile));
        yield return WaitFor(() => input.InputMode != InputMode.WaitForKey && notification.text != announcement);
        Assert.That(input.LastPrimaryActionFrame, Is.EqualTo(priorPressFrame));
    }

    [UnityTest]
    public IEnumerator TurnAnnouncement_AiDismissesAutomatically()
    {
        yield return VerifyTimedTurnAnnouncement(false);
    }

    [UnityTest]
    public IEnumerator TurnAnnouncement_EliminationShowsOnceAtStartOfTurn()
    {
        yield return VerifyTimedTurnAnnouncement(true);
    }

    private IEnumerator VerifyTimedTurnAnnouncement(bool eliminated)
    {
        unity.enabled = false;
        unity.InteractiveUI = true;
        var player = Game.Current.GetCurrentPlayer();
        player.IsHuman = false;
        if (eliminated)
        {
            Game.Current.DeselectArmies();
            foreach (var city in player.GetCities().ToArray())
                player.RazeCity(city);
            Assert.That(player.IsDead, Is.False);
        }

        input.SetInputMode(InputMode.AITurn);
        var processor = new StartTurnProcessor(manager.LoggerFactory, unity);
        var command = new StartTurnCommand(manager.ControllerProvider.GameController, player);
        var notification = GameObject.FindGameObjectWithTag("NotificationBox").GetComponent<Text>();
        var result = processor.Execute(command);
        Assert.That(result, Is.EqualTo(ActionState.InProgress));
        Assert.That(notification.text, Does.Contain(eliminated ? "the war is over" : "your turn is starting"));
        Assert.That(player.IsDead, Is.EqualTo(eliminated));
        Assert.That(input.InputMode, Is.EqualTo(InputMode.AITurn));

        yield return new WaitForSecondsRealtime(0.1f);
        Assert.That(processor.Execute(command), Is.EqualTo(ActionState.InProgress));
        yield return WaitFor(() =>
        {
            result = processor.Execute(command);
            return result != ActionState.InProgress;
        });
        Assert.That(result, Is.EqualTo(ActionState.Succeeded));
        Assert.That(notification.text, Is.Empty);
        Assert.That(player.IsDead, Is.EqualTo(eliminated));
    }

    [UnityTest]
    public IEnumerator BattlePresentation_AiVsAi_ShowsWarningArmiesAndCasualty()
        => ExerciseBattlePresentation(false, false, true, true);

    [UnityTest]
    public IEnumerator BattlePresentation_AiVsHuman_PreservesSequence()
        => ExerciseBattlePresentation(false, true, true, true);

    [UnityTest]
    public IEnumerator BattlePresentation_HumanVsAi_PreservesSequence()
        => ExerciseBattlePresentation(true, false, true, true);

    [UnityTest]
    public IEnumerator BattlePresentation_HumanAttackerLossShowsDefeat()
        => ExerciseBattlePresentation(true, false, true, true, defenderWins: true);

    [UnityTest]
    public IEnumerator BattlePresentation_HumanDefenseWinShowsVictory()
        => ExerciseBattlePresentation(false, true, true, true, defenderWins: true);

    [UnityTest]
    public IEnumerator BattlePresentation_NoninteractiveAiVsAi_ResolvesSilently()
        => ExerciseBattlePresentation(false, false, false, true);

    [UnityTest]
    public IEnumerator BattlePresentation_NoninteractiveHumanVsAi_ResolvesSilently()
        => ExerciseBattlePresentation(true, false, false, true);

    [UnityTest]
    public IEnumerator BattlePresentation_AiCapturesEmptyCity_CleansUpWithoutCasualty()
        => ExerciseBattlePresentation(false, false, true, false);

    [UnityTest]
    public IEnumerator BattlePresentation_OptionOff_SkipsAiVsAi()
        => ExerciseBattlePresentation(false, false, true, true, false);

    [UnityTest]
    public IEnumerator BattlePresentation_OptionOff_StillShowsAiVsHuman()
        => ExerciseBattlePresentation(false, true, true, true, false);

    [UnityTest]
    public IEnumerator BattlePresentation_OptionOff_StillShowsHumanVsAi()
        => ExerciseBattlePresentation(true, false, true, true, false);

    [UnityTest]
    public IEnumerator BattlePresentation_OptionSurvivesSaveLoadAndOldSavesDefaultOn()
    {
        Assert.That(unity.ShowAiCombat, Is.True);
        Assert.That(new Assets.Scripts.UnityGame.Persistance.Entities.UnityNewGameEntity().ShowAiCombat, Is.True);
        var oldSnapshot = PersistanceManager.GetLastSnapshot();
        var filename = "battle-setting-proof-" + System.Guid.NewGuid().ToString("N") + ".json";
        var path = System.IO.Path.Combine(Application.persistentDataPath, filename);
        try
        {
            PersistanceManager.Save(filename, "Legacy combat setting", unity);
            var legacy = Newtonsoft.Json.Linq.JObject.Parse(System.IO.File.ReadAllText(path));
            legacy.Remove("SaveFormat");
            legacy.Remove("ShowAiCombat");
            System.IO.File.WriteAllText(path, legacy.ToString());
            Assert.That(PersistanceManager.LoadEntities(path, unity).ShowAiCombat, Is.True);
            foreach (var enabled in new[] { false, true })
            {
                unity.ShowAiCombat = enabled;
                PersistanceManager.Save(filename, "Combat setting", unity);
                PersistanceManager.LoadEntities(path, unity);
                unity.ShowAiCombat = !enabled;
                typeof(PersistanceManager).GetMethod("LoadLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                    .Invoke(null, new object[] { unity });
                Assert.That(unity.ShowAiCombat, Is.EqualTo(enabled));
            }
        }
        finally
        {
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            typeof(PersistanceManager).GetMethod("SetLastSnapshot", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                .Invoke(null, new object[] { oldSnapshot });
        }
        yield return null;
    }

    private IEnumerator ExerciseBattlePresentation(bool humanAttacker, bool humanDefender,
        bool interactive, bool defended, bool showAiCombat = true, bool defenderWins = false)
    {
        var attacker = hero.Player;
        var defender = Game.Current.Players[1];
        var city = defender.Capitol;
        Hero defendingHero = null;
        if (defended)
        {
            defender.HireHero(city.Tile);
            defendingHero = defender.GetArmies().OfType<Hero>().First();
            defendingHero.Strength = defenderWins ? 9 : 1;
        }
        hero.Strength = defenderWins ? 1 : 9;
        city.Defense = 0;
        manager.SelectArmies(new List<Army> { hero });
        yield return WaitFor(() => Game.Current.ArmiesSelected());
        manager.MoveSelectedArmies(city.X - 1, city.Y);
        yield return new Assets.Tests.PlayMode.WaitForLastCommand(manager.ControllerProvider);

        // Drive the real processors explicitly so autonomous planning cannot consume the fixture.
        unity.enabled = false;
        input.enabled = false;
        attacker.IsHuman = humanAttacker;
        defender.IsHuman = humanDefender;
        unity.InteractiveUI = interactive;
        unity.ShowAiCombat = showAiCombat;
        var shouldPresent = interactive && (showAiCombat || humanAttacker || humanDefender);
        manager.WarTime = 0.1f;
        var warScene = UnityUtilities.GameObjectHardFind("War!");
        var notification = GameObject.FindGameObjectWithTag("NotificationBox")
            .GetComponent<Assets.Scripts.UI.NotificationBox>();
        var notificationText = notification.GetComponent<Text>();
        notification.ClearNotification();
        var armyController = manager.ControllerProvider.ArmyController;
        var armies = Game.Current.GetSelectedArmies();
        var prepare = new PrepareForBattleCommand(armyController, armies, city.X, city.Y);
        var attack = new AttackOnceCommand(armyController, armies, city.X, city.Y);
        var complete = new CompleteBattleCommand(armyController, attack);
        var prepareProcessor = new PrepareForBattleProcessor(manager.LoggerFactory, unity);
        var battleProcessor = new BattleProcessor(manager.LoggerFactory, unity);
        var completeProcessor = (Wism.Client.CommandProcessors.ICommandProcessor)System.Activator.CreateInstance(
            typeof(PrepareForBattleProcessor).Assembly.GetType("Assets.Scripts.CommandProcessors.CompleteBattleProcessor"),
            manager.LoggerFactory, unity);
        var before = State();
        var originalStep = Time.fixedDeltaTime;
        var camera = Camera.main;
        var follow = camera.GetComponent<CameraFollow>();
        var attackerPosition = unity.WorldTilemap.ConvertGameToUnityVector(hero.X, hero.Y);
        follow.target = null;
        follow.SetCameraTarget(attackerPosition + new Vector3(-3f, 0f, 0f));
        try
        {
            Assert.That(unity.WarPanel.gameObject.activeSelf, Is.False);
            var result = prepareProcessor.Execute(prepare);
            if (shouldPresent)
            {
                Assert.That(result, Is.EqualTo(ActionState.InProgress), "The warning must precede combat.");
                Assert.That(State(), Is.EqualTo(before), "Presentation must not resolve combat early.");
                var notifiedPlayer = defended ? defender : attacker;
                Assert.That(notificationText.text, Does.StartWith(notifiedPlayer.Clan.DisplayName));
                Assert.That(notificationText.text, Does.Contain("being attacked!"));
                Assert.That(warScene.activeSelf, Is.True);
                Assert.That(unity.WarPanel.gameObject.activeSelf, Is.False);
                yield return WaitFor(() =>
                {
                    if (result == ActionState.InProgress) result = prepareProcessor.Execute(prepare);
                    return result != ActionState.InProgress;
                });
                Assert.That(unity.WarPanel.gameObject.activeSelf, Is.True);
                if (humanAttacker || unity.ObserveAiMovement)
                    Assert.That(Vector2.Distance(camera.WorldToViewportPoint(attackerPosition),
                        new Vector2(0.5f, 0.5f)), Is.LessThan(0.01f), "Frame the attacker before showing the battle panel.");
                Assert.That(BattleClones(unity.WarPanel.AttackerPrefab), Is.EqualTo(1));
                Assert.That(BattleClones(unity.WarPanel.DefenderPrefab), Is.EqualTo(defended ? 1 : 0));
            }
            else
            {
                Assert.That(notificationText.text, Is.Empty);
                Assert.That(warScene.activeSelf, Is.False);
                Assert.That(unity.WarPanel.gameObject.activeSelf, Is.False);
            }
            Assert.That(result, Is.EqualTo(ActionState.Succeeded));
            Assert.That(input.InputMode, Is.EqualTo(humanAttacker ? InputMode.Game : InputMode.AITurn));
            if (defenderWins) Game.Current.Random = new DefenderVictoryRolls();
            var expectedResult = defenderWins ? ActionState.Failed : ActionState.Succeeded;
            Assert.That(battleProcessor.Execute(attack), Is.EqualTo(expectedResult));
            Assert.That(hero.IsDead, Is.EqualTo(defenderWins));
            if (defended) Assert.That(defendingHero.IsDead, Is.EqualTo(!defenderWins));
            Assert.That(BattleClones(unity.WarPanel.KilledPrefab), Is.EqualTo(shouldPresent && defended ? 1 : 0));
            Assert.That(completeProcessor.Execute(complete), Is.EqualTo(expectedResult));
            Assert.That(city.Player, Is.SameAs(defenderWins ? defender : attacker));
            var winner = defenderWins ? defender : attacker;
            var loser = defenderWins ? attacker : defender;
            var expectedOutcome = winner.IsHuman ? "You are victorious!" :
                loser.IsHuman ? "You have been defeated." : winner.Clan.DisplayName + " won the battle.";
            Assert.That(notificationText.text, Is.EqualTo(shouldPresent ? expectedOutcome : string.Empty));
            Assert.That(unity.WarPanel.gameObject.activeSelf, Is.False);
            Assert.That(warScene.activeSelf, Is.False);
            if (!humanAttacker) Assert.That(input.InputMode, Is.EqualTo(InputMode.AITurn));
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.That(BattleClones(unity.WarPanel.AttackerPrefab), Is.Zero);
            Assert.That(BattleClones(unity.WarPanel.DefenderPrefab), Is.Zero);
            Assert.That(BattleClones(unity.WarPanel.KilledPrefab), Is.Zero);
        }
        finally
        {
            attacker.IsHuman = defender.IsHuman = true;
            unity.InteractiveUI = false;
            Time.fixedDeltaTime = originalStep;
        }
    }

    private sealed class DefenderVictoryRolls : System.Random
    {
        private bool attackerRoll = true;

        public override int Next(int minValue, int maxValue)
        {
            var roll = this.attackerRoll ? minValue : maxValue - 1;
            this.attackerRoll = !this.attackerRoll;
            return roll;
        }
    }

    private int BattleClones(GameObject prefab) => unity.WarPanel.GetComponentsInChildren<Image>(true)
        .Count(image => image.gameObject != prefab && image.gameObject.name == prefab.name + "(Clone)");
}
