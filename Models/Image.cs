namespace Inventory_Management_Backend.Models
{
    public class Image
    {
        public int ImageID { get; set; }
        public string Url { get; set; }
        public int ProductID { get; set; }
        public Product Product { get; set; }
    }
}