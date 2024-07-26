using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface ICategoryRepository
    {
        public Task<List<CategoryResponseDTO>> GetCategories();
        public Task<CategoryResponseDTO> GetCategory(int categoryID);
        public Task<CategoryResponseDTO> CreateCategory(string categoryName);
        public Task<CategoryResponseDTO> UpdateCategory(CategoryRequestDTO requestDTO);
        public Task<bool> DeleteCategory(int categoryID);
    }
}
