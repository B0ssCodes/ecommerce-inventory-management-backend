namespace Inventory_Management_Backend.Models
{
    public class UserWithRole
    {
        
            public int UserId { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Password { get; set; }
            public int UserRoleID { get; set; }
            public string UserRole { get; set; }
        
    }
}
