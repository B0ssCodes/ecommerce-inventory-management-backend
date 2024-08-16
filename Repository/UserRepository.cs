using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly DapperContext _db;

        public UserRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task DeleteUser(int userID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    UPDATE user_info
                    SET deleted = true
                    WHERE user_id_pkey = @UserID";

                await connection.ExecuteAsync(query, new { UserID = userID });

            }
        }

        public async Task<UserResponseDTO> GetUser(int userID)
        {
            if (userID == 0 || userID == null)
            {
                throw new Exception("Invalid or no User ID");
            }
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
            SELECT 
            user_id_pkey AS UserID,
            user_first_name AS FirstName,
            user_last_name AS LastName,
            user_email AS Email, 
            user_role_id_pkey AS UserRoleID,
            role AS Role 
            FROM user_mv
            WHERE user_id_pkey = @UserID;";

                var parameters = new { UserID = userID };

                UserResponseDTO userDTO = await connection.QueryFirstOrDefaultAsync<UserResponseDTO>(query, parameters);
                if (userDTO == null)
                {
                    throw new Exception("User not found");
                }

                return userDTO;
            }
        }

        public async Task<(List<UserResponseDTO>, int itemCount)> GetUsers(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var pageNumber = paginationParams.PageNumber;
                var pageSize = paginationParams.PageSize;
                var searchQuery = paginationParams.Search;
                var startRow = (pageNumber - 1) * pageSize;
                var endRow = pageNumber * pageSize;

                // CTE to get total count of users
                var query = @"
        WITH UserCTE AS (
            SELECT 
                row_num,
                user_id_pkey AS UserID,
                user_first_name AS FirstName,
                user_last_name AS LastName, 
                user_email AS Email, 
                user_role_id AS UserRoleID,
                role AS Role,
                item_count AS UserCount
            FROM user_mv
                   WHERE (@SearchQuery IS NULL OR 
                   user_first_name ILIKE '%' || @SearchQuery || '%' OR 
                   user_last_name ILIKE '%' || @SearchQuery || '%' OR 
                   user_email ILIKE '%' || @SearchQuery || '%')
        )
        SELECT UserID, FirstName, LastName, Email, UserRoleID, Role, UserCount
        FROM UserCTE
        WHERE row_num > @StartRow AND row_num <= @EndRow
        ORDER BY row_num;";

                var parameters = new
                {
                    StartRow = startRow,
                    EndRow = endRow,
                    PageSize = pageSize,
                    SearchQuery = searchQuery
                };

                var result = await connection.QueryAsync<UserResponseDTO, long, (UserResponseDTO, long)>(
                    query,
                    (user, userCount) => (user, userCount),
                    parameters,
                    splitOn: "UserCount"
                );

                var users = result.Select(r => r.Item1).ToList();
                int totalCount = result.Any() ? (int)result.First().Item2 : 0; // Explicitly cast to int

                return (users, totalCount);
            }
        }
        
        public async Task UpdateUser(int userID, UserUpdateDTO updateDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    UPDATE user_info
                    SET user_first_name = @FirstName,
                        user_last_name = @LastName,
                        user_role_id = @RoleID
                    WHERE user_id_pkey = @UserID";

                var parameters = new
                {
                    UserID = userID,
                    FirstName = updateDTO.FirstName,
                    LastName = updateDTO.LastName,
                    RoleID = updateDTO.UserRoleID
                };

                await connection.ExecuteAsync(query, parameters);
            }
        }
    }
}
