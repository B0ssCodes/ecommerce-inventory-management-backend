﻿using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Models.Dto.WarehouseDTO;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IWarehouseBinRepository
    {
        public Task<List<WarehouseBinResponseDTO>> GetBins(int shelfID);
        public Task<List<AllProductResponseDTO>> GetProductsBin(int binID);
        public Task<WarehouseBinResponseDTO> GetBin(int binID);
        public Task CreateBin(int shelfID, WarehouseBinRequestDTO requestDTO);
        public Task UpdateBin(int binID, int shelfID, WarehouseBinRequestDTO requestDTO);
        public Task DeleteBin(int? binID, int? shelfID);
    }
}
