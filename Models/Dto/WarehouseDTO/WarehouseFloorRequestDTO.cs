namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseFloorRequestDTO
    {
        public int? FloorID { get; set; }
        public string FloorName { get; set; }
        public List<WarehouseRoomRequestDTO>? Rooms { get; set; }
    }
}
