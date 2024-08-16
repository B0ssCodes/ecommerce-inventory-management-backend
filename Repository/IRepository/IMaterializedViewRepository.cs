using Inventory_Management_Backend.Models;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IMaterializedViewRepository
    {
        public Task CreateProductMV();
        public Task CreateCategoryMV();
        public Task CreateVendorMV();

        public Task RefreshProductMV();
        public Task RefreshCategoryMV();
        public Task RefreshVendorMV();
    }
}
