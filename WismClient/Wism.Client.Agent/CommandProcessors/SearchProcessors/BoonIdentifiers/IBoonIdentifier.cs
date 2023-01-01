using Wism.Client.Core;

namespace Wism.Client.Agent.CommandProcessors;

public interface IBoonIdentfier
{
    bool CanIdentify(IBoon boon);

    void Identify(IBoon boon);
}