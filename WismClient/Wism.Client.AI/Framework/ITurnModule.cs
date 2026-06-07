using System.Collections.Generic;
using Wism.Client.Commands;
using Wism.Client.Core;

namespace Wism.Client.AI.Framework
{
    public interface ITurnModule
    {
        IEnumerable<ICommandAction> GenerateCommands(World world);
    }
}
