using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Wism.Client.Modules;
using Wism.Client.Modules.Profiles;

namespace Wism.Client.Test.Unit;

[TestFixture]
public sealed class ModularProfileCatalogTests
{
    [SetUp]
    public void SetUp()
    {
        Environment.CurrentDirectory = TestContext.CurrentContext.TestDirectory;
        ModFactory.ModPath = "mod";
        ModFactory.ActiveFeaturePackIds = Array.Empty<string>();
        ModFactory.ResetCache();
    }

    [Test]
    public void ClassicProfile_ResolvesWithNoPacks()
    {
        var selection = ModularGameProfileCatalog.Resolve(TestContext.CurrentContext.TestDirectory);

        Assert.That(selection.Profile.Id, Is.EqualTo("classic-warlords"));
        Assert.That(selection.BaseWorld, Is.EqualTo("TestWorld"));
        Assert.That(selection.Packs, Is.Empty);
    }

    [Test]
    public void FeaturePackDiscovery_FindsTheBundledProofPacks()
    {
        var packs = ModularGameProfileCatalog.DiscoverPacks(TestContext.CurrentContext.TestDirectory);
        var packIds = packs.Select(pack => pack.Id).ToArray();

        Assert.That(packIds, Does.Contain("pack-dusklands-visual"));
        Assert.That(packIds, Does.Contain("pack-illurian-legends-flavor"));
        Assert.That(packIds, Does.Contain("pack-quick-clash-mode"));
        Assert.That(packIds, Is.Unique);
        Assert.That(packIds, Has.No.Member(null));
        Assert.That(packIds, Has.None.Empty);
    }

    [Test]
    public void ZeroPackLoad_PreservesClassicDisplayNames()
    {
        var sirian = ModFactory.FindClanInfo("Sirians");

        Assert.That(sirian.DisplayName, Is.EqualTo("The Sirians"));
    }

    [Test]
    public void FlavorPack_OverridesDisplayNamesButPreservesStableIds()
    {
        ModFactory.ActiveFeaturePackIds = new[] { "pack-illurian-legends-flavor" };
        ModFactory.ResetCache();

        var sirian = ModFactory.FindClanInfo("Sirians");
        var infantry = ModFactory.FindArmyInfo("LightInfantry");
        var artifact = ModFactory.FindArtifactInfo("Firesword");

        Assert.That(sirian.ShortName, Is.EqualTo("Sirians"));
        Assert.That(sirian.DisplayName, Is.EqualTo("Sirians of the Dawn"));
        Assert.That(infantry.ShortName, Is.EqualTo("LightInfantry"));
        Assert.That(infantry.DisplayName, Is.EqualTo("Border Spears"));
        Assert.That(artifact.ShortName, Is.EqualTo("Firesword"));
        Assert.That(artifact.DisplayName, Is.EqualTo("Emberblade"));
    }

    [Test]
    public void AllThreeProofPacks_AreStackable()
    {
        var selection = ModularGameProfileCatalog.Resolve(
            TestContext.CurrentContext.TestDirectory,
            "classic-warlords",
            new[]
            {
                "pack-dusklands-visual",
                "pack-illurian-legends-flavor",
                "pack-quick-clash-mode"
            });

        Assert.That(selection.PackIds, Is.EqualTo(new[]
        {
            "pack-dusklands-visual",
            "pack-illurian-legends-flavor",
            "pack-quick-clash-mode"
        }));
        Assert.That(selection.Launch.Seed, Is.EqualTo(20260604));
        Assert.That(selection.Launch.Scenario, Is.EqualTo("capture-pressure"));
    }

    [Test]
    public void ConflictingPacks_AreRejected()
    {
        var root = CreateConflictingPackFixture();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            ModularGameProfileCatalog.Resolve(root, "classic-warlords", new[] { "pack-a", "pack-b" }));

        Assert.That(ex!.Message, Does.Contain("conflicts"));
    }

    private static string CreateConflictingPackFixture()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "mod-conflict-fixture-" + Guid.NewGuid().ToString("N"));
        var mod = Path.Combine(root, "WismClient", "Wism.Client.Core", "mod");
        Directory.CreateDirectory(Path.Combine(mod, "Profiles", "classic-warlords"));
        Directory.CreateDirectory(Path.Combine(mod, "FeaturePacks", "pack-a"));
        Directory.CreateDirectory(Path.Combine(mod, "FeaturePacks", "pack-b"));
        File.WriteAllText(Path.Combine(mod, "Clan.json"), "[]");
        File.WriteAllText(Path.Combine(mod, "Profiles", "classic-warlords", "profile.json"),
            "{\"id\":\"classic-warlords\",\"displayName\":\"Classic\",\"baseWorld\":\"TestWorld\",\"modeId\":\"classic\",\"enabledPacks\":[],\"modRoot\":\"mod\"}");
        File.WriteAllText(Path.Combine(mod, "FeaturePacks", "pack-a", "pack.json"),
            "{\"id\":\"pack-a\",\"displayName\":\"A\",\"kind\":\"Visual\",\"conflicts\":[\"pack-b\"]}");
        File.WriteAllText(Path.Combine(mod, "FeaturePacks", "pack-b", "pack.json"),
            "{\"id\":\"pack-b\",\"displayName\":\"B\",\"kind\":\"Flavor\"}");
        return root;
    }
}
