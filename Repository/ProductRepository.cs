using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly DapperContext _db;
        public ProductRepository(DapperContext db)
        {
            _db = db;
        }
        public async Task<ProductResponseDTO> CreateProduct(ProductRequestDTO productRequestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Insert the product and return all available details
                        var query = @"
                    INSERT INTO Product (sku, product_name, product_description, product_price, product_cost_price, category_id)
                    VALUES (@SKU, @Name, @Description, @Price, @Cost, @CategoryID)
                    RETURNING product_id_pkey, sku, product_name, product_description, product_price, product_cost_price, category_id";

                        var insertedProduct = await connection.QuerySingleOrDefaultAsync<ProductResponseDTO>(query, new
                        {
                            productRequestDTO.SKU,
                            productRequestDTO.Name,
                            productRequestDTO.Description,
                            productRequestDTO.Price,
                            productRequestDTO.Cost,
                            productRequestDTO.CategoryID
                        }, transaction);

                        if (insertedProduct == null)
                        {
                            throw new Exception("Product creation failed");
                        }

                        // Insert images
                        var imageQuery = @"
                    INSERT INTO Image (product_id, image_url)
                    VALUES (@ProductID, @ImageUrl)";

                        foreach (var image in productRequestDTO.Images)
                        {
                            await connection.ExecuteAsync(imageQuery, new
                            {
                                ProductID = insertedProduct.ProductID,
                                ImageUrl = image.Url
                            }, transaction);
                        }

                        // Get the category details based on the passed ID
                        var categoryQuery = @"
                    SELECT category_id AS CategoryID, category_name AS CategoryName
                    FROM Category
                    WHERE category_id = @CategoryID";

                        var category = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(categoryQuery, new
                        {
                            CategoryID = productRequestDTO.CategoryID
                        }, transaction);

                        if (category == null)
                        {
                            throw new Exception("Category not found");
                        }

                        // Commit the transaction
                        transaction.Commit();

                        // Set the category in the response DTO
                        insertedProduct.Category = category;

                        return insertedProduct;
                    }
                    catch
                    {
                        // Rollback the transaction if any error occurs
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public Task<bool> DeleteProduct(int productID)
        {
            throw new NotImplementedException();
        }

        public Task<ProductResponseDTO> GetProduct(int productID)
        {
            throw new NotImplementedException();
        }

        public Task<List<ProductResponseDTO>> GetProducts()
        {
            throw new NotImplementedException();
        }

        public Task<ProductResponseDTO> UpdateProduct(int productID, ProductRequestDTO productRequestDTO)
        {
            throw new NotImplementedException();
        }
    }
}
