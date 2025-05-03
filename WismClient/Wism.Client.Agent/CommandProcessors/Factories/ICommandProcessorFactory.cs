using System.Collections.Generic;
using Wism.Client.Agent.UI;
using Wism.Client.CommandProcessors;

public interface ICommandProcessorFactory
{
    List<ICommandProcessor> CreateProcessors(AsciiGame game);
}
