﻿using Dapper;
using Inventory_Management_Backend.Data;
using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;
using Inventory_Management_Backend.Repository.IRepository;

namespace Inventory_Management_Backend.Repository
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly DapperContext _db;

        public UserRoleRepository(DapperContext db)
        {
            _db = db;
        }

        public async Task<UserRoleDTO> CreateUserRole(string roleName)
        {
            using (var connection = _db.CreateConnection())
            {
                var query = @"
            INSERT INTO user_role (role)
            VALUES (@Name)
            RETURNING user_role_id_pkey AS UserRoleID, role AS Role";

                var parameters = new { Name = roleName };
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
            using (var connection = _db.CreateConnection())
            {
                var query = "DELETE FROM user_role WHERE user_role_id_pkey = @Id";

                var parameters = new { Id = roleId };
                await connection.ExecuteAsync(query, parameters);
            }
        }

        public async Task<UserRoleDTO> GetUserRole(int userRoleId)
        {
            using (var connection = _db.CreateConnection())
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

        public Task<List<UserRoleDTO>> GetUserRoles()
        {
            throw new NotImplementedException();
        }

        public Task<UserRoleDTO> UpdateUserRole(int roleId, string roleName)
        {
            throw new NotImplementedException();
        }
    }
}
