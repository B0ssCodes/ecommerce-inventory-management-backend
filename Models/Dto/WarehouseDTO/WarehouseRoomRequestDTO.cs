namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseRoomRequestDTO
    {
        public string RoomName { get; set; }
        public int? RoomCapacity { get; set; }
        public List<WarehouseAisleRequestDTO>? Aisles { get; set; }
    }
}
