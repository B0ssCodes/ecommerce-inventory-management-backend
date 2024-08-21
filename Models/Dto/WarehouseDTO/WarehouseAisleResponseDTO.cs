namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseAisleResponseDTO
    {
        public int AisleID { get; set; }
        public string AisleName { get; set; }
        public List<WarehouseShelfResponseDTO> Shelves { get; set; }
    }
}
