namespace Wism.Client.AI.Tactical
{
    public interface IBlockedReasonProvider
    {
        string LastBlockingReason { get; }
    }
}
