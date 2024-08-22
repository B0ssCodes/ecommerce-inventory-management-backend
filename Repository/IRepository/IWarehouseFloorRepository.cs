using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using System.Data;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseFloorRepository
    {
        public Task<List<WarehouseFloorResponseDTO>> GetFloors(int warehouseID);
        public Task<WarehouseFloorResponseDTO> GetFloor(int floorID);
        public Task CreateFloor(int warehouseID, WarehouseFloorRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task UpdateFloor(int floorID, WarehouseFloorRequestDTO requestDTO);
        public Task DeleteFloor(int? floorID, int? warehouseID);

    }
}
