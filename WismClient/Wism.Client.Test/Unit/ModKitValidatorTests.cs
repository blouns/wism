using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Data.Entities;
using Wism.Client.Modules.Profiles;

namespace Wism.Client.Test.Unit;

[TestFixture]
public sealed class ModKitValidatorTests
{
    [TestCase("(0, 127, 255)", true)]
    [TestCase(" ( 30, 180, 90 ) ", true)]
    [TestCase("(256, 0, 0)", false)]
    [TestCase("(-1, 0, 0)", false)]
    [TestCase("(0, 0)", false)]
    [TestCase("red", false)]
    [TestCase(null, false)]
    public void ClanColors_AreStrictRgb(string value, bool valid)
    {
        Assert.That(Wism.Client.Modules.Infos.ClanInfo.TryParseRgb(value, out _, out _, out _), Is.EqualTo(valid));
    }

    [Test]
    public void ClanPresentation_DefaultsPreserveLegacyIdentity()
    {
        var clan = Newtonsoft.Json.JsonConvert.DeserializeObject<Wism.Client.Modules.Infos.ClanInfo>("{\"ShortName\":\"NewClan\"}");
        Assert.That(clan!.Playable, Is.True);
        Assert.That(clan.ArtworkClanName, Is.EqualTo("NewClan"));
        clan.VisualClanName = "Sirians";
        Assert.That(clan.ArtworkClanName, Is.EqualTo("Sirians"));
        Assert.That(clan.ShortName, Is.EqualTo("NewClan"));
    }

