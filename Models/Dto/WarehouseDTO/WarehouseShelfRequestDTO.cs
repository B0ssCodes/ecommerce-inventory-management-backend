namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseShelfRequestDTO
    {
        public string ShelfName { get; set; }
        public List<WarehouseBinRequestDTO>? Bins { get; set; }
    }
}
