using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;
using System.Data;

namespace Inventory_Management_Backend.Repository
{
    public class UserPermissionRepository : IUserPermissionRepository
    {
        private readonly DapperContext _db;

        public UserPermissionRepository(DapperContext db)
        {
            _db = db;
        }
        public async Task<int> CreateUserPermission(UserPermissionRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    INSERT INTO user_permission (permission)
                    VALUES (@Permission)
                    RETURNING user_permission_id_pkey;";

                var parameters = new
                {
                    Permission = requestDTO.Permission
                };

               int result =  await connection.QuerySingleAsync<int>(query, parameters);
               return result;
            }
        }

        public async Task DeleteUserPermission(int userPermissionID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                    DELETE FROM user_permission
                    WHERE user_permission_id_pkey = @UserPermissionID;";

                var parameters = new { UserPermissionID = userPermissionID };

                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task<UserPermissionResponseDTO> GetUserPermission(int userPermissionID)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                SELECT urp.user_permission_id AS UserPermissionID,
                       urp.user_role_id AS UserRoleID,
                       p.permission AS Permission
                FROM user_role_permission urp
                JOIN permission p ON urp.user_permission_id = p.user_permission_id_pkey
                WHERE urp.user_permission_id = @UserPermissionID;";

                var parameters = new { UserPermissionID = userPermissionID };

                UserPermissionResponseDTO userPermission = await connection.QueryFirstOrDefaultAsync<UserPermissionResponseDTO>(query, parameters);
                if (userPermission == null)
                {
                    throw new Exception("User Permission not found");
                }
                return userPermission;
            }

        }

        public async Task<List<UserPermissionResponseDTO>> GetUserPermissions(PaginationParams paginationParams)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                SELECT urp.user_permission_id AS UserPermissionID,
                       urp.user_role_id AS UserRoleID,
                       p.permission AS Permission
                FROM user_role_permission urp
                JOIN permission p ON urp.user_permission_id = p.user_permission_id_pkey;";

                List<UserPermissionResponseDTO> userPermissions = (await connection.QueryAsync<UserPermissionResponseDTO>(query)).ToList();

                return userPermissions;
            }
        }

        public async Task<int> PermissionExists(UserPermissionRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                SELECT user_permission_id_pkey
                FROM user_permission
                WHERE permission = @Permission
                LIMIT 1;";

                var parameters = new { Permission = requestDTO.Permission };

                int userPermissionID = await connection.QueryFirstOrDefaultAsync<int>(query, parameters);
                return userPermissionID;
            }
        }

        public async Task UpdateUserPermission(int userPermissionID, UserPermissionRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                UPDATE user_permission
                SET permission = @Permission
                WHERE user_permission_id_pkey = @UserPermissionID;";

                var parameters = new
                {
                    Permission = requestDTO.Permission,
                    UserPermissionID = userPermissionID
                };

                await connection.ExecuteAsync(query, parameters);

            }
        }
    }
}
