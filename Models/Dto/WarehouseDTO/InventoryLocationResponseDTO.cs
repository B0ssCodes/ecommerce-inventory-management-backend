namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class InventoryLocationResponseDTO
    {
        public int InventoryLocationID { get; set; }
        public int InventoryID { get; set; }
        public int BinID { get; set; }
        public string BinName { get; set; }
        public int ShelfID { get; set; }
        public string ShelfName { get; set; }
        public int AisleID { get; set; }
        public string AisleName { get; set; }
        public int RoomID { get; set; }
        public string RoomName { get; set; }
        public int FloorID { get; set; }
        public string FloorName { get; set; }
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; }


    }
}
