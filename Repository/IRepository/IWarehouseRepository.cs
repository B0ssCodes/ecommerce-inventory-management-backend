using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseRepository
    {
        public Task<List<AllWarehouseResponseDTO>> GetWarehouses(PaginationParams paginationParams);
        public Task<WarehouseResponseDTO> GetWarehouse(int warehouseID);
        public Task CreateWarehouse(WarehouseRequestDTO requestDTO);
        public Task UpdateWarehouse(int warehouseID, WarehouseRequestDTO requestDTO);
        public Task DeleteWarehouse(int warehouseID);

    }
}
