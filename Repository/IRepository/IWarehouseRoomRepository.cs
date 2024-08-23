using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using System.Data;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseRoomRepository
    {
        public Task<WarehouseRoomResponseDTO> GetRoom(int roomID);
        public Task<List<WarehouseRoomResponseDTO>> GetRooms(int floorID);
        public Task CreateOrUpdateRooms(int floorID, List<WarehouseRoomRequestDTO> requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task CreateRoom(int floorID, WarehouseRoomRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task UpdateRoom(WarehouseRoomRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task DeleteRoom(int? roomID, int? floorID, IDbConnection? connection, IDbTransaction? transaction);
    }
}
