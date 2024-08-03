namespace Inventory_Management_Backend.Models.Dto
{
    public class AllInventoryResponseDTO
    {
        public int InventoryID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string ProductName { get; set; } 
        public string ProductSKU { get; set; }
        public decimal ProductPrice { get; set; }
    }
}
