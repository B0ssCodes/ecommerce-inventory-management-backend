namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class AllWarehouseRoomResponseDTO
    {
        public int RoomID { get; set; }
        public string RoomName { get; set; }
        public string RoomDescription { get; set; }
        public int RoomCurrentStock { get; set; }
    }
}
