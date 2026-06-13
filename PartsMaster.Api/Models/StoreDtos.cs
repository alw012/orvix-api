namespace PartsMaster.Api.Models
{
    public class StoreDto
    {
        public string Name { get; set; } = "";
        public string City { get; set; } = "";
        public string Phone { get; set; } = "";
        public string? Phone2 { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Location { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int MaxDevices { get; set; } = 1;
    }
}
