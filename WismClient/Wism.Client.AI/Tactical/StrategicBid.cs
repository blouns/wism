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
            int? targetY = null,
            string reason = null)
        {
            Armies = armies;
            Module = module;
            Utility = utility;
            ObjectiveKind = objectiveKind;
            TargetCityShortName = targetCityShortName;
            TargetLocationShortName = targetLocationShortName;
            TargetX = targetX;
            TargetY = targetY;
            Reason = reason ?? BuildDefaultReason(objectiveKind, targetCityShortName, targetLocationShortName, targetX, targetY);
        }

        public List<Army> Armies { get; }

        public double Utility { get; }

        public ITacticalModule Module { get; }

        public string ObjectiveKind { get; }

        public string TargetCityShortName { get; }

        public string TargetLocationShortName { get; }

        public int? TargetX { get; }

        public int? TargetY { get; }

        public string Reason { get; }

        private static string BuildDefaultReason(
            string objectiveKind,
            string targetCityShortName,
            string targetLocationShortName,
            int? targetX,
            int? targetY)
        {
            var target = targetCityShortName ?? targetLocationShortName;
            if (!string.IsNullOrWhiteSpace(target))
            {
                return objectiveKind + " target " + target + ".";
            }

            if (targetX.HasValue && targetY.HasValue)
            {
                return objectiveKind + " target (" + targetX.Value + "," + targetY.Value + ").";
            }

            return objectiveKind + " objective.";
        }
    }
}
