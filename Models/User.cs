using Microsoft.AspNetCore.Identity;

namespace Inventory_Management_Backend.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateOnly Birthday { get; set; }

        // Foreign Key for UserRole
        public int UserRoleID { get; set; }
        public UserRole UserRole { get; set; }
    }
}
