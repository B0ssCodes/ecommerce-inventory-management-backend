namespace Inventory_Management_Backend.Models
{
    public class TransactionItem
    {
        public int TransactionItemID { get; set; } 
        public int ProductID { get; set; }
        public List<Product> Products { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int TransactionID { get; set; }
    }
}
