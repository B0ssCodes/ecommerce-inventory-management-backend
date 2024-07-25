namespace Inventory_Management_Backend.Models.Dto
{
    public class RegisterRequestDTO
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateOnly Birthday { get; set; }
        public int UserRoleID { get; set; }

    }
}
