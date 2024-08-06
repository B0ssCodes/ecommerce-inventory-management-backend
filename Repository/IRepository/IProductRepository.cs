using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using System.Net;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IProductRepository
    {
        public Task<(List<AllProductResponseDTO>, int itemCount)> GetProducts(PaginationParams paginationParams);
        public Task<(List<ProductSelectResponseDTO>, int itemCount)> GetProductsSelect(PaginationParams paginationParams);
        public Task<ProductResponseDTO> GetProduct(int productID);
        public Task<ProductResponseDTO> CreateProduct(ProductRequestDTO productRequestDTO);
        public Task<ProductResponseDTO> UpdateProduct(int productID, ProductRequestDTO productRequestDTO);
        public Task<bool> DeleteProduct(int productID);
    }
}
