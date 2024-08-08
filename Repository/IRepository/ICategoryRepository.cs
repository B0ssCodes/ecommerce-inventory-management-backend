using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface ICategoryRepository
    {
        public Task<(List<CategoryResponseDTO>, int ItemCount)> GetCategories(PaginationParams paginationParams);
        public Task<CategoryResponseDTO> GetCategory(int categoryID);
        public Task<CategoryProductsResponseDTO> GetCategoryProducts(int categoryID);
        public Task CreateCategory(CategoryRequestDTO requestDTO);
        public Task UpdateCategory(int categoryID, CategoryRequestDTO requestDTO);
        public Task DeleteCategory(int categoryID);
    }
}
