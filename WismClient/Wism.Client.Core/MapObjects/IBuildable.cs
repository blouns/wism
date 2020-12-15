namespace Wism.Client.MapObjects
{
    public interface IBuildable
    {
        bool TryBuild();

        void Raze();
    }
}