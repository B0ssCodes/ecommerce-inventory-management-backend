namespace Inventory_Management_Backend.Models.Dto
{
    public class ProductSelectResponseDTO
    {
        public int ProductID { get; set; }
        public string SKU { get; set; }
        public string Name { get; set; }
        public decimal Cost { get; set; }
        public int Quantity { get; set; }
        public string Category { get; set; }
    }
}
