using Inventory_Management_Backend.Models;
using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IUserLogRepository
    {
        public Task CreateUserLog(UserLogRequestDTO requestDTO);
        public Task<(List<AllUserLogResponseDTO>, int)> GetUserLogs(PaginationParams paginationParams);
        public Task<UserLogResponseDTO> GetUserLog(int id);

    }
}
