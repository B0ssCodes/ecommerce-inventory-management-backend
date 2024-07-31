namespace Inventory_Management_Backend.Models.Dto
{
    public class AllTransactionResponseDTO
    {
        public int TransactionID { get; set; }
        public int Amount { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
        public string Status { get; set; }
        public ShortVendorResponseDTO Vendor { get; set; }
    }
}
