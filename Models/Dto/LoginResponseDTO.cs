namespace Inventory_Management_Backend.Models.Dto
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }
        public string FirstName { get; set; } 
        public string Email { get; set; }
        public string Role { get; set; }


    }
}
