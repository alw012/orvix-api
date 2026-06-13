namespace PartsMaster.Api.Models
{
    public class Device
    {
        public int Id { get; set; }
        public string DeviceId { get; set; } = "";
        public string Name { get; set; } = "";
        public string MacAddress { get; set; } = "";
        public string HardwareFingerprint { get; set; } = "";
        public int StoreId { get; set; }
        public Store? Store { get; set; }
        public bool IsOnline { get; set; } = false;
        public DateTime? LastSeen { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
