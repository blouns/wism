using Wism.Client.Core;

namespace Assets.Scripts.Managers
{
    public interface IBoonIdentfier
    {
        bool CanIdentify(IBoon boon);

        void Identify(IBoon boon);
    }
}
