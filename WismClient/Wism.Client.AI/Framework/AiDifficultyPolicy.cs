using System;
using Wism.Client.Core;

namespace Wism.Client.AI.Framework
{
    public sealed class AiDifficultyPolicy
    {
        private AiDifficultyPolicy(
            AiDifficultyTier tier,
            double cityAttackWinProbability,
            double endgameCityAttackWinProbability,
            double blockerAttackWinProbability,
            double exterminationAttackWinProbability,
            double counterAttackWinProbability)
        {
            this.Tier = tier;
            this.CityAttackWinProbability = cityAttackWinProbability;
            this.EndgameCityAttackWinProbability = endgameCityAttackWinProbability;
            this.BlockerAttackWinProbability = blockerAttackWinProbability;
            this.ExterminationAttackWinProbability = exterminationAttackWinProbability;
            this.CounterAttackWinProbability = counterAttackWinProbability;
        }

        public AiDifficultyTier Tier { get; }

        public double CityAttackWinProbability { get; }

        public double EndgameCityAttackWinProbability { get; }

        public double BlockerAttackWinProbability { get; }

        public double ExterminationAttackWinProbability { get; }

        public double CounterAttackWinProbability { get; }

        public static AiDifficultyPolicy ForCurrentPlayer()
        {
            var player = Game.Current?.GetCurrentPlayer();
            return For(player?.AiDifficulty ?? AiDifficultyTier.Lord);
        }

        public static AiDifficultyPolicy For(AiDifficultyTier tier)
        {
            switch (tier)
            {
                case AiDifficultyTier.Knight:
                    return new AiDifficultyPolicy(tier, 0.65, 0.40, 0.70, 0.70, 0.85);
                case AiDifficultyTier.Baron:
                    return new AiDifficultyPolicy(tier, 0.55, 0.30, 0.55, 0.55, 0.80);
                case AiDifficultyTier.Lord:
                    return new AiDifficultyPolicy(tier, 0.40, 0.20, 0.40, 0.40, 0.75);
                case AiDifficultyTier.Warlord:
                    return new AiDifficultyPolicy(tier, 0.30, 0.10, 0.30, 0.30, 0.65);
                default:
                    throw new ArgumentOutOfRangeException(nameof(tier), tier, "Unknown AI difficulty tier.");
            }
        }

        public static bool TryParseProfileSuffix(string aiProfile, out AiDifficultyTier tier)
        {
            foreach (AiDifficultyTier candidate in Enum.GetValues(typeof(AiDifficultyTier)))
            {
                if (aiProfile != null && aiProfile.EndsWith("-" + candidate, StringComparison.OrdinalIgnoreCase))
                {
                    tier = candidate;
                    return true;
                }
            }

            tier = AiDifficultyTier.Lord;
            return false;
        }

        public static string GetBaseProfile(string aiProfile)
        {
            if (!TryParseProfileSuffix(aiProfile, out var tier))
            {
                return aiProfile;
            }

            return aiProfile.Substring(0, aiProfile.Length - tier.ToString().Length - 1);
        }
    }
}
