namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class InventoryLocationRequestDTO
    {
        public int? WarehouseID { get; set; }
        public int? FloorID { get; set; }
        public int? RoomID { get; set; }
        public int? AisleID { get; set; }
        public int? ShelfID { get; set; }
        public int BinID { get; set; }
        public int InventoryID { get; set; }
    }
}
