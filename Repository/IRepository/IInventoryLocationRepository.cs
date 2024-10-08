﻿using Inventory_Management_Backend.Models.Dto.WarehouseDTO;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IInventoryLocationRepository
    {
        public Task<InventoryLocationResponseDTO> GetInventoryLocation(int? inventoryID, int? locationID);
        public Task CreateInventoryLocation(InventoryLocationRequestDTO requestDTO);

        public Task DeleteInventoryLocation(int locationID);

        public Task UpdateInventoryLocation(InventoryLocationRequestDTO requestDTO);

    }
}
