using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Modules.Profiles;

namespace Wism.Client.Test.Unit;

[TestFixture]
public sealed class ModKitValidatorTests
{
    [Test]
    public void DefaultModKit_ValidatesCleanly()
    {
        var report = ModKitValidator.Validate(TestContext.CurrentContext.TestDirectory);

        Assert.That(report.IsValid, Is.True, string.Join(Environment.NewLine, report.Issues));
    }

    [Test]
    public void FlavorOverlay_WithUnknownStableId_ReturnsActionableError()
    {
        var root = CreateFixture(
            "pack-bad-flavor",
            "{\"id\":\"pack-bad-flavor\",\"displayName\":\"Bad Flavor\",\"kind\":\"Flavor\",\"overlay\":\"overlays/mod-overlay.json\"}",
            ("FeaturePacks/pack-bad-flavor/overlays/mod-overlay.json",
                "{\"clans\":[{\"shortName\":\"MissingClan\",\"displayName\":\"Ghost Clan\"}]}"));

        var report = ModKitValidator.Validate(root);

        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues.Any(issue =>
            issue.Code == "flavor-clan-unknown"
            && issue.Message.Contains("MissingClan")), Is.True);
    }

    [Test]
    public void ModePack_WithMissingWorld_ReturnsActionableError()
    {
        var root = CreateFixture(
            "pack-bad-mode",
            "{\"id\":\"pack-bad-mode\",\"displayName\":\"Bad Mode\",\"kind\":\"Mode\",\"launch\":{\"world\":\"NoSuchWorld\",\"seed\":1,\"clans\":2,\"maxTurns\":4,\"scenario\":\"standard\"}}");

        var report = ModKitValidator.Validate(root);

        Assert.That(report.IsValid, Is.False);
        Assert.That(report.Issues.Any(issue =>
            issue.Code == "mode-launch-world-missing"
            && issue.Message.Contains("NoSuchWorld")), Is.True);
    }

    private static string CreateFixture(string packId, string packJson, params (string Path, string Content)[] extraFiles)
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "mod-kit-fixture-" + Guid.NewGuid().ToString("N"));
        var mod = Path.Combine(root, "WismClient", "Wism.Client.Core", "mod");

        Write(mod, "Clan.json", "[{\"shortName\":\"Sirians\",\"displayName\":\"The Sirians\"}]");
        Write(mod, "Army.json", "[{\"shortName\":\"LightInfantry\",\"displayName\":\"Light Infantry\"}]");
        Write(mod, "Artifact.json", "[{\"shortName\":\"Firesword\",\"displayName\":\"Firesword\"}]");
        Write(mod, "Worlds/TestWorld/City.json", "[]");
        Write(mod, "Worlds/TestWorld/Location.json", "[]");
        Write(mod, "Profiles/classic-warlords/profile.json",
            "{\"id\":\"classic-warlords\",\"displayName\":\"Classic Warlords\",\"baseWorld\":\"TestWorld\",\"modeId\":\"classic\",\"enabledPacks\":[],\"modRoot\":\"mod\"}");
        Write(mod, $"FeaturePacks/{packId}/pack.json", packJson);

        foreach (var file in extraFiles)
        {
            Write(mod, file.Path, file.Content);
        }

        return root;
    }

    private static void Write(string root, string relativePath, string content)
    {
        var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }
}
