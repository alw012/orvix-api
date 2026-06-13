using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;
using PartsMaster.Api.Models;

namespace PartsMaster.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class DevicesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public DevicesController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll([FromQuery] int? storeId = null)
        {
            var query = _context.Devices.Include(d => d.Store).AsQueryable();

            if (storeId.HasValue)
                query = query.Where(d => d.StoreId == storeId.Value);

            var devices = await query
                .Select(d => new
                {
                    d.Id,
                    d.DeviceId,
                    d.Name,
                    d.MacAddress,
                    d.HardwareFingerprint,
                    d.IsOnline,
                    d.LastSeen,
                    d.CreatedAt,
                    d.StoreId,
                    StoreName = d.Store != null ? d.Store.Name : ""
                })
                .ToListAsync();

            return Ok(devices);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Device device)
        {
            // تحقق من الحد الأقصى للأجهزة
            var store = await _context.Stores
                .Include(s => s.Devices)
                .FirstOrDefaultAsync(s => s.Id == device.StoreId);

            if (store == null)
                return NotFound(new { message = "المحل غير موجود" });

            if (store.Devices.Count >= store.MaxDevices)
                return BadRequest(new { message = $"وصل المحل للحد الأقصى للأجهزة ({store.MaxDevices})" });

            // توليد DeviceId تلقائي
            var count = await _context.Devices.CountAsync();
            device.DeviceId = $"DEV-{(count + 1):D6}";
            device.CreatedAt = DateTime.UtcNow;

            _context.Devices.Add(device);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إضافة الجهاز بنجاح", device.Id, device.DeviceId });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var device = await _context.Devices.FindAsync(id);
            if (device == null)
                return NotFound(new { message = "الجهاز غير موجود" });

            _context.Devices.Remove(device);
            await _context.SaveChangesAsync();
            return Ok(new { message = "تم حذف الجهاز بنجاح" });
        }
    }
}
