using Wism.Client.Core.Boons;

namespace Assets.Scripts.CommandProcessors
{
    public interface IBoonIdentifier
    {
        bool CanIdentify(IBoon boon);

        void Identify(IBoon boon);
    }
}
