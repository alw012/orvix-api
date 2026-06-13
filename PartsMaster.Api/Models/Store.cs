namespace PartsMaster.Api.Models
{
    public class Store
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Phone2 { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Location { get; set; }
        public bool IsActive { get; set; } = true;
        public int MaxDevices { get; set; } = 1;
        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // العلاقات
        public ICollection<Part> Parts { get; set; } = new List<Part>();
        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}
