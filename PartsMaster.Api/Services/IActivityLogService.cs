namespace PartsMaster.Api.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string action, string module, string? description = null,
            string? userId = null, string? userName = null,
            string? shopId = null, string? shopName = null,
            string? ipAddress = null, bool isSuccess = true,
            string? errorMessage = null);
    }
}