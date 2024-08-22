namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseRoomRequestDTO
    {
        public int? RoomID { get; set; }
        public string RoomName { get; set; }
        public List<WarehouseAisleRequestDTO>? Aisles { get; set; }
    }
}
