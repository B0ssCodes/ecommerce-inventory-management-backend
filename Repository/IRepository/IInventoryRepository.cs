using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IInventoryRepository
    {
        public Task<int> InventoryExists(int productID);
        public Task<(List<AllInventoryResponseDTO>, int)> GetInventories(PaginationParams paginationParams);
        public Task<InventoryResponseDTO> GetInventory(int inventoryID);
        public Task<int> CreateInventory(TransactionItemRequestDTO requestDTO);
        public Task IncreaseInventory(int inventoryID,TransactionItemRequestDTO requestDTO);
        public Task DecreaseInventory(int inventoryID, TransactionItemRequestDTO requestDTO);
        public Task DeleteInventory(int inventoryID);
    }
}
