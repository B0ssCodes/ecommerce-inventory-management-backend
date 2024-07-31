namespace Inventory_Management_Backend.Models.Dto
{
    public class TransactionResponseDTO
    {
        public int TransactionID { get; set; }
        public int Amount { get; set; }
        public DateTime Date { get; set; }
        public VendorResponseDTO Vendor { get; set; }
        public string TransactionType { get; set; }
        public string TransactionStatus { get; set; }
        public List<TransactionItemResponseDTO> TransactionItems { get; set; }
    }
}
