using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;

namespace PartsMaster.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ActivityLogsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ActivityLogsController(AppDbContext context)
        {
            _context = context;
        }

        // GET: api/activitylogs
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? module = null,
            [FromQuery] string? userId = null,
            [FromQuery] string? shopId = null)
        {
            var query = _context.ActivityLogs.AsQueryable();

            if (!string.IsNullOrEmpty(module))
                query = query.Where(x => x.Module == module);

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(x => x.UserId == userId);

            if (!string.IsNullOrEmpty(shopId))
                query = query.Where(x => x.ShopId == shopId);

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new { total, page, pageSize, data = logs });
        }

        // GET: api/activitylogs/5
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            var log = await _context.ActivityLogs.FindAsync(id);
            if (log == null) return NotFound();
            return Ok(log);
        }
    }
}