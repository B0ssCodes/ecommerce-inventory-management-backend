using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IVendorRepository
    {
        public Task<List<VendorResponseDTO>> GetVendors(PaginationParams paginationParams);
        public Task<VendorResponseDTO> GetVendor(int vendorId);
        public Task CreateVendor(VendorRequestDTO vendorRequestDTO);
        public Task UpdateVendor(int vendorId, VendorRequestDTO vendorRequestDTO);
        public Task DeleteVendor(int vendorId);

    }
}
