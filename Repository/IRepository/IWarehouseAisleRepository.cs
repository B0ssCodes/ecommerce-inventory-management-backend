using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using System.Data;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseAisleRepository
    {
        public Task<List<WarehouseAisleResponseDTO>> GetAisles(int roomID);
        public Task<WarehouseAisleResponseDTO> GetAisle(int aisleID);
        public Task CreateOrUpdateAisles(int roomID, List<WarehouseAisleRequestDTO> requestDTOs, IDbConnection? connection, IDbTransaction? transaction);
        public Task CreateAisle(int roomID, WarehouseAisleRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task UpdateAisle(WarehouseAisleRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task DeleteAisle(int? aisleID, int? roomID, IDbConnection? connection, IDbTransaction? transaction);
    }
}
