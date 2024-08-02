namespace Inventory_Management_Backend.Models.Dto
{
    public class InventoryRequestDTO
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }
}
