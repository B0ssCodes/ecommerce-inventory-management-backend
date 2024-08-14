namespace Inventory_Management_Backend.Models.Dto
{
    public class VendorAnalyticsResponseDTO
    {
        public int VendorID { get; set; }
        public string VendorName { get; set; }
        public string VendorEmail { get; set; }
        public int ProductsSold { get; set; }
        public decimal StockValue { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
