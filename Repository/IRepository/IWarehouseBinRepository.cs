using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;
using System.Data;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseBinRepository
    {
        public Task<List<WarehouseBinResponseDTO>> GetBins(int shelfID);
        public Task<List<AllProductResponseDTO>> GetProductsBin(int binID);
        public Task<WarehouseBinResponseDTO> GetBin(int binID);
        public Task CreateBin(int shelfID, WarehouseBinRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task UpdateBin(int shelfID, WarehouseBinRequestDTO requestDTO, IDbConnection? connection, IDbTransaction? transaction);
        public Task DeleteBin(int? binID, int? shelfID, IDbConnection? connection, IDbTransaction? transaction);
    }
}
