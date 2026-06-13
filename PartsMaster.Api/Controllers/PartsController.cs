using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;
using PartsMaster.Api.Models;

namespace PartsMaster.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class PartsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PartsController(AppDbContext context)
        {
            _context = context;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult<List<Part>>> GetAll()
        {
            return await _context.Parts.ToListAsync();
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<ActionResult<Part>> GetById(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound();

            return part;
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<ActionResult> Create(Part part)
        {
            _context.Parts.Add(part);
            await _context.SaveChangesAsync();
            return Ok(part);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, Part updated)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound();

            part.PartNumber = updated.PartNumber;
            part.Type = updated.Type;
            part.Quantity = updated.Quantity;
            part.Price = updated.Price;
            part.StoreId = updated.StoreId;

            await _context.SaveChangesAsync();
            return Ok(part);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var part = await _context.Parts.FindAsync(id);
            if (part == null)
                return NotFound();

            _context.Parts.Remove(part);
            await _context.SaveChangesAsync();
            return Ok("Deleted");
        }
    }
}
