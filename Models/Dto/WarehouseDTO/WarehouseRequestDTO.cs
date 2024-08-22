namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseRequestDTO
    {
        public int? WarehouseID {get;set;}
        public string WarehouseName { get; set; }
        public string WarehouseAddress { get; set; }
        public List<WarehouseFloorRequestDTO>? Floors { get; set; }
    }
}
