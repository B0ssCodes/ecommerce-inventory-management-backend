using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IUserRepository
    {

        public Task<(List<UserResponseDTO>, int itemCount)> GetUsers(PaginationParams paginationParams);
        public Task<UserResponseDTO> GetUser(int userID);
        public Task UpdateUser (int userID, UserUpdateDTO updateDTO);
        public Task DeleteUser(int userID);
    }
}
