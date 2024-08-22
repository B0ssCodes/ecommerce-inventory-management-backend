namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseShelfRequestDTO
    {
        public int? ShelfID { get; set; }
        public string ShelfName { get; set; }
        public List<WarehouseBinRequestDTO>? Bins { get; set; }
    }
}
