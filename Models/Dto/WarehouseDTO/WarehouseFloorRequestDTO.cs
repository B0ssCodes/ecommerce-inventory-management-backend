namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseFloorRequestDTO
    {
        public string FloorName { get; set; }
        public List<WarehouseRoomRequestDTO>? Rooms { get; set; }
    }
}
