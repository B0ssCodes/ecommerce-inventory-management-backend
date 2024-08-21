namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseRoomResponseDTO
    {
        public int RoomID { get; set; }
        public string RoomName { get; set; }
        public List<WarehouseAisleResponseDTO> Aisles { get; set; }
    }
}
