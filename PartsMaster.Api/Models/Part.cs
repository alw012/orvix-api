namespace PartsMaster.Api.Models
{
    public class Part
    {
        public int Id { get; set; }
        public string PartNumber { get; set; } = "";
        public string Type { get; set; } = "";
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal CompanyPrice { get; set; }
        public int StoreId { get; set; }

        // العلاقة مع المحل
        public Store? Store { get; set; }
    }
}