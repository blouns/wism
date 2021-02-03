namespace Wism.Client.Core
{
    public interface IBoon
    {
        bool IsDefended { get; }

        object Redeem(Tile target);
    }
}
