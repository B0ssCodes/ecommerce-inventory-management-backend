using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IUserPermissionRepository
    {
        public Task<int> PermissionExists(UserPermissionRequestDTO requestDTO);
        public Task<List<UserPermissionResponseDTO>> GetUserPermissions(PaginationParams paginationParams);
        public Task<UserPermissionResponseDTO> GetUserPermission(int userPermissionID);
        public Task<int> CreateUserPermission(UserPermissionRequestDTO requestDTO);
        public Task UpdateUserPermission(int userPermissionID, UserPermissionRequestDTO requestDTO);
        public Task DeleteUserPermission(int userPermissionID);

    }
}
