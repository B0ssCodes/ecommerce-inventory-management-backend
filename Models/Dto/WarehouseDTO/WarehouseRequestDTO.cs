namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseRequestDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public List<WarehouseFloorRequestDTO>? Floors { get; set; }
    }
}
