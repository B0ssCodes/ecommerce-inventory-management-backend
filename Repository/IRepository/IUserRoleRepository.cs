using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IUserRoleRepository
    {
        public Task<List<UserRoleDTO>> GetUserRoles();
        public Task<UserRoleDTO> GetUserRole(int userRoleId);
        public Task<UserRoleDTO> CreateUserRole(string roleName);
        public Task<UserRoleDTO> UpdateUserRole(int roleId, string roleName);
        public Task DeleteUserRole(int roleId);
    }
}
