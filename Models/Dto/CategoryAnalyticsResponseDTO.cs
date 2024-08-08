namespace Inventory_Management_Backend.Models.Dto
{
    public class CategoryAnalyticsResponseDTO
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public int ProductsSold { get; set; }
        public decimal StockValue { get; set; }
        
    }
}
