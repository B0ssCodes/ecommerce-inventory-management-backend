using Inventory_Management_Backend.Models.Dto.WarehouseDTO;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IInventoryLocationRepository
    {
        public Task<InventoryLocationResponseDTO> GetInventoryLocation(int locationID);
        public Task CreateInventoryLocation(InventoryLocationRequestDTO requestDTO);

        public Task DeleteInventoryLocation(int locationID);

        public Task UpdateInventoryLocation(int locationID, InventoryLocationUpdateDTO updateDTO);

    }
}
