using Dapper;
using Inventory_Management_Backend.Data;
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

        public async Task<CategoryResponseDTO> CreateCategory(string categoryName)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = "INSERT INTO category (category_name) VALUES (@Name) RETURNING category_id_pkey AS CategoryID, category_name AS Name;";
                var parameters = new { Name = categoryName }; 
                CategoryResponseDTO response = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(query, parameters);
                if (response == null)
                {
                    throw new Exception("Category creation failed");
                }
                return response;
            }
        }

        public async Task<bool> DeleteCategory(int categoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"DELETE FROM category WHERE category_id_pkey = @CategoryID;";
                var parameters = new { CategoryID = categoryID };

                await connection.ExecuteAsync(query, parameters);
                return true;
            }
        }

        public async Task<List<CategoryResponseDTO>> GetCategories()
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT category_id_pkey AS CategoryID, category_name AS Name
                    FROM category;";

                var response = await connection.QueryAsync<CategoryResponseDTO>(query);
                return response.ToList();
            }
        }

        public async Task<CategoryResponseDTO> GetCategory(int categoryID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT category_id_pkey AS CategoryID, category_name AS Name
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

        public async Task<CategoryResponseDTO> UpdateCategory(CategoryRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            UPDATE category
            SET category_name = @Name
            WHERE category_id_pkey = @CategoryID
            RETURNING category_id_pkey AS CategoryID, category_name AS Name;";

                var parameters = new
                {
                    Name = requestDTO.Name,
                    CategoryID = requestDTO.CategoryID
                };

                var updatedCategory = await connection.QuerySingleOrDefaultAsync<CategoryResponseDTO>(query, parameters);

                if (updatedCategory == null)
                {
                    throw new Exception("Category update failed");
                }

                return updatedCategory;
            }
        }
    }
}
