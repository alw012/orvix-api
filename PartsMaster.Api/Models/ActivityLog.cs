namespace PartsMaster.Api.Models
{
    public class ActivityLog
    {
        public long Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? UserId { get; set; }
        public string? UserName { get; set; }
        public string? ShopId { get; set; }
        public string? ShopName { get; set; }
        public string? IpAddress { get; set; }
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}