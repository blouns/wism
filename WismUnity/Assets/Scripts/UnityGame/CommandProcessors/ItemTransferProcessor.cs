using System.Linq;
using Assets.Scripts.Managers;
using Wism.Client.CommandProcessors;
using Wism.Client.Commands;
using Wism.Client.Commands.Heros;
using Wism.Client.Controllers;

namespace Assets.Scripts.CommandProcessors
{
    public sealed class ItemTransferProcessor : ICommandProcessor
    {
        private readonly UnityManager unity;
        public ItemTransferProcessor(UnityManager unity) { this.unity = unity; }
        public bool CanExecute(ICommandAction command) => command is TakeItemsCommand || command is DropItemsCommand;

        public ActionState Execute(ICommandAction command)
        {
            var take = command as TakeItemsCommand;
            var drop = command as DropItemsCommand;
            var hero = take != null ? take.Hero : drop.Hero;
            // Some callers pass the live inventory/tile list, which execution mutates.
            var requested = (take != null ? take.Items : drop.Items).ToArray();
            var result = command.Execute();
            if (result != ActionState.Succeeded || !hero.Player.IsHuman) return result;
            var names = requested.Where(item => take != null
                ? hero.Items?.Contains(item) == true : hero.Tile.Items?.Contains(item) == true)
                .Select(item => item.DisplayName).ToArray();
            if (names.Length > 0)
                this.unity.NotifyUser(take != null ? "Taken: {0}" : "Dropped: {0}", string.Join(", ", names));
            return result;
        }
    }
}
