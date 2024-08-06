using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IInventoryRepository
    {
        // Checks if the inventory exists, returns the inventory ID if it exists and 0 if it doesn't
        public Task<int> InventoryExists(int productID);

        // Returns a list of all inventories
        public Task<(List<AllInventoryResponseDTO>, int)> GetInventories(PaginationParams paginationParams);

        // Returns a specific inventory
        public Task<InventoryResponseDTO> GetInventory(int inventoryID);
        
        // Returns a list of all low stock inventories
        public Task<(List<AllInventoryResponseDTO>, int)> GetLowStockInventories(int minStockQuantity, PaginationParams paginationParams);
        public Task<int> GetLowStockInventoriesCount(int minStockQuantity);

        // Returns a list of all out of stock inventories
        public Task<(List<ProductWithoutInventoryDTO>, int)> GetOutStockInventories(PaginationParams paginationParams);
        public Task<int> GetOutStockInventoriesCount();

        // Creates a new inventory based on the product ID
        public Task<int> CreateInventory(TransactionItemRequestDTO requestDTO);

        // Increases the inventory based on the inventory ID (if transaction type is Inbound)
        public Task IncreaseInventory(int inventoryID,TransactionItemRequestDTO requestDTO);

        // Decreases the inventory based on the inventory ID (if transaction type is Outbounf)
        public Task DecreaseInventory(int inventoryID, TransactionItemRequestDTO requestDTO);

        // Deletes the inventory based on the inventory ID (happens when quantity = 0)
        public Task DeleteInventory(int inventoryID);
    }
}
