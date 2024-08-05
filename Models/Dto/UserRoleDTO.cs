namespace Inventory_Management_Backend.Models.Dto
{
    public class UserRoleDTO
    {
        public int UserRoleID { get; set; } 
        public string Role { get; set; }
        public List<AllUserPermissionResponseDTO> Permissions { get; set; }
    }
}
