namespace Wism.Client.AI.Tactical
{
    public interface IStrategicBidMetadata
    {
        string ObjectiveKind { get; }

        string TargetCityShortName { get; }

        string TargetLocationShortName { get; }

        int? TargetX { get; }

        int? TargetY { get; }

        string Reason { get; }
    }
}
