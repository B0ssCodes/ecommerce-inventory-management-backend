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
        private readonly IProductRepository _productRepository;
        public CategoryRepository(DapperContext db, IProductRepository productRepository)
        {
            _db = db;
            _productRepository = productRepository;
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

                var query = @"
                    UPDATE category
                    SET deleted = true
                    WHERE category_id_pkey = @CategoryID;";

                var parameters = new { CategoryID = categoryID };

                var productQuery = @"
                    UPDATE product
                    SET deleted = true
                    WHERE category_id = @CategoryID;";

                await connection.QuerySingleOrDefaultAsync(productQuery, parameters);
                await connection.QuerySingleOrDefaultAsync(query, parameters);
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

        public async Task<CategoryProductsResponseDTO> GetCategoryProducts(int categoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            SELECT 
                c.category_id_pkey AS CategoryID,
                c.category_name AS CategoryName,
                p.product_id_pkey AS ProductID,
                p.sku AS SKU,
                p.product_name AS Name,
                p.product_description AS Description,
                p.product_price AS Price,
                p.product_cost_price AS Cost
            FROM category c
            LEFT JOIN product p ON c.category_id_pkey = p.category_id
            WHERE c.category_id_pkey = @CategoryID AND p.deleted = false;";

                var parameters = new { CategoryID = categoryID };

                var categoryDictionary = new Dictionary<int, CategoryProductsResponseDTO>();

                var result = await connection.QueryAsync<CategoryProductsResponseDTO, AllProductCategoryResponseDTO, CategoryProductsResponseDTO>(
                    query,
                    (category, product) =>
                    {
                        if (!categoryDictionary.TryGetValue(category.CategoryID, out var categoryEntry))
                        {
                            categoryEntry = category;
                            categoryEntry.Products = new List<AllProductCategoryResponseDTO>();
                            categoryDictionary.Add(category.CategoryID, categoryEntry);
                        }

                        if (product != null)
                        {
                            categoryEntry.Products.Add(product);
                        }

                        return categoryEntry;
                    },
                    parameters,
                    splitOn: "ProductID"
                );

                return categoryDictionary.Values.FirstOrDefault();
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
