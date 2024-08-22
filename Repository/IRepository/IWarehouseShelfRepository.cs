using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using System.Data;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseShelfRepository
    {
        public Task<List<WarehouseShelfResponseDTO>> GetShelves(int aisleID);
        public Task<WarehouseShelfResponseDTO> GetShelf(int shelfID);
        public Task CreateShelf(int aisleID, WarehouseShelfRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task UpdateShelf(int shelfID, int aisleID , WarehouseShelfRequestDTO requestDTO);
        public Task DeleteShelf(int? shelfID, int? aisleID);
    }
}
