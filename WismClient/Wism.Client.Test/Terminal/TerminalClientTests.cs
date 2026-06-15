using System;
using System.IO;
using NUnit.Framework;
using Wism.Client.Terminal.Cli;
using Wism.Client.Terminal.Game;
using Wism.Client.Terminal.Input;
using Wism.Client.Terminal.Rendering;
using WismGame = Wism.Client.Core.Game;

namespace Wism.Client.Test.Terminal;

[TestFixture]
[NonParallelizable]
public sealed class TerminalClientTests
{
    private string originalDirectory = string.Empty;
    private string recordingRoot = string.Empty;

    [SetUp]
    public void SetUp()
    {
        originalDirectory = Environment.CurrentDirectory;
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        recordingRoot = Path.Combine(
            TestContext.CurrentContext.WorkDirectory,
            "terminal-records",
            Guid.NewGuid().ToString("N"));
    }

    [TearDown]
    public void TearDown()
    {
        if (!string.IsNullOrWhiteSpace(originalDirectory))
        {
            Environment.CurrentDirectory = originalDirectory;
        }

        if (!string.IsNullOrWhiteSpace(recordingRoot) && Directory.Exists(recordingRoot))
        {
            Directory.Delete(recordingRoot, recursive: true);
        }
    }

    [Test]
    public void TerminalCommand_Parse_DefaultsToPlay()
    {
        var command = TerminalCommand.Parse(Array.Empty<string>());
        var options = TerminalLaunchOptions.From(command);

        Assert.That(command.Name, Is.EqualTo("play"));
        Assert.That(command.Positionals, Is.Empty);
        Assert.That(options.ProfileId, Is.EqualTo("classic-warlords"));
        Assert.That(options.PackIds, Is.Null);
    }

    [Test]
    public void TerminalLaunchOptions_From_PreservesClassicWarlordsDefaultsAndFlags()
    {
        var command = TerminalCommand.Parse(new[]
        {
            "new",
            "profile=classic-warlords",
            "world=Illuria",
            "packs=a,b",
            "clans=8",
            "mode=detailed",
            "--agent",
            "--json",
            "--no-color",
            "--no-animation",
            "out=transcript.jsonl",
            "record=records"
        });

        var options = TerminalLaunchOptions.From(command);

        Assert.That(command.Name, Is.EqualTo("new"));
        Assert.That(options.ProfileId, Is.EqualTo("classic-warlords"));
        Assert.That(options.World, Is.EqualTo("Illuria"));
        Assert.That(options.PackIds, Is.EqualTo(new[] { "a", "b" }));
        Assert.That(options.ClanCount, Is.EqualTo(8));
        Assert.That(options.TileMode, Is.EqualTo(TileRenderMode.Detailed));
        Assert.That(options.Agent, Is.True);
        Assert.That(options.Json, Is.True);
        Assert.That(options.NoColor, Is.True);
        Assert.That(options.NoAnimation, Is.True);
        Assert.That(options.OutputPath, Is.EqualTo("transcript.jsonl"));
        Assert.That(options.RecordRoot, Is.EqualTo("records"));
    }

    [Test]
    public void Viewport_CentersAndClampsIlluriaSizedMaps()
    {
        var viewport = new Viewport(109, 156);

        viewport.Resize(20, 10);
        viewport.CenterOn(108, 155);

        Assert.That(viewport.CursorX, Is.EqualTo(108));
        Assert.That(viewport.CursorY, Is.EqualTo(155));
        Assert.That(viewport.X, Is.EqualTo(89));
        Assert.That(viewport.Y, Is.EqualTo(146));
        Assert.That(viewport.MapYForRow(0), Is.EqualTo(155));
        Assert.That(viewport.MapYForRow(9), Is.EqualTo(146));
        Assert.That(viewport.Contains(108, 155), Is.True);
    }

    [Test]
    public void TerminalFrame_ProducesSemanticPlainText()
    {
        var frame = new TerminalFrame(6, 3);

        frame.WriteText(1, 1, "W!", ConsoleColor.Cyan);

        Assert.That(frame[1, 1].Glyph, Is.EqualTo('W'));
        Assert.That(frame[2, 1].Glyph, Is.EqualTo('!'));
        Assert.That(frame[1, 1].Foreground, Is.EqualTo(ConsoleColor.Cyan));
        Assert.That(frame.ToPlainText(), Does.Contain(" W!   "));
    }

    [Test]
    public void TerminalSession_LoadsIlluriaAndRendersViewport()
    {
        var session = TerminalGameSession.Create(new TerminalLaunchOptions
        {
            World = "Illuria",
            RecordRoot = recordingRoot,
            NoColor = true
        });

        try
        {
            Assert.That(session.MapWidth, Is.EqualTo(109));
            Assert.That(session.MapHeight, Is.EqualTo(156));

            var viewport = new Viewport(session.MapWidth, session.MapHeight);
            var selected = WismGame.Current.GetSelectedArmies();
            if (selected is { Count: > 0 })
            {
                viewport.CenterOn(selected[0].X, selected[0].Y);
            }

            var frame = new TerminalMapRenderer().Render(
                session,
                viewport,
                width: 100,
                height: 32,
                new RenderOptions { NoColor = true });
            var text = frame.ToPlainText();

            Assert.That(text, Does.Contain("WISM Terminal"));
            Assert.That(text, Does.Contain("INSPECTOR"));
            Assert.That(text, Does.Contain("MINIMAP"));
        }
        finally
        {
            session.CompleteRecording();
        }
    }

    [Test]
    public void TerminalInputActions_MoveCursor_DoesNotMoveSelectedArmy()
    {
        var session = TerminalGameSession.Create(new TerminalLaunchOptions
        {
            World = "Illuria",
            RecordRoot = recordingRoot,
            NoColor = true
        });

        try
        {
            var selected = WismGame.Current.GetSelectedArmies();

            Assert.That(selected, Is.Not.Null);
            Assert.That(selected, Has.Count.GreaterThan(0));

            var army = selected![0];
            var originalX = army.X;
            var originalY = army.Y;
            var viewport = new Viewport(session.MapWidth, session.MapHeight);
            viewport.CenterOn(originalX, originalY);
            var follow = true;

            TerminalInputActions.MoveCursor(viewport, 1, 0, ref follow);

            Assert.That(viewport.CursorX, Is.EqualTo(originalX + 1));
            Assert.That(viewport.CursorY, Is.EqualTo(originalY));
            Assert.That(army.X, Is.EqualTo(originalX));
            Assert.That(army.Y, Is.EqualTo(originalY));
            Assert.That(follow, Is.False);
        }
        finally
        {
            session.CompleteRecording();
        }
    }
}