    [Test]
    public void ClanPresentation_RuntimeSerializerKeepsLegacyClansPlayable()
    {
        var serializer = new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Wism.Client.Modules.Infos.ClanInfo));
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("{\"ShortName\":\"Sirians\"}"));
        var clan = (Wism.Client.Modules.Infos.ClanInfo)serializer.ReadObject(stream)!;
        Assert.That(clan.Playable, Is.True);
        Assert.That(clan.Color, Is.EqualTo("(255, 0, 0)"));
    }

    [TestCase("{\"clans\":[{\"shortName\":\"Sirians\",\"displayName\":\"New name\",\"primaryColor\":\"(999,0,0)\"}]}", "clan-color-invalid")]
    [TestCase("{\"clans\":[null]}", "flavor-entry-invalid")]
    public void InvalidClanPresentation_IsRejected(string overlay, string code)
    {
        var root = CreateFixture("pack-colors", "{\"id\":\"pack-colors\",\"displayName\":\"Colors\",\"kind\":\"Flavor\",\"overlay\":\"overlays/mod-overlay.json\"}",
            ("FeaturePacks/pack-colors/overlays/mod-overlay.json", overlay));
        Assert.That(ModKitValidator.Validate(root).Issues.Any(issue => issue.Code == code), Is.True);
    }

    [TestCase("[null]", "clan-id-invalid")]
    [TestCase("[{\"ShortName\":\"Sirians\"},{\"ShortName\":\"sirians\"}]", "clan-id-invalid")]
    [TestCase("[{\"ShortName\":\"Sirians\",\"PrimaryColor\":\"(256,0,0)\"}]", "clan-color-invalid")]
    [TestCase("[{\"ShortName\":\"Sirians\",\"VisualClanName\":\"Missing\"}]", "clan-artwork-invalid")]
    [TestCase("[{\"ShortName\":\"Sirians\",\"VisualClanName\":\"Sirians\"}]", "clan-artwork-invalid")]
    public void InvalidBaseClanData_IsRejected(string clans, string code)
    {
        var root = CreateFixture("pack-labels", "{\"id\":\"pack-labels\",\"displayName\":\"Labels\",\"kind\":\"Flavor\",\"overlay\":\"labels.json\"}",
            ("Clan.json", clans), ("FeaturePacks/pack-labels/labels.json", "{}"));
        Assert.That(ModKitValidator.Validate(root).Issues.Any(issue => issue.Code == code), Is.True);
    }

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

    [Test]
    public void VersionedSelection_IsGreenVerified()
    {
        var report = ModKitSelectionService.VerifySelection(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "mod"),
            "classic-warlords",
            new[] { "pack-illurian-legends-flavor" },
            "TestWorld");

        Assert.That(report.IsGreen, Is.True, string.Join(Environment.NewLine, report.Issues.Select(issue => issue.Message)));
        Assert.That(report.Selection, Is.Not.Null);
        Assert.That(report.Selection!.ProfileId, Is.EqualTo("classic-warlords"));
        Assert.That(report.Selection.PackIds, Is.EqualTo(new[] { "pack-illurian-legends-flavor" }));
        Assert.That(report.Selection.ContentFingerprint, Is.Not.Empty);
    }

    [Test]
    public void MissingVersionMetadata_IsLoadableButNotGreen()
    {
        var root = CreateFixture(
            "pack-legacy",
            "{\"id\":\"pack-legacy\",\"displayName\":\"Legacy Pack\",\"kind\":\"Flavor\",\"overlay\":\"overlays/mod-overlay.json\"}",
            ("FeaturePacks/pack-legacy/overlays/mod-overlay.json",
                "{\"clans\":[{\"shortName\":\"Sirians\",\"displayName\":\"Legacy Name\"}]}"));
        var report = ModKitSelectionService.VerifySelection(
            Path.Combine(root, "WismClient", "Wism.Client.Core", "mod"),
            "classic-warlords",
            new[] { "pack-legacy" },
            "TestWorld");

        Assert.That(report.IsLoadable, Is.True);
        Assert.That(report.IsGreen, Is.False);
        Assert.That(report.Status, Is.EqualTo(ModKitCompatibilityStatus.Legacy));
        Assert.That(report.Issues.Any(issue => issue.Code == "profile-version-missing"), Is.True);
        Assert.That(report.Issues.Any(issue => issue.Code == "pack-version-missing"), Is.True);
    }

    [Test]
    public void UnsupportedMinVersion_BlocksSelection()
    {
        var root = CreateFixtureWithProfile(
            "pack-future",
            "{\"schemaVersion\":1,\"version\":\"1.0.0\",\"minWismVersion\":\"999.0.0\",\"id\":\"pack-future\",\"displayName\":\"Future Pack\",\"kind\":\"Flavor\",\"overlay\":\"overlays/mod-overlay.json\"}",
            "{\"schemaVersion\":1,\"version\":\"1.0.0\",\"minWismVersion\":\"0.1.0\",\"id\":\"classic-warlords\",\"displayName\":\"Classic Warlords\",\"baseWorld\":\"TestWorld\",\"modeId\":\"classic\",\"enabledPacks\":[],\"modRoot\":\"mod\"}",
            ("FeaturePacks/pack-future/overlays/mod-overlay.json",
                "{\"clans\":[{\"shortName\":\"Sirians\",\"displayName\":\"Future Name\"}]}"));
        var report = ModKitSelectionService.VerifySelection(
            Path.Combine(root, "WismClient", "Wism.Client.Core", "mod"),
            "classic-warlords",
            new[] { "pack-future" },
            "TestWorld");

        Assert.That(report.IsLoadable, Is.False);
        Assert.That(report.Status, Is.EqualTo(ModKitCompatibilityStatus.UnsupportedVersion));
        Assert.That(report.Issues.Any(issue => issue.Code == "mod-min-version-unsupported"), Is.True);
    }

    [Test]
    public void Fingerprint_ChangesWhenSelectedPackContentChanges()
    {
        var root = CreateFixtureWithProfile(
            "pack-flavor",
            "{\"schemaVersion\":1,\"version\":\"1.0.0\",\"minWismVersion\":\"0.1.0\",\"id\":\"pack-flavor\",\"displayName\":\"Flavor Pack\",\"kind\":\"Flavor\",\"overlay\":\"overlays/mod-overlay.json\"}",
            "{\"schemaVersion\":1,\"version\":\"1.0.0\",\"minWismVersion\":\"0.1.0\",\"id\":\"classic-warlords\",\"displayName\":\"Classic Warlords\",\"baseWorld\":\"TestWorld\",\"modeId\":\"classic\",\"enabledPacks\":[],\"modRoot\":\"mod\"}",
            ("FeaturePacks/pack-flavor/overlays/mod-overlay.json",
                "{\"clans\":[{\"shortName\":\"Sirians\",\"displayName\":\"First Name\"}]}"));
        var modRoot = Path.Combine(root, "WismClient", "Wism.Client.Core", "mod");
        var first = ModKitSelectionService.VerifySelection(modRoot, "classic-warlords", new[] { "pack-flavor" }, "TestWorld");

        Write(modRoot, "FeaturePacks/pack-flavor/overlays/mod-overlay.json",
            "{\"clans\":[{\"shortName\":\"Sirians\",\"displayName\":\"Second Name\"}]}");
        var second = ModKitSelectionService.VerifySelection(modRoot, "classic-warlords", new[] { "pack-flavor" }, "TestWorld");

        Assert.That(first.Selection!.ContentFingerprint, Is.Not.EqualTo(second.Selection!.ContentFingerprint));
    }

    private static string CreateFixture(
        string packId,
        string packJson,
        params (string Path, string Content)[] extraFiles)
    {
        return CreateFixtureCore(packId, packJson, null, extraFiles);
    }

    private static string CreateFixtureWithProfile(
        string packId,
        string packJson,
        string profileJson,
        params (string Path, string Content)[] extraFiles)
    {
        return CreateFixtureCore(packId, packJson, profileJson, extraFiles);
    }

    private static string CreateFixtureCore(
        string packId,
        string packJson,
        string profileJson,
        (string Path, string Content)[] extraFiles)
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "mod-kit-fixture-" + Guid.NewGuid().ToString("N"));
        var mod = Path.Combine(root, "WismClient", "Wism.Client.Core", "mod");

        Write(mod, "Clan.json", "[{\"shortName\":\"Sirians\",\"displayName\":\"The Sirians\"}]");
        Write(mod, "Army.json", "[{\"shortName\":\"LightInfantry\",\"displayName\":\"Light Infantry\"}]");
        Write(mod, "Artifact.json", "[{\"shortName\":\"Firesword\",\"displayName\":\"Firesword\"}]");
        Write(mod, "Terrain.json", "[]");
        Write(mod, "ClanTerrainModifier.json", "[]");
        Write(mod, "Worlds/TestWorld/City.json", "[]");
        Write(mod, "Worlds/TestWorld/Location.json", "[]");
        Write(mod, "Profiles/classic-warlords/profile.json",
            profileJson ?? "{\"id\":\"classic-warlords\",\"displayName\":\"Classic Warlords\",\"baseWorld\":\"TestWorld\",\"modeId\":\"classic\",\"enabledPacks\":[],\"modRoot\":\"mod\"}");
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
