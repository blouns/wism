namespace Wism.Client.Core.Boons
{
    public interface IBoon
    {
        object Result { get; set; }

        bool IsDefended { get; }

        object Redeem(Tile target);
    }
}