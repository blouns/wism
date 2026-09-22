using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using Assets.Scripts.Telemetry;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using Wism.Client.Core;

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator PerformanceDiagnostics_RecordsDecisionCommandsFramesAndReplayCheckpoint()
    {
        unity.enabled = false;
        var diagnostics = PlaytestPerformance.Instance ?? new GameObject("Diagnostic proof").AddComponent<PlaytestPerformance>();
        Assert.That(diagnostics.Recording, Is.True);
        string output = diagnostics.OutputDirectory;
        Game.Current.GetCurrentPlayer().IsHuman = false;
        // Bind the newly-created recorder when this fixture did not start with profiling enabled.
        typeof(Assets.Scripts.Managers.UnityManager).GetMethod("InitializeAI", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unity, null);
        InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F9, Key.F10));
        InputSystem.Update();
        for (int i = 0; i < 12; i++)
            typeof(Assets.Scripts.Managers.UnityManager).GetMethod("DoTasks", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unity, null);
        typeof(Assets.Scripts.Managers.UnityManager).GetMethod("GenerateAICommands", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unity, null);
        for (int i = 0; i < 12; i++)
            typeof(Assets.Scripts.Managers.UnityManager).GetMethod("DoTasks", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(unity, null);
        yield return null;
        yield return null;
        Object.DestroyImmediate(diagnostics.gameObject);
        var rows = File.ReadLines(Path.Combine(output, "samples.jsonl")).Select(JObject.Parse).ToArray();
        Assert.That(rows.Any(r => (string)r["Kind"] == "decision-start"), Is.True);
        var context = JObject.Parse((string)rows.First(r => (string)r["Kind"] == "decision-start")["Detail"]);
        int neutral = World.Current.GetCities().Count(c => c.Player == null || c.Clan.ShortName == "Neutral");
        Assert.That(neutral, Is.GreaterThan(0), "Fixture must contain neutral cities.");
        Assert.That((int)context["NeutralCities"], Is.EqualTo(neutral));
        Assert.That((int)context["OwnedCities"], Is.EqualTo(World.Current.GetCities().Count - neutral));
        Assert.That(rows.Any(r => (string)r["Kind"] == "decision-end"), Is.True);
        Assert.That(rows.Any(r => (string)r["Kind"] == "scope" && (string)r["Name"] == "ai-decision"), Is.True);
        Assert.That(rows.Any(r => (string)r["Kind"] == "timing-alias"), Is.True);
        Assert.That(rows.Any(r => (string)r["Kind"] == "command-attempt" && (long)r["Decision"] > 0), Is.True);
        Assert.That(rows.Any(r => (string)r["Kind"] == "frame"), Is.True);
        Assert.That(rows.Any(r => (string)r["Kind"] == "bookmark" && r["InputEventSeconds"].Type != JTokenType.Null), Is.True);
        Assert.That(Directory.GetFiles(output, "checkpoint-*.json").Length, Is.EqualTo(1));
        Assert.That(Directory.GetFiles(output, "*.raw").Any(p => new FileInfo(p).Length > 0), Is.True);
        Assert.That((long)JObject.Parse(File.ReadAllText(Path.Combine(output, "receipt.json")))["Dropped"], Is.Zero);
    }
}
