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
using Wism.Client.Controllers;
using Wism.Client.Core;
using Wism.Client.MapObjects;

public sealed partial class ArmyUiInputTests
{
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
            System.IO.File.WriteAllText(path, "{}");
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
        bool interactive, bool defended, bool showAiCombat = true)
    {
        var attacker = hero.Player;
        var defender = Game.Current.Players[1];
        var city = defender.Capitol;
        Hero defendingHero = null;
        if (defended)
        {
            defender.HireHero(city.Tile);
            defendingHero = defender.GetArmies().OfType<Hero>().First();
            defendingHero.Strength = 1;
        }
        hero.Strength = 9;
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
        var prepareProcessor = new PrepareForBattleProcessor(manager.LoggerFactory, unity);
        var battleProcessor = new BattleProcessor(manager.LoggerFactory, unity);
        var completeProcessor = (Wism.Client.CommandProcessors.ICommandProcessor)System.Activator.CreateInstance(
            typeof(PrepareForBattleProcessor).Assembly.GetType("Assets.Scripts.CommandProcessors.CompleteBattleProcessor"),
            manager.LoggerFactory, unity);
        var before = State();
        var originalStep = Time.fixedDeltaTime;
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
            Assert.That(battleProcessor.Execute(attack), Is.EqualTo(ActionState.Succeeded));
            Assert.That(hero.IsDead, Is.False);
            if (defended) Assert.That(defendingHero.IsDead, Is.True);
            Assert.That(BattleClones(unity.WarPanel.KilledPrefab), Is.EqualTo(shouldPresent && defended ? 1 : 0));
            Assert.That(completeProcessor.Execute(new CompleteBattleCommand(armyController, attack)),
                Is.EqualTo(ActionState.Succeeded));
            Assert.That(city.Player, Is.SameAs(attacker));
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

    private int BattleClones(GameObject prefab) => unity.WarPanel.GetComponentsInChildren<Image>(true)
        .Count(image => image.gameObject != prefab && image.gameObject.name == prefab.name + "(Clone)");
}
