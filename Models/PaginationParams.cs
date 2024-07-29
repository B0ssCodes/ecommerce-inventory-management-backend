namespace Inventory_Management_Backend.Models
{
    public class PaginationParams
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string? Search { get; set; }

    }
}
