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

public sealed class StrategicReportModelTests
{
    [Test]
    public void WinningUsesDocumentedSharesAndExcludesEliminatedClans()
    {
        var rows = new[] {
            new ClanReportRow("A", "white", 2, 7, 6, 100, false),
            new ClanReportRow("B", "red", 2, 1, 2, 100, false),
            new ClanReportRow("Out", "black", 100, 100, 100, 10000, true) };
        Assert.That(StrategicReportData.Values(rows, StrategicReportKind.Winning), Is.EqualTo(new[] { 58.75, 41.25, 0 }));
        Assert.That(StrategicReportData.Values(rows, StrategicReportKind.Armies), Is.EqualTo(new double[] { 7, 1, 100 }));
        Assert.That(StrategicReportData.Values(rows, StrategicReportKind.Gold), Is.EqualTo(new double[] { 100, 100, 10000 }));
    }

    [Test]
    public void WinningIsFiniteForZeroAndEmptyRosters()
    {
        Assert.That(StrategicReportData.Values(new ClanReportRow[0], StrategicReportKind.Winning), Is.Empty);
        var rows = new[] { new ClanReportRow("Zero", "white", 0, 0, 0, -3, false) };
        Assert.That(StrategicReportData.Values(rows, StrategicReportKind.Winning), Is.EqualTo(new double[] { 0 }));
    }

    [TestCase(4, 3, 100)]
    [TestCase(3, 4, 100)]
    [TestCase(3, 3, 101)]
    public void IncreasingOneComponentNeverReducesTheLeaderEstimate(double cities, double strength, double gold)
    {
        var other = new ClanReportRow("B", "red", 3, 2, 3, 100, false);
        var rows = new[] { new ClanReportRow("A", "white", cities, 2, strength, gold, false), other };
        Assert.That(StrategicReportData.Values(rows, StrategicReportKind.Winning)[0], Is.GreaterThan(50));
    }
}

