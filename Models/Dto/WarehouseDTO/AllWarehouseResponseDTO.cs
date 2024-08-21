namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class AllWarehouseResponseDTO
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; }
        public string WarehouseAddress { get; set; }
        public int FloorCount { get; set; }
    }
}
