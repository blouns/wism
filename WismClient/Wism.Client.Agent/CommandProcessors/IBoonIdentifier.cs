using Wism.Client.Core.Boons;

namespace Wism.Client.Agent.CommandProcessors;

public interface IBoonIdentifier
{
    bool CanIdentify(IBoon boon);

    void Identify(IBoon boon);
}