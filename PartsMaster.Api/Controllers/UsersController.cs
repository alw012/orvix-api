using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;
using PartsMaster.Api.Models;
using System.Security.Claims;

namespace PartsMaster.Api.Controllers
{
    [Authorize(Roles = "Admin")]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult> GetAll()
        {
            var users = await _context.Users
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Phone,
                    u.Role,
                    u.IsActive,
                    u.CreatedAt
                })
                .ToListAsync();

            return Ok(users);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult> GetById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            return Ok(new
            {
                user.Id,
                user.Username,
                user.FullName,
                user.Phone,
                user.Role,
                user.IsActive,
                user.CreatedAt
            });
        }

        [HttpPost]
        public async Task<ActionResult> Create(CreateUserDto dto)
        {
            var exists = await _context.Users
                .AnyAsync(u => u.Username == dto.Username);

            if (exists)
                return BadRequest(new { message = "اسم المستخدم موجود مسبقاً" });

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                FullName = dto.FullName,
                Role = dto.Role,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم إنشاء المستخدم بنجاح", user.Id });
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            // تحقق إذا اسم المستخدم الجديد مأخوذ من شخص آخر
            if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
            {
                var exists = await _context.Users.AnyAsync(u => u.Username == dto.Username && u.Id != id);
                if (exists)
                    return BadRequest(new { message = "اسم المستخدم موجود مسبقاً" });
                user.Username = dto.Username;
            }

            user.FullName = dto.FullName;
            user.Role = dto.Role;
            user.IsActive = dto.IsActive;

            await _context.SaveChangesAsync();
            return Ok(new { message = "تم تحديث المستخدم بنجاح" });
        }

        [HttpPut("{id}/change-password")]
        public async Task<ActionResult> ChangePassword(int id, ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تغيير كلمة المرور بنجاح" });
        }

        [HttpPut("{id}/toggle-status")]
        public async Task<ActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            user.IsActive = !user.IsActive;
            await _context.SaveChangesAsync();

            var status = user.IsActive ? "مفعّل" : "موقوف";
            return Ok(new { message = $"المستخدم الآن {status}" });
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound(new { message = "المستخدم غير موجود" });

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم حذف المستخدم بنجاح" });
        }

        [AllowAnonymous]
        [HttpGet("my-profile")]
        public async Task<ActionResult> GetMyProfile()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.FullName,
                user.Phone,
                user.Username,
                user.Role
            });
        }

        [AllowAnonymous]
        [HttpPut("my-profile")]
        public async Task<ActionResult> UpdateMyProfile(UpdateProfileDto dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("id");
            if (userIdClaim == null)
                return Unauthorized();

            int userId = int.Parse(userIdClaim.Value);
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return NotFound();

            user.FullName = dto.FullName;
            user.Phone = dto.Phone;
            await _context.SaveChangesAsync();

            return Ok(new { message = "تم تحديث الحساب بنجاح" });
        }
    }
}
