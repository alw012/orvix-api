using PartsMaster.Api.Data;
using PartsMaster.Api.Models;

namespace PartsMaster.Api.Services
{
    public class ActivityLogService : IActivityLogService
    {
        private readonly AppDbContext _context;

        public ActivityLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task LogAsync(string action, string module, string? description = null,
            string? userId = null, string? userName = null,
            string? shopId = null, string? shopName = null,
            string? ipAddress = null, bool isSuccess = true,
            string? errorMessage = null)
        {
            var log = new ActivityLog
            {
                Action = action,
                Module = module,
                Description = description,
                UserId = userId,
                UserName = userName,
                ShopId = shopId,
                ShopName = shopName,
                IpAddress = ipAddress,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage,
                CreatedAt = DateTime.UtcNow
            };

            _context.ActivityLogs.Add(log);
            await _context.SaveChangesAsync();
        }
    }
}