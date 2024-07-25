using Inventory_Management_Backend.Models.Dto;

namespace Inventory_Management_Backend.Repository.IRepository
{
    public interface IAuthRepository
    {
        public Task<LoginResponseDTO> Login(LoginRequestDTO loginDTO);
        public Task Register(RegisterRequestDTO registerDTO);
    }
}
