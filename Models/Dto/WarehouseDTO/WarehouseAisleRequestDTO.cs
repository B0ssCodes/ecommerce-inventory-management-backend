namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseAisleRequestDTO
    {
        public string AisleName { get; set; }
        public List<WarehouseShelfRequestDTO>? Shelves { get; set; }
    }
}
