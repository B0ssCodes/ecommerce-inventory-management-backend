using Inventory_Management_Backend.Models.Dto;

public class ProductResponseDTO
{
    public int ProductID { get; set; }
    public string SKU { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public CategoryResponseDTO Category { get; set; }
    public List<ImageDTO> Images { get; set; }
}