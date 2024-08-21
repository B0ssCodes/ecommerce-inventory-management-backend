namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseFloorResponseDTO
    {
        public int FloorID { get; set; }
        public string FloorName { get; set; }
        public List<WarehouseRoomResponseDTO> Rooms { get; set; }
    }
}
