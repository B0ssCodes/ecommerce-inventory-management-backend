namespace Inventory_Management_Backend.Models.Dto
{
    public class ProductWithoutInventoryDTO
    {
        public int ProductID { get; set; }
        public string Name { get; set; }
        public string SKU { get; set; }
        public decimal Price { get; set; }
    }
}
