namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseShelfResponseDTO
    {
        public int ShelfID { get; set; }
        public string ShelfName { get; set; }
        public List<WarehouseBinResponseDTO> Bins { get; set; }
    }
}
