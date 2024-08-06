using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly DapperContext _db;

        public CategoryRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task CreateCategory(CategoryRequestDTO categoryDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = "INSERT INTO category (category_name, category_description) VALUES (@Name, @Description);";
                var parameters = new { Name = categoryDTO.Name, Description = categoryDTO.Description };
                await connection.QuerySingleOrDefaultAsync(query, parameters);

            }
        }

        public async Task DeleteCategory(int categoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {

                    // Get the list of product IDs related to the category
                    var getProductIdsQuery = "SELECT product_id_pkey FROM product WHERE category_id = @CategoryID;";
                    var productIds = await connection.QueryAsync<int>(getProductIdsQuery, new { CategoryID = categoryID }, transaction);

                    // Delete related images from the database
                    var deleteImagesQuery = "DELETE FROM image WHERE product_id = @ProductID;";
                    foreach (var productId in productIds)
                    {
                        await connection.ExecuteAsync(deleteImagesQuery, new { ProductID = productId }, transaction);

                        // Delete the image folders
                        var imageFolderPath = Path.Combine("ProductImages", productId.ToString());
                        if (Directory.Exists(imageFolderPath))
                        {
                            Directory.Delete(imageFolderPath, true);
                            Console.WriteLine($"Deleted image folder for product ID: {productId}");
                        }
                    }

                    // Delete related products
                    var deleteProductsQuery = "DELETE FROM product WHERE category_id = @CategoryID;";
                    await connection.ExecuteAsync(deleteProductsQuery, new { CategoryID = categoryID }, transaction);

                    // Delete the category
                    var deleteCategoryQuery = "DELETE FROM category WHERE category_id_pkey = @CategoryID;";
                    await connection.ExecuteAsync(deleteCategoryQuery, new { CategoryID = categoryID }, transaction);

                    transaction.Commit();

                }
            }
        }


        public async Task<(List<CategoryResponseDTO>, int ItemCount)> GetCategories(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
                var searchQuery = paginationParams.Search;

                var query = @"
            WITH CategoryCTE AS (
                SELECT 
                    category_id_pkey AS CategoryID, 
                    category_name AS Name, 
                    category_description AS Description,
                    COUNT(*) OVER() AS TotalCount
                FROM category
                WHERE (@SearchQuery IS NULL OR 
                       category_name ILIKE '%' || @SearchQuery || '%' OR 
                       category_description ILIKE '%' || @SearchQuery || '%')
            )
            SELECT CategoryID, Name, Description, TotalCount
            FROM CategoryCTE
            ORDER BY Name
            OFFSET @Offset ROWS
            FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
                    SearchQuery = searchQuery
                };

                var result = await connection.QueryAsync<CategoryResponseDTO, long, (CategoryResponseDTO, long)>(
                    query,
                    (category, totalCount) => (category, totalCount),
                    parameters,
                    splitOn: "TotalCount"
                );

                var categories = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0;

                return (categories, totalCount);
            }
        }

        public async Task<CategoryResponseDTO> GetCategory(int categoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT category_id_pkey AS CategoryID, category_name AS Name, category_description AS Description
                    FROM category
                    WHERE category_id_pkey = @CategoryID";

                var parameters = new { CategoryID = categoryID };

                CategoryResponseDTO response = await connection.QueryFirstOrDefaultAsync<CategoryResponseDTO>(query, parameters);
                if (response == null)
                {
                    throw new Exception("Category not found");
                }
                return response;
            }
        }

        public async Task UpdateCategory(int categoryID, CategoryRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            UPDATE category
            SET category_name = @Name,
                category_description = @Description
            WHERE category_id_pkey = @CategoryID";

                var parameters = new
                {
                    Name = requestDTO.Name,
                    Description = requestDTO.Description,
                    CategoryID = categoryID
                };

                await connection.QuerySingleOrDefaultAsync(query, parameters);

            }
        }
    }
}
