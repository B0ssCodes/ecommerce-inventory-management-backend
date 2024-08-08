namespace Inventory_Management_Backend.Models.Dto
{
    public class CategoryProductsResponseDTO
    {
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public List<AllProductCategoryResponseDTO> Products { get; set; }
    }
}
