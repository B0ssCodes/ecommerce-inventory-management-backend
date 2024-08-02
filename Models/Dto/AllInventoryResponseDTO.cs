namespace Inventory_Management_Backend.Models.Dto
{
    public class AllInventoryResponseDTO
    {
        public int InventoryID { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public string ProductName { get; set; } 
        public string ProductSKU { get; set; }
    }
}
