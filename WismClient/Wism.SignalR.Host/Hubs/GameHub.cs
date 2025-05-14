using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
using Wism.Companion.Shared.Events;

namespace Wism.SignalR.Host.Hubs
{
    public class GameHub : Hub
    {
        public async Task BroadcastCommand(CommandExecutedEvent @event)
        {
            await Clients.All.SendAsync("OnCommandExecuted", @event);
        }

        public async Task BroadcastMapSnapshot(MapSnapshot snapshot)
        {
            await Clients.All.SendAsync("OnMapSnapshot", snapshot);
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            await base.OnDisconnectedAsync(exception);
        }
    }
}
