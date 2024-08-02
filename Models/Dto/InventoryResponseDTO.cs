using System.Diagnostics.Contracts;

namespace Inventory_Management_Backend.Models.Dto
{
    public class InventoryResponseDTO
    {
        public int InventoryID { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int ProductID { get; set; }
    }
}
