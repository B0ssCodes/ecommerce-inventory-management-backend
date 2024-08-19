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
        private readonly IUserPermissionRepository _userPermissionRepository;
        public UserRoleRepository(DapperContext db, IUserPermissionRepository userPermissionRepository)
        {
            _db = db;
            _userPermissionRepository = userPermissionRepository;
        }

        public async Task CreateUserRole(UserRoleRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
                INSERT INTO user_role (role)
                VALUES (@Name)
                RETURNING user_role_id_pkey;";

                var parameters = new { Name = requestDTO.RoleName };
                int roleID = await connection.QuerySingleAsync<int>(query, parameters);

                if (roleID == 0)
                {
                    throw new Exception("Failed to create user role");
                }

                if (requestDTO.Permissions != null)
                {
                    foreach (var permission in requestDTO.Permissions)
                    {
                        int permissionID = await _userPermissionRepository.PermissionExists(new UserPermissionRequestDTO { Permission = permission });
                        if (permissionID == 0)
                        {
                            permissionID = await _userPermissionRepository.CreateUserPermission(new UserPermissionRequestDTO { Permission = permission });
                        }

                        var associateQuery = @"
                            INSERT INTO user_role_permission (user_role_id, user_permission_id)
                            VALUES (@UserRoleID, @UserPermissionID);";

                        var associateParameters = new { UserRoleID = roleID, UserPermissionID = permissionID };
                        await connection.ExecuteAsync(associateQuery, associateParameters);


                    }
                }
            }
        }

        public async Task DeleteUserRole(int roleId)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();

                var query = @"
                    UPDATE user_role
                    SET deleted = true
                    WHERE user_role_id_pkey = @RoleID;";

                var userQuery = @"
                    UPDATE user_info
                    SET deleted = true
                    WHERE user_role_id = @RoleID;";
                var parameters = new { RoleID = roleId };
                await connection.ExecuteAsync(userQuery, parameters);
                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task<UserRoleDTO> GetUserRole(int userRoleId)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                var query = @"
            SELECT ur.user_role_id_pkey AS UserRoleID,
                   ur.role AS Role,
                   ur.can_purchase AS CanPurchase,
                   up.user_permission_id_pkey AS UserPermissionID,
                   up.permission AS Permission
            FROM user_role ur
            LEFT JOIN user_role_permission urp ON ur.user_role_id_pkey = urp.user_role_id
            LEFT JOIN user_permission up ON up.user_permission_id_pkey = urp.user_permission_id
            WHERE ur.user_role_id_pkey = @UserRoleID";

                var parameters = new { UserRoleID = userRoleId };

                var userRoleDictionary = new Dictionary<int, UserRoleDTO>();

                var result = await connection.QueryAsync<UserRoleDTO, AllUserPermissionResponseDTO, UserRoleDTO>(
                    query,
                    (userRole, permission) =>
                    {
                        if (!userRoleDictionary.TryGetValue(userRole.UserRoleID, out var currentUserRole))
                        {
                            currentUserRole = userRole;
                            currentUserRole.Permissions = new List<AllUserPermissionResponseDTO>();
                            userRoleDictionary.Add(currentUserRole.UserRoleID, currentUserRole);
                        }

                        if (permission != null)
                        {
                            currentUserRole.Permissions.Add(permission);
                        }

                        return currentUserRole;
                    },
                    parameters,
                    splitOn: "UserPermissionID"
                );

                return userRoleDictionary.Values.FirstOrDefault();
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

        public async Task UpdateUserRole(int roleId, UserRoleRequestDTO requestDTO)
        {
            using (IDbConnection connection = _db.CreateConnection())
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Update the role name
                        var query = @"
                    UPDATE user_role
                    SET role = @RoleName, can_purchase = @CanPurchase
                    WHERE user_role_id_pkey = @Id
                    RETURNING user_role_id_pkey as UserRoleID, role AS Role, can_purchase AS CanPurchase";

                        var parameters = new { RoleName = requestDTO.RoleName, CanPurchase = requestDTO.CanPurchase, Id = roleId };
                        UserRoleDTO updatedUserRole = await connection.QueryFirstOrDefaultAsync<UserRoleDTO>(query, parameters, transaction);

                        if (updatedUserRole == null)
                        {
                            throw new Exception("Failed to update user role");
                        }


                        var deletePermissionsQuery = @"
                    DELETE FROM user_role_permission
                    WHERE user_role_id = @UserRoleID";

                        await connection.ExecuteAsync(deletePermissionsQuery, new { UserRoleID = roleId }, transaction);


                        if (requestDTO.Permissions != null)
                        {
                            foreach (var permission in requestDTO.Permissions)
                            {
                                int permissionID = await _userPermissionRepository.PermissionExists(new UserPermissionRequestDTO { Permission = permission });
                                if (permissionID == 0)
                                {
                                    permissionID = await _userPermissionRepository.CreateUserPermission(new UserPermissionRequestDTO { Permission = permission });
                                }

                                var associateQuery = @"
                            INSERT INTO user_role_permission (user_role_id, user_permission_id)
                            VALUES (@UserRoleID, @UserPermissionID)";

                                var associateParameters = new { UserRoleID = roleId, UserPermissionID = permissionID };
                                await connection.ExecuteAsync(associateQuery, associateParameters, transaction);
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw new Exception("Failed to update user role and permissions", ex);
                    }
                }
            }
        }
    }
}
