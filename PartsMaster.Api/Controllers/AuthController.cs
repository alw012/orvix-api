using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PartsMaster.Api.Data;
using PartsMaster.Api.Models;
using PartsMaster.Api.Services;

namespace PartsMaster.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly JwtService _jwtService;

        public AuthController(AppDbContext context, JwtService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto login)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(x => x.Username == login.Username && x.IsActive);

            if (user == null)
                return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة" });

            bool isValid = BCrypt.Net.BCrypt.Verify(login.Password, user.PasswordHash);

            if (!isValid)
                return Unauthorized(new { message = "اسم المستخدم أو كلمة المرور غير صحيحة" });

            var token = _jwtService.GenerateToken(user);

            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.Username,
                    user.FullName,
                    user.Role
                }
            });
        }
    }
}
