using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Wism.SignalR.Host.Hubs;
using Wism.Companion.Shared.Events;
using Wism.Companion.Shared.Models;

namespace Wism.SignalR.Host.Controllers
{
    [ApiController]
    [Route("simulate")]
    public class SimulationController : ControllerBase
    {
        private readonly IHubContext<GameHub> _hub;

        public SimulationController(IHubContext<GameHub> hub)
        {
            _hub = hub;
        }

        [HttpPost("command")]
        public async Task<IActionResult> SimulateCommand()
        {
            var simulated = new CommandExecutedEvent
            {
                CommandType = "MoveCommand",
                ActorId = "Hero123",
                TargetPosition = new PositionDto { X = 3, Y = 4 },
                Result = "Success",
                Timestamp = DateTime.UtcNow,
                Parameters = new Dictionary<string, object>
                {
                    { "Speed", 2 },
                    { "Direction", "NE" }
                }
            };

            await _hub.Clients.All.SendAsync("OnCommandExecuted", simulated);
            return Ok("Simulated CommandExecutedEvent broadcasted.");
        }

        [HttpPost("map")]
        public async Task<IActionResult> SimulateMap()
        {
            var snapshot = new MapSnapshot
            {
                Width = 5,
                Height = 5,
                Tiles = new List<TileDto>(),
                Heroes = new List<HeroDto>
                {
                    new HeroDto
                    {
                        Name = "TestHero",
                        Owner = "Red",
                        Health = 100,
                        Position = new PositionDto { X = 2, Y = 2 }
                    }
                },
                Timestamp = DateTime.UtcNow
            };

            for (int x = 0; x < 5; x++)
            {
                for (int y = 0; y < 5; y++)
                {
                    snapshot.Tiles.Add(new TileDto
                    {
                        X = x,
                        Y = y,
                        TerrainType = (x + y) % 2 == 0 ? "Grass" : "Forest",
                        HasCity = (x == 2 && y == 2)
                    });
                }
            }

            await _hub.Clients.All.SendAsync("OnMapSnapshot", snapshot);
            return Ok("Simulated MapSnapshot broadcasted.");
        }
    }
}
