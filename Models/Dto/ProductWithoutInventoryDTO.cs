namespace Inventory_Management_Backend.Models.Dto
{
    public class ProductWithoutInventoryDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; }
        public string ProductSKU { get; set; }
        public decimal ProductPrice { get; set; }
    }
}
