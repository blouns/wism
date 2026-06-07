using System.Collections.Generic;
using System.Linq;
using Wism.Client.AI.Strategic;
using Wism.Client.AI.Tactical;
using Wism.Client.Core;
using Wism.Client.Commands;

namespace Wism.Client.AI.Framework
{
    public class AiController
    {
        private readonly IStrategicModule strategicModule;
        private readonly List<ITacticalModule> tacticalModules;
        private readonly List<ITurnModule> turnModules;

        public AiController(IStrategicModule strategicModule, List<ITacticalModule> tacticalModules)
            : this(strategicModule, tacticalModules, new List<ITurnModule>())
        {
        }

        public AiController(IStrategicModule strategicModule, List<ITacticalModule> tacticalModules, List<ITurnModule> turnModules)
        {
            this.strategicModule = strategicModule;
            this.tacticalModules = tacticalModules ?? new List<ITacticalModule>();
            this.turnModules = turnModules ?? new List<ITurnModule>();
        }

        public IEnumerable<IBid> GetBids(World world)
        {
            return tacticalModules.SelectMany(module => module.GenerateBids(world));
        }

        public List<ICommandAction> ExecuteTurnAndReturnCommands(World world)
        {
            var commands = new List<ICommandAction>();

            foreach (var module in turnModules)
            {
                var generated = module.GenerateCommands(world);
                if (generated != null)
                {
                    commands.AddRange(generated);
                }
            }

            var bids = GetBids(world).ToList();
            if (bids.Count == 0)
            {
                return commands;
            }

            strategicModule.UpdateGoals(world);
            strategicModule.AllocateAssets(bids);

            var winningBids = (strategicModule as SimpleStrategicModule)?.GetAcceptedBids() ?? bids;

            foreach (var bid in winningBids)
            {
                var generated = bid.Module.GenerateCommands(bid.Armies, world);
                if (generated != null)
                {
                    commands.AddRange(generated);
                }
            }

            return commands;
        }

    }
}
