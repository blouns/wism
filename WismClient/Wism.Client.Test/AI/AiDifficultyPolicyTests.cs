using NUnit.Framework;
using Wism.Client.AI.Framework;
using Wism.Client.Core;

namespace Wism.Client.Test.AI;

[TestFixture]
public sealed class AiDifficultyPolicyTests
{
    [Test]
    public void Policies_IncreaseCombatPressureWithoutChangingResources()
    {
        var knight = AiDifficultyPolicy.For(AiDifficultyTier.Knight);
        var baron = AiDifficultyPolicy.For(AiDifficultyTier.Baron);
        var lord = AiDifficultyPolicy.For(AiDifficultyTier.Lord);
        var warlord = AiDifficultyPolicy.For(AiDifficultyTier.Warlord);

        Assert.That(knight.CityAttackWinProbability, Is.GreaterThan(baron.CityAttackWinProbability));
        Assert.That(baron.CityAttackWinProbability, Is.GreaterThan(lord.CityAttackWinProbability));
        Assert.That(lord.CityAttackWinProbability, Is.GreaterThan(warlord.CityAttackWinProbability));
        Assert.That(knight.BlockerAttackWinProbability, Is.GreaterThan(warlord.BlockerAttackWinProbability));
        Assert.That(knight.ExterminationAttackWinProbability, Is.GreaterThan(warlord.ExterminationAttackWinProbability));
        Assert.That(knight.CounterAttackWinProbability, Is.GreaterThan(warlord.CounterAttackWinProbability));
    }

    [TestCase("strategic-knight", AiDifficultyTier.Knight, "strategic")]
    [TestCase("strategic-baron", AiDifficultyTier.Baron, "strategic")]
    [TestCase("strategic-lord", AiDifficultyTier.Lord, "strategic")]
    [TestCase("strategic-warlord", AiDifficultyTier.Warlord, "strategic")]
    public void EvalProfileSuffix_SeparatesDifficultyFromAiProfile(
        string label,
        AiDifficultyTier expectedTier,
        string expectedProfile)
    {
        Assert.That(AiDifficultyPolicy.TryParseProfileSuffix(label, out var tier), Is.True);
        Assert.That(tier, Is.EqualTo(expectedTier));
        Assert.That(AiDifficultyPolicy.GetBaseProfile(label), Is.EqualTo(expectedProfile));
    }
}
