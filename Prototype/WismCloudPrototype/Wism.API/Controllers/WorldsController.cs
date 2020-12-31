using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using BranallyGames.Wism.Repository;
using BranallyGames.Wism.API.Model;
using BranallyGames.Wism.Repository.Entities;
using Microsoft.AspNetCore.Http;

namespace BranallyGames.Wism.API.Controllers
{
    [Produces("application/json", "application/xml")]
    [Route("api/[controller]")]
    [ApiController]
    public class WorldsController : ControllerBase
    {
        private readonly IWismRepository wismRepository;
        private readonly IMapper mapper;

        public WorldsController(IWismRepository wismRepository, IMapper mapper)
        {
            this.wismRepository = wismRepository ?? throw new ArgumentNullException(nameof(wismRepository));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Get all available worlds.
        /// </summary>
        /// <returns>ActionResult of all available worlds</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet(Name = "GetWorlds")]
        [HttpHead]
        public ActionResult<IEnumerable<WorldModel>> GetWorlds()
        {
            var worldsFromRepo = wismRepository.GetWorldsAsync().Result;
            return Ok(mapper.Map<IEnumerable<WorldModel>>(worldsFromRepo));
        }

        /// <summary>
        /// Get a world by its ID
        /// </summary>
        /// <param name="worldId">The ID of the world to get</param>
        /// <returns>ActionResult of a world with ID, name, and details</returns>
        /// <response code="200">Returns the requested world</response>
        [ProducesResponseType(StatusCodes.Status200OK)]        
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [HttpGet("{worldId}", Name = "GetWorld")]
        public ActionResult<WorldModel> GetWorld(Guid worldId)
        {
            var worldFromRepo = wismRepository.GetWorldAsync(worldId).Result;
            if (worldFromRepo == null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<WorldModel>(worldFromRepo));
        }

        /// <summary>
        /// Create a new world
        /// </summary>
        /// <param name="world">World name and details to create</param>
        /// <returns>ActionResult of a new world with ID and details</returns>
        /// <response code="201">Returns the created world</response>
        [HttpPost(Name = "CreateWorld")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]        
        public ActionResult<WorldModel> CreateWorld(WorldForCreationModel world)
        {
            var worldEntity = mapper.Map<World>(world);
            wismRepository.AddWorld(worldEntity);
            wismRepository.Save();

            var worldToReturn = mapper.Map<WorldModel>(worldEntity);
            return CreatedAtRoute("GetWorld",
                new { worldId = worldToReturn.Id },
                worldToReturn);
        }

        // PUT api/<WorldsController>/5
        /// <summary>
        /// Updates the given world by ID from body in JSON format
        /// </summary>
        /// <param name="worldId">World ID to update</param>
        /// <param name="world">World value from body in JSON</param>
        /// <returns>World with updated values</returns>
        [Consumes("application/json")]
        [HttpPut("{worldId}", Name = "UpdateWorld")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]        
        public IActionResult UpdateWorld(Guid worldId, [FromBody] WorldForUpdateModel world)
        {
            var worldFromRepo = wismRepository.GetWorldAsync(worldId).Result;
            if (worldFromRepo == null)
            {
                return NotFound();
            }

            var worldEntity = mapper.Map<World>(world);
            wismRepository.UpdateWorld(worldEntity);
            wismRepository.Save();

            return NoContent();
        }

        /// <summary>
        /// Delete the given world by ID
        /// </summary>
        /// <param name="worldId">World ID to delete</param>
        /// <returns>No content</returns>
        [HttpDelete("{worldId}", Name = "DeleteWorld")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeleteWorld(Guid worldId)
        {
            var worldFromRepo = wismRepository.GetWorldAsync(worldId).Result;
            if (worldFromRepo == null)
            {
                return NotFound();
            }

            wismRepository.DeleteWorld(worldFromRepo);
            wismRepository.Save();

            return NoContent();
        }
    }
}
