namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseResponseDTO
    {
        public int WarehouseID { get; set; }
        public string WarehouseName { get; set; }
        public string WarehouseAddress { get; set; }
        public List<WarehouseFloorResponseDTO> Floors { get; set; }
    }
}
