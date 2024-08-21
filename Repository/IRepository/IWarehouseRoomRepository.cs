using Inventory_Management_Backend.Models.Dto.WarehouseDTO;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseRoomRepository
    {
        public Task<WarehouseRoomResponseDTO> GetRoom(int roomID);
        public Task<List<WarehouseRoomResponseDTO>> GetRooms(int floorID);
        public Task CreateRoom(int floorID, WarehouseRoomRequestDTO requestDTO);
        public Task UpdateRoom(int roomID, WarehouseRoomRequestDTO requestDTO);
        public Task DeleteRoom(int? roomID, int? floorID);
    }
}
