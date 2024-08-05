namespace Inventory_Management_Backend.Models.Dto
{
    public class UserRoleRequestDTO
    {
        public string RoleName { get; set; }
        public List<string> Permissions { get; set; }
    }
}
