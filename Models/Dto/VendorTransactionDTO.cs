namespace Inventory_Management_Backend.Models.Dto
{
    public class VendorTransactionDTO
    {
        public string? VendorEmail { get; set; }    
        public PaginationParams PaginationParams { get; set; }
    }
}
