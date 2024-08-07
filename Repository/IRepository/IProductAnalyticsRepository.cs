using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IProductAnalyticsRepository
    {
        public Task<List<ProductAnalyticsResponseDTO>> GetProductAnalytics(int refreshDays);
        public Task ResetProductAnalyticsCache();
    }
}
