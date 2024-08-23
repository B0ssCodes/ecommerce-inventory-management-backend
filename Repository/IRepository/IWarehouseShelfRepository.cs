using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using System.Data;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseShelfRepository
    {
        public Task<List<WarehouseShelfResponseDTO>> GetShelves(int aisleID);
        public Task<WarehouseShelfResponseDTO> GetShelf(int shelfID);
        public Task CreateOrUpdateShelves(int aisleID, List<WarehouseShelfRequestDTO> requestDTOs, IDbConnection? connection, IDbTransaction? transaction);
        public Task CreateShelf(int aisleID, WarehouseShelfRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task UpdateShelf(int aisleID, WarehouseShelfRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task DeleteShelf(int? shelfID, int? aisleID, IDbConnection? connection, IDbTransaction? transaction);
    }
}
