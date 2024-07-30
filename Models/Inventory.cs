namespace Inventory_Management_Backend.Models
{
    public class Inventory
    {
        public int InventoryID { get; set; }
        public int Stock { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public int ProductID { get; set; }
    }
}
