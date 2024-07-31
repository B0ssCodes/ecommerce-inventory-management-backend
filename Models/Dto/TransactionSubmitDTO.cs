namespace Inventory_Management_Backend.Models.Dto
{
    public class TransactionSubmitDTO
    {
        public int TransactionID { get; set; }
        public int Amount { get; set; }
        public List<TransactionItemRequestDTO> TransactionItems { get; set; }
    }
}
