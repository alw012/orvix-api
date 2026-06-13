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
    public class StoresController : ControllerBase
    {
        private readonly AppDbContext _context;

        public StoresController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var stores = await _context.Stores
                .Select(s => new
                {
                    s.Id,
                    s.Name,
                    s.City,
                    s.Phone,
                    s.Phone2,
                    s.Email,
                    s.Address,
                    s.Location,
                    s.IsActive,
                    s.MaxDevices,
                    s.UserId,
                    s.CreatedAt,
                    DeviceCount = s.Devices.Count()
                })
                .ToListAsync();

            return Ok(stores);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var store = await _context.Stores
                .Include(s => s.Devices)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (store == null)
                return NotFound(new { message = "المحل غير موجود" });

            var user = store.UserId.HasValue
                ? await _context.Users.FindAsync(store.UserId.Value)
                : null;

            return Ok(new
            {
                store.Id,
                store.Name,
                store.City,
                store.Phone,
                store.Phone2,
                store.Email,
                store.Address,
                store.Location,
                store.IsActive,
                store.MaxDevices,
                store.UserId,
                store.CreatedAt,
                DeviceCount = store.Devices.Count,
                Username = user?.Username ?? ""
            });
        }

        [HttpGet("city/{city}")]
        public async Task<ActionResult> GetByCity(string city)
        {
            var stores = await _context.Stores
                .Where(s => s.City == city && s.IsActive)
                .ToListAsync();

            return Ok(stores);
        }

        [HttpPost]
        public async Task<ActionResult> Create(StoreDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password))
                return BadRequest(new { message = "يرجى إدخال اسم المستخدم وكلمة المرور" });

            var exists = await _context.Users.AnyAsync(u => u.Username == dto.Username);
            if (exists)
                return BadRequest(new { message = "اسم المستخدم مستخدم مسبقاً" });

            var store = new Store
            {
                Name = dto.Name,
                City = dto.City,
                Phone = dto.Phone,
                Phone2 = dto.Phone2,
                Email = dto.Email,
                Address = dto.Address,
                Location = dto.Location,
                IsActive = true,
                MaxDevices = dto.MaxDevices > 0 ? dto.MaxDevices : 1,
                CreatedAt = DateTime.UtcNow
            };

            _context.Stores.Add(store);
            await _context.SaveChangesAsync();

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.Name,
                Role = "Shop",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            store.UserId = user.Id;
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إضافة المحل بنجاح", store.Id });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, StoreDto dto)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
                return NotFound(new { message = "المحل غير موجود" });

            store.Name = dto.Name;
            store.City = dto.City;
            store.Phone = dto.Phone;
            store.Phone2 = dto.Phone2;
            store.Email = dto.Email;
            store.Address = dto.Address;
            store.Location = dto.Location;
            store.MaxDevices = dto.MaxDevices > 0 ? dto.MaxDevices : store.MaxDevices;

            // تعديل اسم المستخدم
            if (!string.IsNullOrWhiteSpace(dto.Username) && store.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(store.UserId.Value);
                if (user != null && user.Username != dto.Username)
                {
                    var usernameExists = await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != user.Id);
                    if (usernameExists)
                        return BadRequest(new { message = "اسم المستخدم مستخدم مسبقاً" });
                    user.Username = dto.Username;
                    user.FullName = dto.Name;
                }
            }

            // تعديل كلمة المرور
            if (!string.IsNullOrWhiteSpace(dto.Password) && store.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(store.UserId.Value);
                if (user != null)
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تحديث المحل بنجاح" });
        }

        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult> ToggleStatus(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
                return NotFound(new { message = "المحل غير موجود" });

            store.IsActive = !store.IsActive;

            if (store.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(store.UserId.Value);
                if (user != null)
                    user.IsActive = store.IsActive;
            }

            await _context.SaveChangesAsync();
            var status = store.IsActive ? "مفعّل" : "موقوف";
            return Ok(new { message = $"المحل الآن {status}" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var store = await _context.Stores.FindAsync(id);
            if (store == null)
                return NotFound(new { message = "المحل غير موجود" });

            if (store.UserId.HasValue)
            {
                var user = await _context.Users.FindAsync(store.UserId.Value);
                if (user != null)
                    _context.Users.Remove(user);
            }

            _context.Stores.Remove(store);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف المحل بنجاح" });
        }
    }
}
