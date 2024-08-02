using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly DapperContext _db;

        public UserRoleRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task<UserRoleDTO> CreateUserRole(UserRoleRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            INSERT INTO user_role (role)
            VALUES (@Name)
            RETURNING user_role_id_pkey AS UserRoleID, role AS Role";

                var parameters = new { Name = requestDTO.RoleName };
                UserRoleDTO userDTO = await connection.QueryFirstOrDefaultAsync<UserRoleDTO>(query, parameters);

                if (userDTO == null)
                {
                    throw new Exception("Failed to create user role");
                }

                return userDTO;
            }
        }

        public async Task DeleteUserRole(int roleId)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {

                        var deleteUserQuery = @"
                        DELETE FROM user_info
                        WHERE user_role_id = @RoleID;";

                        var deleteUserParameters = new { RoleID = roleId };
                        await connection.ExecuteAsync(deleteUserQuery, deleteUserParameters, transaction);

                        var deleteUserRoleQuery = @"
                            DELETE FROM user_role 
                            WHERE user_role_id_pkey = @RoleID";

                        var deleteUserRoleParameters = new { RoleID = roleId };
                        await connection.ExecuteAsync(deleteUserRoleQuery, deleteUserRoleParameters, transaction);

                        transaction.Commit();
                    }
                    catch(Exception ex)
                    {
                        transaction.Rollback();
                    }
                }
                    
            }
        }

        public async Task<UserRoleDTO> GetUserRole(int userRoleId)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    SELECT user_role_id_pkey AS UserRoleID, role AS Role
                    FROM user_role
                    WHERE user_role_id_pkey = @UserRoleID";

                var parameters = new { UserRoleID = userRoleId };

                UserRoleDTO userRoleDTO = await connection.QueryFirstOrDefaultAsync<UserRoleDTO>(query, parameters);
                if (userRoleDTO == null)
                {
                    throw new Exception("User role not found");
                }
                return userRoleDTO;
            }
        }

        public async Task<(List<UserRoleDTO>, int)> GetUserRoles(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            WITH UserRoleCTE AS (
                SELECT 
                    user_role_id_pkey AS UserRoleID, 
                    role AS Role,
                    COUNT(*) OVER() AS TotalCount
                FROM user_role
                WHERE @Search IS NULL OR role ILIKE '%' || @Search || '%'
            )
            SELECT 
                UserRoleID, 
                Role, 
                TotalCount
            FROM UserRoleCTE
            ORDER BY Role
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                var parameters = new
                {
                    Search = paginationParams.Search,
                    Offset = (paginationParams.PageNumber - 1) * paginationParams.PageSize,
                    PageSize = paginationParams.PageSize
                };

                var userRoles = await connection.QueryAsync<UserRoleDTO, long, (UserRoleDTO, long)>(query,
                    (userRole, totalCount) =>
                    {
                        return (userRole, totalCount);
                    },
                    parameters,
                    splitOn: "TotalCount");

                if (userRoles == null)
                {
                    throw new Exception("No user roles found");
                }

                List<UserRoleDTO> userRoleList = userRoles.Select(x => x.Item1).ToList();
                int totalItems = (int)userRoles.Select(x => x.Item2).FirstOrDefault();

                return (userRoleList, totalItems);
            }
        }

        public async Task<UserRoleDTO> UpdateUserRole(int roleId, UserRoleRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    UPDATE user_role
                    SET role = @RoleName
                    WHERE user_role_id_pkey = @Id
                    RETURNING user_role_id_pkey as UserRoleID, role AS Role";

                var parameters = new { RoleName = requestDTO.RoleName, Id = roleId };
                UserRoleDTO updatedUserRole = await connection.QueryFirstOrDefaultAsync<UserRoleDTO>(query, parameters);

                if (updatedUserRole == null)
                {
                    throw new Exception("Failed to update user role");
                }
                return updatedUserRole;
            }
        }
    }
}
