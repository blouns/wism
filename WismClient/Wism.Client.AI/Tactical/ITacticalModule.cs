// File: Wism.Client.AI/Tactical/ITacticalModule.cs

using System.Collections.Generic;
using Wism.Client.Commands;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Tactical
{
    public interface ITacticalModule
    {
        /// <summary>
        /// Generate a list of bids for armies this tactical module wants to control.
        /// </summary>
        IEnumerable<IBid> GenerateBids(World world);

        /// <summary>
        /// Generate the command actions for a given army based on tactical logic.
        /// </summary>
        IEnumerable<ICommandAction> GenerateCommands(List<Army> armies, World world);
    }
}
