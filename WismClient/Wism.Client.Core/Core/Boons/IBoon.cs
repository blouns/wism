namespace Wism.Client.Core
{
    public interface IBoon
    {
        object Result { get;  set; }

        bool IsDefended { get; }

        object Redeem(Tile target);
    }
}
