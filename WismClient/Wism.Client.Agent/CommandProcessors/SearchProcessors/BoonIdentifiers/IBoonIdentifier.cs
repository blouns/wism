using Wism.Client.Core.Boons;

namespace Wism.Client.Agent.CommandProcessors;

public interface IBoonIdentfier
{
    bool CanIdentify(IBoon boon);

    void Identify(IBoon boon);
}