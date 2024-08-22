namespace Inventory_Management_Backend.Models.Dto.WarehouseDTO
{
    public class WarehouseBinRequestDTO
    {
        public int? BinID { get; set; }
        public string BinName { get; set; }
        public int? BinCapacity { get; set; }
    }
}
