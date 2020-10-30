using AutoMapper;
using BranallyGames.Wism;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Wism.Client.Agent.Controllers;
using Wism.Client.Model;
using Wism.Client.Model.Commands;

namespace Wism.Client.View
{
    /// <summary>
    /// Template base class for a generic UI
    /// </summary>
    public abstract class WismViewBase
    {
        private ILogger logger;

        protected BranallyGames.Wism.Army selectedArmy;
        private readonly CommandController commandController;
        private readonly IMapper mapper;

        public WismViewBase(ILoggerFactory loggerFactory, CommandController commandController, IMapper mapper)
        {
            if (loggerFactory is null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            logger = loggerFactory.CreateLogger<WismViewBase>();
            this.commandController = commandController ?? throw new ArgumentNullException(nameof(commandController));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        public async Task RunAsync()
        {
            logger.LogInformation("WISM View successfully started");
            SetupWorld();

            try
            {
                int lastId = 0;
                while (true)
                {
                    // Game loop
                    Draw();
                    HandleInput();
                    DoTasks(ref lastId);

                    await Task.Delay(100);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());
                throw;
            }
        }

        protected abstract void DoTasks(ref int lastId);

        protected abstract void HandleInput();

        protected abstract void Draw();

        private void SetupWorld()
        {
            // BUGBUG: This is creating objects outside the controller resulting in mismatches.
            //         Must change to only create objects through the controller (impl Hire/Conscript)
            World.CreateDefaultWorld();

            // Create a hero
            commandController.AddCommand(
                new HireHeroCommandDto()
                {
                    ShortName = "Hero",
                    X = 1,
                    Y = 1
                });

            // BUGBUG: Hack to mirror the change; needs to be ported to Consumer
            World.Current.Players[0].HireHero(World.Current.Map[hero.X, hero.Y]);

            // Set the player's army to move
            this.selectedArmy = World.Current.Players[0].GetArmies()[0];

            // Create a light infantry opponent
            var lightInfantry = new ArmyDto()
            {
                DisplayName = "Light Infantry",
                HitPoints = 2,
                ShortName = "LightInfantry",
                Strength = 5,
                X = 1,
                Y = 1
            };

            commandController.AddCommand(
                new ConscriptArmyCommandDto()
                {
                    Army = hero
                });

            // BUGBUG: Hack to mirror the change; needs to be ported to Consumer
            World.Current.Players[1].ConscriptArmy(
                ModFactory.FindUnitInfo(lightInfantry.ShortName), 
                World.Current.Map[lightInfantry.X, lightInfantry.Y]);            
        }
    }
}
