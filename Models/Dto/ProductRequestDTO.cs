namespace Inventory_Management_Backend.Models.Dto
{
    public class ProductRequestDTO
    {
        public string SKU { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public decimal Cost { get; set; }
        public int CategoryID { get; set; }
        public List<Image> Images { get; set; }
    }
}
