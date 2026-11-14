using NUnit.Framework;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Client.Data;

namespace Wism.Client.Test.Unit;

[TestFixture]
public sealed class SaveFileFormatTests
{
    private static JObject Current() => new JObject
    {
        ["SaveFormat"] = JObject.FromObject(SaveFileFormat.CreateMetadata("beta-test")),
        ["WismGameEntity"] = new JObject { ["CurrentPlayerIndex"] = 3 },
        ["ShowAiCombat"] = false
    };

    [Test]
    public void LegacyNormalizationPreservesPayloadAndSource()
    {
        const string source = "{\"WismGameEntity\":{\"CurrentPlayerIndex\":3,\"Random\":{\"Seed\":17}},\"CustomOptional\":42}";
        var original = JObject.Parse(source);
        var migrated = SaveFileFormat.Read(source);
        Assert.That(JToken.DeepEquals(original["WismGameEntity"], migrated["WismGameEntity"]), Is.True);
        Assert.That((bool)migrated["ShowAiCombat"], Is.True);
        Assert.That((bool)migrated["ObserveAiMovement"], Is.True);
        Assert.That((int)migrated["CustomOptional"], Is.EqualTo(42));
        Assert.That(original["SaveFormat"], Is.Null);
        Assert.That(JToken.DeepEquals(migrated, SaveFileFormat.Read(migrated.ToString())), Is.True);
    }

    [Test]
    public void CurrentVersionRoundTripsWithoutChangingOptionalData()
    {
        var root = Current();
        root["OptionalFutureLabel"] = "kept";
        Assert.That(JToken.DeepEquals(root, SaveFileFormat.Read(root.ToString())), Is.True);
    }

    [TestCase("SchemaVersion", "0")]
    [TestCase("SchemaVersion", "-1")]
    [TestCase("SchemaVersion", "1.5")]
    [TestCase("SchemaVersion", "\"1\"")]
    [TestCase("SchemaVersion", "null")]
    [TestCase("SchemaVersion", "9999999999999999999")]
    [TestCase("MinimumReaderVersion", "0")]
    [TestCase("MinimumReaderVersion", "null")]
    public void InvalidVersionsAreRejected(string property, string value)
    {
        var root = Current();
        root["SaveFormat"][property] = JToken.Parse(value);
        Assert.Throws<SaveCompatibilityException>(() => SaveFileFormat.Read(root.ToString()));
    }

    [TestCase(2, 1)]
    [TestCase(2, 2)]
    [TestCase(1, 2)]
    public void FutureOrInconsistentVersionsAreRejected(int schema, int minimum)
    {
        var root = Current();
        root["SaveFormat"]["SchemaVersion"] = schema;
        root["SaveFormat"]["MinimumReaderVersion"] = minimum;
        Assert.Throws<SaveCompatibilityException>(() => SaveFileFormat.Read(root.ToString()));
    }

    [TestCase("null")]
    [TestCase("[]")]
    [TestCase("{}")]
    public void InvalidHeaderIsNotTreatedAsLegacy(string header)
    {
        var root = Current();
        root["SaveFormat"] = JToken.Parse(header);
        Assert.Throws<SaveCompatibilityException>(() => SaveFileFormat.Read(root.ToString()));
    }

    [Test]
    public void MissingCampaignIsRejected()
    {
        Assert.Throws<SaveCompatibilityException>(() => SaveFileFormat.Read("{}"));
        var root = Current();
        root.Remove("WismGameEntity");
        Assert.Throws<SaveCompatibilityException>(() => SaveFileFormat.Read(root.ToString()));
    }

    [Test]
    public void IncorrectHeaderCasingCannotBypassValidation()
    {
        Assert.Throws<SaveCompatibilityException>(() => SaveFileFormat.Read(
            "{\"saveFormat\":{\"SchemaVersion\":2},\"WismGameEntity\":{}}"));
    }

    [Test]
    public void DuplicateVersionCannotBypassValidation()
    {
        Assert.Throws<JsonReaderException>(() => SaveFileFormat.Read(
            "{\"SaveFormat\":null,\"SaveFormat\":{},\"WismGameEntity\":{}}"));
    }
}
