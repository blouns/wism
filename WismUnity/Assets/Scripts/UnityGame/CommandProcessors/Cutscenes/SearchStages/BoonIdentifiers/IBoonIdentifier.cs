using Wism.Client.Core;

namespace Assets.Scripts.CommandProcessors
{
    public interface IBoonIdentfier
    {
        bool CanIdentify(IBoon boon);

        void Identify(IBoon boon);
    }
}
