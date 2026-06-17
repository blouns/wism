using Wism.Client.Core;

namespace Wism.Client.AI.InfluenceMaps
{
    /// <summary>
    ///     Null-object spatial advisor used by A/B eval baselines. It preserves the controller
    ///     contract without contributing influence to strategic or tactical consumers.
    /// </summary>
    public sealed class NoOpSpatialAdvisor : ISpatialAdvisor
    {
        public void Update()
        {
        }

        public double GetInfluence(Tile tile) => 0.0;

        public double GetFriendly(Tile tile) => 0.0;

        public double GetEnemy(Tile tile) => 0.0;

        public double GetTension(Tile tile) => 0.0;

        public double GetRawFriendly(Tile tile) => 0.0;

        public double GetRawEnemy(Tile tile) => 0.0;

        public bool IsFrontLine(Tile tile) => false;

        public Tile GetGradientStep(Tile from, bool ascendFriendly) => from;

        public double GetFriendly(int x, int y) => 0.0;

        public double GetEnemy(int x, int y) => 0.0;

        public double GetTension(int x, int y) => 0.0;
    }
}
