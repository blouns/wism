using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WismData.Models;

namespace WismData.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WorldsController : ControllerBase
    {
        private readonly WismDataContext _context;

        public WorldsController(WismDataContext context)
        {
            _context = context;
        }

        // GET: api/Worlds
        [HttpGet]
        public async Task<ActionResult<IEnumerable<World>>> GetWorld()
        {
            return await _context.World.ToListAsync();
        }

        // GET: api/Worlds/5
        [HttpGet("{id}")]
        public async Task<ActionResult<World>> GetWorld(long id)
        {
            var world = await _context.World.FindAsync(id);

            if (world == null)
            {
                return NotFound();
            }

            return world;
        }

        // PUT: api/Worlds/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPut("{id}")]
        public async Task<IActionResult> PutWorld(long id, World world)
        {
            if (id != world.Id)
            {
                return BadRequest();
            }

            _context.Entry(world).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!WorldExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Worlds
        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://go.microsoft.com/fwlink/?linkid=2123754.
        [HttpPost]
        public async Task<ActionResult<World>> PostWorld(World world)
        {
            _context.World.Add(world);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetWorld", new { id = world.Id }, world);
        }

        // DELETE: api/Worlds/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<World>> DeleteWorld(long id)
        {
            var world = await _context.World.FindAsync(id);
            if (world == null)
            {
                return NotFound();
            }

            _context.World.Remove(world);
            await _context.SaveChangesAsync();

            return world;
        }

        private bool WorldExists(long id)
        {
            return _context.World.Any(e => e.Id == id);
        }
    }
}
