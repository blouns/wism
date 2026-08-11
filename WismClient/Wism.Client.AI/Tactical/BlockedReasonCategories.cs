namespace Wism.Client.AI.Tactical
{
    public static class BlockedReasonCategories
    {
        public const string TargetInvalidated = "target-invalidated";
        public const string AlreadyAtAttackPosition = "already-at-attack-position";
        public const string EnemyBlocker = "enemy-blocker";
        public const string LowOddsCityAttack = "low-odds-city-attack";
        public const string LowOddsBlocker = "low-odds-blocker";
        public const string NoRoute = "no-route";
        public const string EmptyRoute = "empty-route";
        public const string BlockedNextStep = "blocked-next-step";
        public const string InsufficientMoves = "insufficient-moves";
        public const string NoSelectedAssets = "no-selected-assets";
        public const string RepeatedCommandBatch = "repeated-command-batch";
        public const string Unknown = "unknown";
    }
}
