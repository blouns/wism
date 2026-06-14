using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Tactical
{
    public sealed class StrategicBid : IBid, IStrategicBidMetadata
    {
        public StrategicBid(
            List<Army> armies,
            ITacticalModule module,
            double utility,
            string objectiveKind,
            string targetCityShortName = null,
            string targetLocationShortName = null,
            int? targetX = null,
            int? targetY = null)
        {
            Armies = armies;
            Module = module;
            Utility = utility;
            ObjectiveKind = objectiveKind;
            TargetCityShortName = targetCityShortName;
            TargetLocationShortName = targetLocationShortName;
            TargetX = targetX;
            TargetY = targetY;
        }

        public List<Army> Armies { get; }

        public double Utility { get; }

        public ITacticalModule Module { get; }

        public string ObjectiveKind { get; }

        public string TargetCityShortName { get; }

        public string TargetLocationShortName { get; }

        public int? TargetX { get; }

        public int? TargetY { get; }
    }
}
