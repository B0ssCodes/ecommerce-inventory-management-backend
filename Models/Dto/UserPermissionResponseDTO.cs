namespace Inventory_Management_Backend.Models.Dto
{
    public class UserPermissionResponseDTO
    {
        public int UserPermissionID { get; set; }
        public string Permission { get; set; }
        public int UserRoleID { get; set; }
    }
}
