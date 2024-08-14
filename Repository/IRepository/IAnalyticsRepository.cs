using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IAnalyticsRepository
    {
        public Task<IEnumerable<ProductAnalyticsResponseDTO>> GetProductAnalytics();
        public Task<IEnumerable<VendorAnalyticsResponseDTO>> GetVendorAnalytics(int VendorCount);
        public Task<IEnumerable<CategoryAnalyticsResponseDTO>> GetCategoryAnalytics(int CategoryCount);
    }
}
