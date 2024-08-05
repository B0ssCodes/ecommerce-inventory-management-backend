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

                var query = "DELETE FROM user_info WHERE user_id_pkey = @UserID";

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
            u.user_id_pkey AS UserID,
            user_first_name AS FirstName,
            u.user_last_name AS LastName,
            u.user_email AS Email, 
            r.user_role_id_pkey AS RoleID,
            r.role AS Role 
            FROM user_info u
            INNER JOIN user_role r
            ON u.user_role_id = r.user_role_id_pkey
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

                var offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize;
                var searchQuery = paginationParams.Search;

                // CTE to get total count of users
                var query = @"
        WITH UserCTE AS (
            SELECT 
                u.user_id_pkey AS UserID,
                u.user_first_name AS FirstName,
                u.user_last_name AS LastName, 
                u.user_email AS Email, 
                r.role AS Role,
                COUNT(*) OVER() AS UserCount
            FROM user_info u
            INNER JOIN user_role r ON u.user_role_id = r.user_role_id_pkey
            WHERE (@SearchQuery IS NULL OR 
                   u.user_first_name ILIKE '%' || @SearchQuery || '%' OR 
                   u.user_last_name ILIKE '%' || @SearchQuery || '%' OR 
                   u.user_email ILIKE '%' || @SearchQuery || '%')
        )
        SELECT UserID, FirstName, LastName, Email, Role, UserCount
        FROM UserCTE
        ORDER BY FirstName
        OFFSET @Offset ROWS
        FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Offset = offset,
                    PageSize = paginationParams.PageSize,
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
        // TODO: Implement UpdateUser method, no use for it now though
        //public Task UpdateUser(RegisterRequestDTO user)
        //{
        //    using (IDbConnection connection = _db.CreateConnection())
        //    {
        //        connection.Open();

        //        var query = @"
        //            UPDATE user
        //            SET user_first_name = @FirstName,
        //                user_last_name = @LastName,
        //                user_email = @Email,
        //                ";
        //    }
        //}
    }
}
