using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;

namespace PartsMaster.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class StatsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StatsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetStats()
        {
            var totalShops      = await _context.Stores.CountAsync();
            var totalDevices    = await _context.Devices.CountAsync();
            var onlineDevices   = await _context.Devices.CountAsync(d => d.IsOnline);
            var offlineDevices  = totalDevices - onlineDevices;
            var totalParts      = await _context.Parts.CountAsync();

            return Ok(new
            {
                totalShops,
                totalDevices,
                onlineDevices,
                offlineDevices,
                totalParts
            });
        }
    }
}