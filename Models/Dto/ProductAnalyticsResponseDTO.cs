namespace Inventory_Management_Backend.Models.Dto
{
    public class ProductAnalyticsResponseDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public decimal ProductCost { get; set; }
        public decimal ProductPrice { get; set; }
        public int UnitsBought { get; set; }
        public int UnitsSold { get; set; }
        public decimal MoneySpent { get; set; }
        public decimal MoneyEarned { get; set; }
        public decimal Profit { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
    }
}
