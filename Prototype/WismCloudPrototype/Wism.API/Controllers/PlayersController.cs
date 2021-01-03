using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using BranallyGames.Wism.API.Model;
using BranallyGames.Wism.Repository;
using BranallyGames.Wism.Repository.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;

namespace BranallyGames.Wism.API.Controllers
{
    [Produces("application/json", "application/xml")]
    [Route("api/worlds/{worldId}/players")]
    [ApiController]
    public class PlayersController : ControllerBase
    {
        private readonly IWismRepository wismRepository;
        private readonly IMapper mapper;

        public PlayersController(IWismRepository wismRepository, IMapper mapper)
        {
            this.wismRepository = wismRepository ?? throw new ArgumentNullException(nameof(wismRepository));
            this.mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Get all available Players in the world
        /// </summary>
        /// <returns>ActionResult of all available Players</returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [HttpGet(Name = "GetPlayersForWorld")]
        [HttpHead]
        public ActionResult<IEnumerable<PlayerModel>> GetPlayersForWorld(Guid worldId)
        {
            var playersFromRepo = wismRepository.GetPlayersAsync(worldId).Result;
            return Ok(mapper.Map<IEnumerable<PlayerModel>>(playersFromRepo));
        }

        /// <summary>
        /// Get a player by its ID for a given world
        /// </summary>
        /// <param name="worldId">The ID of the world with the player</param>
        /// <param name="playerId">The ID of the player to get</param>
        /// <returns>ActionResult of a player with ID, name, and details</returns>
        /// <response code="200">Returns the requested player</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        [HttpGet("{playerId}", Name = "GetPlayerForWorld")]
        public ActionResult<PlayerModel> GetPlayerForWorld(Guid worldId, Guid playerId)
        {
            var playerFromRepo = wismRepository.GetPlayerAsync(worldId, playerId).Result;
            if (playerFromRepo == null)
            {
                return NotFound();
            }

            return Ok(mapper.Map<PlayerModel>(playerFromRepo));
        }

        /// <summary>
        /// Create a new player
        /// </summary>
        /// <param name="worldId">World ID for the new player</param>
        /// <param name="player">Player name and details to create</param>
        /// <returns>ActionResult of a new player with ID and details</returns>
        /// <response code="201">Returns the created player</response>
        [HttpPost(Name = "CreatePlayerForWorld")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult<PlayerModel> CreatePlayer(Guid worldId, PlayerForCreationModel player)
        {
            if (!wismRepository.WorldExistsAsync(worldId).Result)
            {
                return NotFound();
            }

            var playerEntity = mapper.Map<Player>(player);
            wismRepository.AddPlayer(worldId, playerEntity);
            wismRepository.Save();

            var playerToReturn = mapper.Map<PlayerModel>(playerEntity);
            return CreatedAtRoute("GetPlayerForWorld",
                new { worldId = worldId, playerId = playerToReturn.Id },
                playerToReturn);
        }

        /// <summary>
        /// Updates the given player by ID from body in JSON format
        /// </summary>
        /// <param name="worldId">World ID containing player</param>
        /// <param name="playerId">Player ID to update</param>
        /// <param name="player">Player value from body in JSON</param>
        /// <returns>No response except for update or Player with updated ID for upsert</returns>
        /// <response code="204">No content</response>
        /// <response code="201">Player with updated ID and values</response>        
        [HttpPut("{playerId}", Name = "UpdatePlayerForWorld")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult UpdatePlayerForWorld(
            Guid worldId, 
            Guid playerId, 
            [FromBody] PlayerForUpdateModel player)
        {
            if (!wismRepository.WorldExistsAsync(worldId).Result)
            {
                return NotFound();
            }

            var playerFromRepo = wismRepository.GetPlayerAsync(worldId, playerId).Result;
            if (playerFromRepo == null)
            {
                // Upsert
                var playerToAdd = mapper.Map<Player>(player);
                playerToAdd.Id = playerId;

                wismRepository.AddPlayer(worldId, playerToAdd);
                wismRepository.Save();

                var playerToReturn = mapper.Map<PlayerModel>(playerToAdd);

                return CreatedAtRoute("GetPlayerForWorld",
                    new { worldId, playerId = playerToReturn.Id },
                    playerToReturn);
            }

            mapper.Map(player, playerFromRepo);
            wismRepository.UpdatePlayer(playerFromRepo);
            wismRepository.Save();

            return NoContent();
        }

        /// <summary>
        /// Updates a Player partially
        /// </summary>
        /// <param name="worldId">World ID containing the player</param>
        /// <param name="playerId">Player ID to be udpated or inserted</param>
        /// <param name="patchDocument">Player to update from body</param>
        /// <returns>No content on update or new player on upsert</returns>
        /// <response code="204">No response</response>
        /// <response code="201">Player with updated ID and values</response> 
        [HttpPatch("{playerId}", Name ="PatchPlayerForWorld")]
        [Consumes("application/json")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
        public ActionResult PatchPlayerForWorld(Guid worldId,
            Guid playerId,
            JsonPatchDocument<PlayerForUpdateModel> patchDocument)
        {
            if (!wismRepository.WorldExistsAsync(worldId).Result)
            {
                return NotFound();
            }

            var playerForWorldFromRepo = wismRepository.GetPlayerAsync(worldId, playerId).Result;

            if (playerForWorldFromRepo == null)
            {
                // upsert
                var playerModel = new PlayerForUpdateModel();
                patchDocument.ApplyTo(playerModel, ModelState);

                if (!TryValidateModel(playerModel))
                {
                    return ValidationProblem(ModelState);
                }

                var playerToAdd = mapper.Map<Player>(playerModel);
                playerToAdd.Id = playerId;

                wismRepository.AddPlayer(worldId, playerToAdd);
                wismRepository.Save();

                var playerToReturn = mapper.Map<PlayerModel>(playerToAdd);

                return CreatedAtRoute("GetPlayerForWorld",
                    new { worldId, playerId = playerToReturn.Id },
                    playerToReturn);
            }

            var playerToPatch = mapper.Map<PlayerForUpdateModel>(playerForWorldFromRepo);
            // add validation
            patchDocument.ApplyTo(playerToPatch, ModelState);

            if (!TryValidateModel(playerToPatch))
            {
                return ValidationProblem(ModelState);
            }

            mapper.Map(playerToPatch, playerForWorldFromRepo);

            wismRepository.UpdatePlayer(playerForWorldFromRepo);

            wismRepository.Save();

            return NoContent();
        }

        /// <summary>
        /// Delete the given player by ID
        /// </summary>
        /// <param name="worldId">World ID containing the player</param>
        /// <param name="playerId">Player ID to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">No content</response>
        [HttpDelete("{playerId}", Name = "DeletePlayerForWorld")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeletePlayer(Guid worldId, Guid playerId)
        {
            if (!wismRepository.WorldExistsAsync(worldId).Result)
            {
                return NotFound();
            }
            var playerFromRepo = wismRepository.GetPlayerAsync(worldId, playerId).Result;
            if (playerFromRepo == null)
            {
                return NotFound();
            }

            wismRepository.DeletePlayer(playerFromRepo);
            wismRepository.Save();

            return NoContent();
        }
    }
}