using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IUserRoleRepository
    {
        public Task<(List<UserRoleDTO>, int)> GetUserRoles(PaginationParams paginationParams);
        public Task<UserRoleDTO> GetUserRole(int userRoleId);
        public Task CreateUserRole(UserRoleRequestDTO requestDTO);
        public Task UpdateUserRole(int roleId, UserRoleRequestDTO requestDTO);
        public Task DeleteUserRole(int roleId);
    }
}
