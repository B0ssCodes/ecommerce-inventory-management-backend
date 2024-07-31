namespace Inventory_Management_Backend.Models.Dto
{
    public class TransactionItemResponseDTO
    {
        public int TransactionItemID { get; set; }
        public int ProductID { get; set; }
        public ShortProductResponseDTO Product { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public int TransactionID { get; set; }
    }
}
