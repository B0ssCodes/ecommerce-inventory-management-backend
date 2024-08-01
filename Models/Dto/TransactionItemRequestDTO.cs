namespace Inventory_Management_Backend.Models.Dto
{
    public class TransactionItemRequestDTO
    {
        public int ProductID { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