public sealed partial class ArmyUiInputTests
{
    [UnityTest]
    public IEnumerator StrategicReports_ShowExactTotalsWithoutOrdersOrClickThrough()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        yield return Click(ScreenPoint(hero.Tile));
        yield return new WaitForLastCommand(manager.ControllerProvider);
        var before = TerminalState();
        int command = unity.LastCommandId;
        yield return Click(MenuPoint("OpenReports"));
        var report = unity.GameMenu.Reports;
        Assert.That(report.gameObject.activeInHierarchy, Is.True);
        Assert.That(report.Values, Is.EqualTo(Game.Current.Players.Select(player => (double)player.GetCities().Count).ToArray()));
        foreach (var kind in new[] { StrategicReportKind.Armies, StrategicReportKind.Gold, StrategicReportKind.Winning })
        {
            yield return PressJourneyButton(MenuButton("Report" + kind));
            Assert.That(report.Kind, Is.EqualTo(kind));
            Assert.That(report.Values, Is.EqualTo(StrategicReportData.Values(StrategicReportData.Capture(Game.Current), kind)));
            yield return PressJourneyKey(Key.E);
            yield return PressJourneyKey(Key.Q);
            Assert.That(TerminalState(), Is.EqualTo(before));
        }
        report.Show(StrategicReportKind.Armies);
        int tiles = Game.Current.Players.Sum(player => player.GetArmies().Where(army => !army.IsDead && army.Tile != null).Select(army => army.Tile).Distinct().Count());
        Assert.That(report.GetComponentInChildren<MinimapArmyOverlay>().MarkerCount, Is.EqualTo(tiles));
        yield return PressJourneyButton(MenuButton("CloseReport"));
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(input.InputMode, Is.EqualTo(InputMode.Game));
        Assert.That(unity.LastCommandId, Is.EqualTo(command));
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest]
    public IEnumerator StrategicReports_RefreshOnReopenAndWorkInInspection()
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        unity.GameMenu.OpenReports(StrategicReportKind.Gold);
        var player = Game.Current.GetCurrentPlayer();
        int original = player.Gold;
        unity.GameMenu.Close();
        player.Gold += 7;
        unity.GameMenu.OpenReports(StrategicReportKind.Gold);
        Assert.That(unity.GameMenu.Reports.Values[0], Is.EqualTo(original + 7));
        unity.GameMenu.Close();
        Game.Current.Transition(GameState.GameOver);
        yield return new WaitForFixedUpdate();
        var before = TerminalState();
        unity.GameMenu.OpenReports(StrategicReportKind.Winning);
        for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
        Assert.That(unity.GameMenu.Reports.gameObject.activeInHierarchy, Is.True);
        yield return PressJourneyKey(Key.Escape);
        Assert.That(unity.GameMenu.IsOpen, Is.False);
        Assert.That(TerminalState(), Is.EqualTo(before));
    }

    [UnityTest] public IEnumerator StrategicReports_Viewport1024() => ReportsViewport(1024, 768);
    [UnityTest] public IEnumerator StrategicReports_Viewport1280() => ReportsViewport(1280, 720);
    [UnityTest] public IEnumerator StrategicReports_Viewport1920() => ReportsViewport(1920, 1080);
    [UnityTest] public IEnumerator StrategicReports_ViewportUltrawide() => ReportsViewport(2560, 1080);

    private IEnumerator ReportsViewport(int width, int height)
    {
        yield return new WaitForLastCommand(manager.ControllerProvider);
        AssertBatchWindowless();
        int oldWidth = Screen.width, oldHeight = Screen.height;
        try
        {
            Screen.SetResolution(width, height, false);
            yield return null;
            yield return null;
            yield return Tap(MenuPoint("OpenReports"));
            var report = unity.GameMenu.Reports;
            Assert.That(report.gameObject.activeInHierarchy, Is.True);
            var rows = Enumerable.Range(0, 14).Select(i => new ClanReportRow(
                "Clan " + i + " of the Distant Northern Territories", i % 2 == 0 ? "(242, 200, 75)" : "(67, 170, 224)", i, i, i, i * 500, i == 13)).ToArray();
            report.Render(rows, StrategicReportKind.Cities);
            yield return null;
            Canvas.ForceUpdateCanvases();
            var map = report.GetComponentInChildren<RawImage>();
            Assert.That(map.texture, Is.Not.Null);
            Assert.That(map.rectTransform.rect.width / map.rectTransform.rect.height,
                Is.EqualTo((float)World.Current.Map.GetLength(0) / World.Current.Map.GetLength(1)).Within(.001f));
            var corners = new Vector3[4];
            map.rectTransform.GetWorldCorners(corners);
            Assert.That(corners.All(point => point.x >= 0 && point.y >= 0 && point.x <= Screen.width && point.y <= Screen.height), Is.True);
            var scrolling = report.GetComponentInChildren<ScrollRect>();
            Assert.That(scrolling.content.rect.height, Is.GreaterThan(scrolling.viewport.rect.height));
            Assert.That(scrolling.content.GetComponentsInChildren<Text>().Count(text => text.name == "Clan"), Is.EqualTo(14));
            Assert.That(scrolling.verticalScrollbar.gameObject.activeInHierarchy, Is.True);
            var bars = scrolling.content.GetComponentsInChildren<Image>().Where(image => image.name == "ClanBar").ToArray();
            Assert.That(bars[1].color, Is.EqualTo((Color)new Color32(67, 170, 224, 255)));
            CaptureUiCanvas(unity.GameMenu.GetComponentInChildren<Canvas>(), "strategic-reports", width, height);
            yield return null;
            scrolling.OnScroll(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current) { scrollDelta = new Vector2(0, -100) });
            yield return null;
            Assert.That(scrolling.verticalNormalizedPosition, Is.LessThan(.01f));
            yield return Tap(MenuPoint("CloseReport"));
            Assert.That(unity.GameMenu.IsOpen, Is.False);
        }
        finally { Screen.SetResolution(oldWidth, oldHeight, false); }
    }
}
